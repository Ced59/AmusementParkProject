using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class PersonalRankingSharePreviewBuilder : ISharePublicationPreviewBuilder
{
    private const int MaximumRatingCount = 1000;

    private readonly IShareSourceRevisionRepository sourceRevisionRepository;
    private readonly IRatingRepository ratingRepository;
    private readonly IUserRepository userRepository;

    public PersonalRankingSharePreviewBuilder(
        IShareSourceRevisionRepository sourceRevisionRepository,
        IRatingRepository ratingRepository,
        IUserRepository userRepository)
    {
        this.sourceRevisionRepository = sourceRevisionRepository;
        this.ratingRepository = ratingRepository;
        this.userRepository = userRepository;
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
        ShareSourceRevision revisionBefore = await this.sourceRevisionRepository.GetOrCreateAsync(
            sourceScopeKey,
            cancellationToken);
        ShareSourceRevision catalogRevisionBefore = await this.sourceRevisionRepository.GetOrCreateAsync(
            PersonalRankingShareSourceScope.PublicCatalog,
            cancellationToken);
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
        UserRatingStatsResult? sourceStatistics = includesRatings
            ? await this.ratingRepository.GetVisibleUserRatingStatsAsync(ownerUserId, cancellationToken)
            : null;
        IReadOnlyCollection<UserRatingListItemResult> sourceRatings = includesRatings
            ? await this.ratingRepository.GetVisibleUserRankingSourcesAsync(
                ownerUserId,
                MaximumRatingCount,
                cancellationToken)
            : Array.Empty<UserRatingListItemResult>();

        ShareSourceRevision revisionAfter = await this.sourceRevisionRepository.GetOrCreateAsync(
            sourceScopeKey,
            cancellationToken);
        ShareSourceRevision catalogRevisionAfter = await this.sourceRevisionRepository.GetOrCreateAsync(
            PersonalRankingShareSourceScope.PublicCatalog,
            cancellationToken);
        User? userAfter = await this.userRepository.GetByIdAsync(ownerUserId, cancellationToken);
        if (!revisionAfter.IsStable
            || !catalogRevisionAfter.IsStable
            || revisionAfter.Revision != revisionBefore.Revision
            || catalogRevisionAfter.Revision != catalogRevisionBefore.Revision
            || !HasSamePublicIdentity(user, userAfter))
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
            contentPolicy.Includes(ShareContentField.Avatar)
                ? NormalizeOptional(user.AvatarUrl)
                : null,
            statistics,
            ratings,
            sourceStatistics is not null && sourceStatistics.TotalRatings > ratings.Count);
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
            ToPublicBuckets(source.ByPark),
            ToPublicBuckets(source.ByTargetType),
            ToPublicBuckets(source.ByParkItemCategory));
    }

    private static IReadOnlyCollection<PersonalRankingShareStatBucketResult> ToPublicBuckets(
        IEnumerable<UserRatingStatBucketResult> source)
    {
        return source
            .Where(static bucket => !string.IsNullOrWhiteSpace(bucket.Label))
            .Select(static bucket => new PersonalRankingShareStatBucketResult(
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
}
