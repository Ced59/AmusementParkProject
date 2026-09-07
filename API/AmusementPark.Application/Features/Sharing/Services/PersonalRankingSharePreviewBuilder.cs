using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class PersonalRankingSharePreviewBuilder : ISharePublicationPreviewBuilder
{
    private const int MaximumRatingCount = 1000;

    private readonly IShareSourceRevisionRepository sourceRevisionRepository;
    private readonly IRatingRepository ratingRepository;
    private readonly IUserRepository userRepository;
    private readonly IImageRepository imageRepository;

    public PersonalRankingSharePreviewBuilder(
        IShareSourceRevisionRepository sourceRevisionRepository,
        IRatingRepository ratingRepository,
        IUserRepository userRepository,
        IImageRepository imageRepository)
    {
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.ratingRepository = ratingRepository;
        this.userRepository = userRepository;
        this.imageRepository = imageRepository;
    }

    public SharePublicationType PublicationType => SharePublicationType.PersonalRanking;

    public async Task<ApplicationResult<SharePublicationPreviewResult>> BuildAsync(
        string ownerUserId,
        string? sourceId,
        ShareContentPolicy contentPolicy,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(sourceId))
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.InvalidSource());
        }

        string sourceScopeKey = PersonalRankingShareSourceScope.Create(ownerUserId);
        string[] revisionScopeKeys =
        {
            sourceScopeKey,
            PersonalRankingShareSourceScope.PublicCatalog,
        };
        IReadOnlyDictionary<string, ShareSourceRevision> revisionsBefore =
            await this.sourceRevisionRepository.GetSnapshotAsync(
                revisionScopeKeys,
                cancellationToken);
        ShareSourceRevision revisionBefore = revisionsBefore[sourceScopeKey];
        ShareSourceRevision catalogRevisionBefore =
            revisionsBefore[PersonalRankingShareSourceScope.PublicCatalog];
        if (!revisionBefore.IsStable || !catalogRevisionBefore.IsStable)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        User? user = await this.userRepository.GetByIdAsync(ownerUserId, cancellationToken);
        if (user is null || !user.IsActivated || user.IsBlocked)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        bool includesRatings = contentPolicy.Includes(ShareContentField.GlobalRatings);
        bool includesAvatar = contentPolicy.Includes(ShareContentField.Avatar);
        Image? avatarBefore = includesAvatar
            ? await this.imageRepository.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                ownerUserId,
                ImageCategory.Avatar,
                cancellationToken)
            : null;
        UserRatingStatsResult? sourceStatistics = includesRatings
            ? await this.ratingRepository.GetVisibleUserRatingStatsAsync(
                ownerUserId,
                cancellationToken)
            : null;
        IReadOnlyCollection<UserRatingListItemResult> sourceRatingCandidates = includesRatings
            ? await this.ratingRepository.GetVisibleUserRankingSourcesAsync(
                ownerUserId,
                MaximumRatingCount + 1,
                cancellationToken)
            : Array.Empty<UserRatingListItemResult>();
        IReadOnlyCollection<UserRatingListItemResult> sourceRatings = sourceRatingCandidates
            .Take(MaximumRatingCount)
            .ToArray();

        IReadOnlyDictionary<string, ShareSourceRevision> revisionsAfter =
            await this.sourceRevisionRepository.GetSnapshotAsync(
                revisionScopeKeys,
                cancellationToken);
        ShareSourceRevision revisionAfter = revisionsAfter[sourceScopeKey];
        ShareSourceRevision catalogRevisionAfter =
            revisionsAfter[PersonalRankingShareSourceScope.PublicCatalog];
        User? userAfter = await this.userRepository.GetByIdAsync(ownerUserId, cancellationToken);
        Image? avatarAfter = includesAvatar
            ? await this.imageRepository.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                ownerUserId,
                ImageCategory.Avatar,
                cancellationToken)
            : null;
        if (!revisionAfter.IsStable
            || !catalogRevisionAfter.IsStable
            || revisionAfter.Revision != revisionBefore.Revision
            || catalogRevisionAfter.Revision != catalogRevisionBefore.Revision
            || !HasSamePublicIdentity(user, userAfter)
            || !string.Equals(
                ResolvePublicAvatarUrl(avatarBefore, ownerUserId),
                ResolvePublicAvatarUrl(avatarAfter, ownerUserId),
                StringComparison.Ordinal))
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        PersonalRankingShareStatisticsResult? statistics = sourceStatistics is null
            ? null
            : ToPublicStatistics(sourceStatistics);
        IReadOnlyCollection<PersonalRankingSharePreviewItemResult> ratings = sourceRatings
            .Where(static rating => !string.IsNullOrWhiteSpace(rating.TargetName))
            .Select(static rating => new PersonalRankingSharePreviewItemResult(
                rating.TargetType,
                rating.TargetName.Trim(),
                NormalizeOptional(rating.ParkName),
                rating.ParkItemCategory,
                rating.ParkItemType,
                rating.Value))
            .ToArray();
        PersonalRankingSharePreviewResult personalRanking = new PersonalRankingSharePreviewResult(
            contentPolicy.Includes(ShareContentField.PublicDisplayName)
                ? NormalizeOptional(user.ResolvePublicDisplayName()) ?? "User"
                : null,
            includesAvatar
                ? ResolvePublicAvatarUrl(avatarAfter, ownerUserId)
                : null,
            statistics,
            ratings,
            sourceRatingCandidates.Count > MaximumRatingCount);
        long sourceVersion;
        try
        {
            sourceVersion = checked(revisionAfter.Revision + catalogRevisionAfter.Revision);
        }
        catch (OverflowException)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.SourceVersionUnavailable());
        }

        SharePublicationPreviewResult preview = new SharePublicationPreviewResult(
            this.PublicationType,
            sourceVersion,
            contentPolicy.SchemaVersion,
            contentPolicy.DatePrecision,
            contentPolicy.IncludedFields,
            personalRanking);
        return ApplicationResult<SharePublicationPreviewResult>.Success(preview);
    }

    private static PersonalRankingShareStatisticsResult ToPublicStatistics(
        UserRatingStatsResult source)
    {
        return new PersonalRankingShareStatisticsResult(
            source.TotalRatings,
            source.AverageRating,
            source.HighestRating,
            source.LowestRating,
            ToPublicBuckets(source.ByPark, includeStableKey: false),
            ToPublicBuckets(source.ByTargetType, includeStableKey: true),
            ToPublicBuckets(source.ByParkItemCategory, includeStableKey: true));
    }

    private static IReadOnlyCollection<PersonalRankingShareStatBucketResult> ToPublicBuckets(
        IEnumerable<UserRatingStatBucketResult> source,
        bool includeStableKey)
    {
        return source
            .Where(static bucket => !string.IsNullOrWhiteSpace(bucket.Label))
            .Select(bucket => new PersonalRankingShareStatBucketResult(
                includeStableKey ? NormalizeOptional(bucket.Key) : null,
                bucket.Label.Trim(),
                bucket.Count,
                bucket.AverageRating))
            .ToArray();
    }

    private static string? NormalizeOptional(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }

    private static bool HasSamePublicIdentity(User before, User? after)
    {
        return after is not null
            && after.IsActivated
            && !after.IsBlocked
            && string.Equals(
                NormalizeOptional(before.ResolvePublicDisplayName()),
                NormalizeOptional(after.ResolvePublicDisplayName()),
                StringComparison.Ordinal)
            && string.Equals(
                NormalizeOptional(before.AvatarUrl),
                NormalizeOptional(after.AvatarUrl),
                StringComparison.Ordinal);
    }

    private static string? ResolvePublicAvatarUrl(Image? image, string ownerUserId)
    {
        if (image is null
            || string.IsNullOrWhiteSpace(image.Id)
            || !image.IsPublished
            || !image.IsCurrent
            || image.OwnerType != ImageOwnerType.User
            || image.Category != ImageCategory.Avatar
            || !string.Equals(image.OwnerId?.Trim(), ownerUserId, StringComparison.Ordinal))
        {
            return null;
        }

        return $"/images/{image.Id}";
    }
}
