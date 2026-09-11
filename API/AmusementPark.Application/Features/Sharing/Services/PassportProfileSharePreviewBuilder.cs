using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class PassportProfileSharePreviewBuilder
    : ISharePublicationPreviewBuilder, IPassportProfileSharePreviewBuilder
{
    private readonly IPassportProfileSourceReader sourceReader;
    private readonly IPassportProfileShareSourceVersionProvider sourceVersionProvider;
    private readonly IParkRepository parkRepository;
    private readonly IVisitTargetResolver targetResolver;
    private readonly IRatingRepository ratingRepository;
    private readonly IUserRepository userRepository;
    private readonly IImageRepository imageRepository;

    public PassportProfileSharePreviewBuilder(
        IPassportProfileSourceReader sourceReader,
        IPassportProfileShareSourceVersionProvider sourceVersionProvider,
        IParkRepository parkRepository,
        IVisitTargetResolver targetResolver,
        IRatingRepository ratingRepository,
        IUserRepository userRepository,
        IImageRepository imageRepository)
    {
        this.sourceReader = sourceReader ?? throw new ArgumentNullException(nameof(sourceReader));
        this.sourceVersionProvider = sourceVersionProvider
            ?? throw new ArgumentNullException(nameof(sourceVersionProvider));
        this.parkRepository = parkRepository ?? throw new ArgumentNullException(nameof(parkRepository));
        this.targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
        this.ratingRepository = ratingRepository ?? throw new ArgumentNullException(nameof(ratingRepository));
        this.userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        this.imageRepository = imageRepository ?? throw new ArgumentNullException(nameof(imageRepository));
    }

    public SharePublicationType PublicationType => SharePublicationType.PassportProfile;

    public Task<ApplicationResult<SharePublicationPreviewResult>> BuildAsync(
        string ownerUserId,
        string? sourceId,
        ShareContentPolicy contentPolicy,
        CancellationToken cancellationToken)
    {
        return !string.IsNullOrWhiteSpace(sourceId)
            ? Task.FromResult(ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.InvalidSource()))
            : this.BuildAsync(ownerUserId, contentPolicy, null, cancellationToken);
    }

    public async Task<ApplicationResult<SharePublicationPreviewResult>> BuildAsync(
        string ownerUserId,
        ShareContentPolicy contentPolicy,
        PassportProfileShareInput? input,
        CancellationToken cancellationToken)
    {
        ApplicationResult<PassportProfileShareInput> normalizedResult =
            PassportProfileShareInputNormalizer.Normalize(input, contentPolicy);
        if (!normalizedResult.IsSuccess || normalizedResult.Value is null)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(normalizedResult.Errors);
        }

        PassportProfileShareInput normalizedInput = normalizedResult.Value;
        ApplicationResult<PassportProfileShareSourceRevision> versionBefore =
            await this.sourceVersionProvider.GetOwnedSourceVersionAsync(
                ownerUserId,
                cancellationToken);
        if (!versionBefore.IsSuccess || versionBefore.Value is null)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(versionBefore.Errors);
        }

        PassportProfileSourceData source = await this.sourceReader.ReadOwnedCompletedPassportAsync(
            ownerUserId,
            cancellationToken);
        User? user = await this.userRepository.GetByIdAsync(ownerUserId, cancellationToken);
        if (user is null || !user.IsActivated || user.IsBlocked)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.SourceUnavailable());
        }

        string[] visitedParkIds = source.Visits
            .Select(static visit => visit.ParkId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<Park> parkCandidates = visitedParkIds.Length == 0
            ? Array.Empty<Park>()
            : await this.parkRepository.GetByIdsAsync(visitedParkIds, cancellationToken);
        IReadOnlyDictionary<string, Park> publicParks = parkCandidates
            .Where(static park => park.IsVisible
                && !string.IsNullOrWhiteSpace(park.Id)
                && !string.IsNullOrWhiteSpace(park.Name))
            .GroupBy(static park => park.Id, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First(),
                StringComparer.Ordinal);
        string[] parkItemIds = source.Rides
            .Select(static ride => ride.ParkItemId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyDictionary<string, VisitTarget> targets = parkItemIds.Length == 0
            ? new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            : await this.targetResolver.ResolveAsync(parkItemIds, cancellationToken);
        IReadOnlyCollection<UserRatingListItemResult> ratingCandidates =
            contentPolicy.Includes(ShareContentField.GlobalRatings)
                ? await this.ratingRepository.GetVisibleUserRankingSourcesAsync(
                    ownerUserId,
                    PassportProfileShareInputNormalizer.MaximumSelectedRatings + 1,
                    cancellationToken)
                : Array.Empty<UserRatingListItemResult>();
        Image? avatarBefore = contentPolicy.Includes(ShareContentField.Avatar)
            ? await this.imageRepository.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                ownerUserId,
                ImageCategory.Avatar,
                cancellationToken)
            : null;

        ApplicationResult<PassportProfileSharePreviewResult> contentResult = this.BuildContent(
            user,
            avatarBefore,
            source,
            publicParks,
            targets,
            ratingCandidates,
            contentPolicy,
            normalizedInput);
        if (!contentResult.IsSuccess || contentResult.Value is null)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(contentResult.Errors);
        }

        ApplicationResult<PassportProfileShareSourceRevision> versionAfter =
            await this.sourceVersionProvider.GetOwnedSourceVersionAsync(
                ownerUserId,
                cancellationToken);
        User? userAfter = await this.userRepository.GetByIdAsync(ownerUserId, cancellationToken);
        Image? avatarAfter = contentPolicy.Includes(ShareContentField.Avatar)
            ? await this.imageRepository.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                ownerUserId,
                ImageCategory.Avatar,
                cancellationToken)
            : null;
        if (!versionAfter.IsSuccess
            || versionAfter.Value is null
            || versionAfter.Value.Version != versionBefore.Value.Version
            || !string.Equals(
                versionBefore.Value.SourceFingerprint,
                versionAfter.Value.SourceFingerprint,
                StringComparison.Ordinal)
            || !string.Equals(
                source.SourceFingerprint,
                versionAfter.Value.SourceFingerprint,
                StringComparison.Ordinal)
            || !HasSamePublicIdentity(user, userAfter)
            || !string.Equals(
                ResolvePublicAvatarUrl(avatarBefore, ownerUserId),
                ResolvePublicAvatarUrl(avatarAfter, ownerUserId),
                StringComparison.Ordinal))
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        string fingerprint = PassportProfileShareInputNormalizer.CreateFingerprint(normalizedInput);
        return ApplicationResult<SharePublicationPreviewResult>.Success(
            new SharePublicationPreviewResult(
                this.PublicationType,
                versionAfter.Value.Version,
                contentPolicy.SchemaVersion,
                contentPolicy.DatePrecision,
                contentPolicy.IncludedFields,
                null,
                PassportProfile: contentResult.Value,
                ContentFingerprint: fingerprint));
    }

    private ApplicationResult<PassportProfileSharePreviewResult> BuildContent(
        User user,
        Image? avatar,
        PassportProfileSourceData source,
        IReadOnlyDictionary<string, Park> publicParks,
        IReadOnlyDictionary<string, VisitTarget> targets,
        IReadOnlyCollection<UserRatingListItemResult> ratingCandidates,
        ShareContentPolicy policy,
        PassportProfileShareInput input)
    {
        HashSet<int> selectedYears = new HashSet<int>(
            input.SelectedYears ?? Array.Empty<int>());
        HashSet<string> selectedParkIds = new HashSet<string>(
            input.SelectedParkIds ?? Array.Empty<string>(),
            StringComparer.Ordinal);
        HashSet<string> availableParkIds = source.Visits
            .Select(static visit => visit.ParkId)
            .Where(publicParks.ContainsKey)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<int> availableYears = source.Visits
            .Where(visit => availableParkIds.Contains(visit.ParkId))
            .Select(static visit => visit.VisitDate.Year)
            .ToHashSet();
        if (!selectedYears.IsSubsetOf(availableYears)
            || !selectedParkIds.IsSubsetOf(availableParkIds))
        {
            return ApplicationResult<PassportProfileSharePreviewResult>.Failure(
                SharingApplicationErrors.InvalidPassportProfileSelection());
        }

        PassportVisitStatisticsObservation[] visits = source.Visits
            .Where(visit => selectedYears.Contains(visit.VisitDate.Year)
                && selectedParkIds.Contains(visit.ParkId)
                && publicParks.ContainsKey(visit.ParkId))
            .ToArray();
        HashSet<string> visitIds = visits
            .Select(static visit => visit.VisitId)
            .ToHashSet(StringComparer.Ordinal);
        PassportRideStatisticsObservation[] rides = source.Rides
            .Where(ride => visitIds.Contains(ride.VisitId)
                && CanExposeTarget(ride, source.HistoricalItemNames, targets))
            .ToArray();
        PassportGlobalStatistics statistics = PassportGlobalStatisticsCalculator.Calculate(visits, rides);
        bool includesActivity = policy.Includes(ShareContentField.RideCount);
        bool includesTemporalRatings = policy.Includes(ShareContentField.TemporalRatings);
        bool includesGeography = policy.Includes(ShareContentField.GeographicStatistics);
        bool includesRanking = policy.Includes(ShareContentField.GlobalRatings);
        bool includesMissed = policy.Includes(ShareContentField.MissedItems);

        Dictionary<string, UserRatingListItemResult> ratingsByKey = ratingCandidates
            .Take(PassportProfileShareInputNormalizer.MaximumSelectedRatings)
            .GroupBy(
                static rating => PassportProfileRatingSelectionKey.Create(
                    rating.TargetType,
                    rating.TargetId),
                StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.First(),
                StringComparer.Ordinal);
        HashSet<string> selectedRatingKeys = new HashSet<string>(
            input.SelectedRatingKeys ?? Array.Empty<string>(),
            StringComparer.Ordinal);
        if (!selectedRatingKeys.IsSubsetOf(ratingsByKey.Keys))
        {
            return ApplicationResult<PassportProfileSharePreviewResult>.Failure(
                SharingApplicationErrors.InvalidPassportProfileSelection());
        }

        PassportProfileShareRatingResult[] ranking = includesRanking
            ? selectedRatingKeys
                .Select(key => ratingsByKey[key])
                .Where(rating => selectedParkIds.Contains(rating.ParkId))
                .OrderByDescending(static rating => rating.Value)
                .ThenBy(static rating => rating.TargetName, StringComparer.OrdinalIgnoreCase)
                .Select(static rating => new PassportProfileShareRatingResult(
                    rating.TargetType.ToString(),
                    rating.TargetName.Trim(),
                    NormalizeOptional(rating.ParkName),
                    rating.ParkItemCategory?.ToString(),
                    rating.Value))
                .ToArray()
            : Array.Empty<PassportProfileShareRatingResult>();
        PassportProfileSharePreviewResult result = new PassportProfileSharePreviewResult(
            policy.Includes(ShareContentField.PublicDisplayName)
                ? NormalizeOptional(user.ResolvePublicDisplayName()) ?? "User"
                : null,
            policy.Includes(ShareContentField.Avatar)
                ? ResolvePublicAvatarUrl(avatar, user.Id)
                : null,
            policy.Includes(ShareContentField.PublicCaption) ? input.PublicCaption : null,
            input.Visibility,
            input.AllowsComparisons,
            includesGeography ? statistics.ParkCount : null,
            includesActivity ? statistics.Summary.VisitCount : null,
            includesActivity ? statistics.Summary.RideOutcomes.CompletedRideCount : null,
            includesActivity ? statistics.Summary.DistinctCompletedItemCount : null,
            includesTemporalRatings ? ToRatingSummary(
                statistics.Summary.RatedVisitCount,
                statistics.Summary.VisitCount,
                statistics.Summary.ParkRatings?.Average) : null,
            includesTemporalRatings ? ToRatingSummary(
                statistics.Summary.RatedRideCount,
                statistics.Summary.RideOutcomes.CompletedRideCount,
                statistics.Summary.RideRatings?.Average) : null,
            includesGeography ? BuildCountries(visits, publicParks) : Array.Empty<PassportProfileShareCountryResult>(),
            includesGeography
                ? BuildYears(visits, rides, includesActivity)
                : Array.Empty<PassportProfileShareYearResult>(),
            includesGeography
                ? BuildParks(visits, rides, publicParks, includesActivity, includesTemporalRatings)
                : Array.Empty<PassportProfileShareParkResult>(),
            ranking,
            includesMissed
                ? BuildMissedItems(rides, source.HistoricalItemNames, targets)
                : Array.Empty<PassportProfileShareMissedItemResult>(),
            source.Visits.Any(visit => !publicParks.ContainsKey(visit.ParkId))
                || source.Rides.Any(ride => publicParks.ContainsKey(ride.ParkId)
                    && !CanExposeTarget(ride, source.HistoricalItemNames, targets))
                || ratingCandidates.Count > PassportProfileShareInputNormalizer.MaximumSelectedRatings,
            PassportProfileShareVersion.CalculationVersion,
            visits.Length == 0 && ranking.Length == 0);
        return ApplicationResult<PassportProfileSharePreviewResult>.Success(result);
    }

    private static PassportProfileShareRatingSummaryResult ToRatingSummary(
        long ratedCount,
        long eligibleCount,
        double? average)
    {
        return new PassportProfileShareRatingSummaryResult(ratedCount, eligibleCount, average);
    }

    private static PassportProfileShareCountryResult[] BuildCountries(
        IEnumerable<PassportVisitStatisticsObservation> visits,
        IReadOnlyDictionary<string, Park> parks)
    {
        return visits
            .Where(visit => !string.IsNullOrWhiteSpace(parks[visit.ParkId].CountryCode))
            .GroupBy(
                visit => parks[visit.ParkId].CountryCode!.Trim().ToUpperInvariant(),
                StringComparer.Ordinal)
            .Select(group => new PassportProfileShareCountryResult(
                group.Key,
                group.Select(static visit => visit.ParkId).Distinct(StringComparer.Ordinal).LongCount(),
                group.LongCount()))
            .OrderByDescending(static country => country.VisitCount)
            .ThenBy(static country => country.CountryCode, StringComparer.Ordinal)
            .ToArray();
    }

    private static PassportProfileShareYearResult[] BuildYears(
        IEnumerable<PassportVisitStatisticsObservation> visits,
        IEnumerable<PassportRideStatisticsObservation> rides,
        bool includesActivity)
    {
        IReadOnlyDictionary<int, long> completedRidesByYear = rides
            .Where(static ride => ride.Status == RideOccurrenceStatus.Completed)
            .GroupBy(static ride => ride.VisitDate.Year)
            .ToDictionary(static group => group.Key, static group => group.LongCount());
        return visits.GroupBy(static visit => visit.VisitDate.Year)
            .OrderByDescending(static group => group.Key)
            .Select(group => new PassportProfileShareYearResult(
                group.Key,
                group.LongCount(),
                group.Select(static visit => visit.ParkId).Distinct(StringComparer.Ordinal).LongCount(),
                includesActivity ? completedRidesByYear.GetValueOrDefault(group.Key) : null))
            .ToArray();
    }

    private static PassportProfileShareParkResult[] BuildParks(
        IEnumerable<PassportVisitStatisticsObservation> visits,
        IEnumerable<PassportRideStatisticsObservation> rides,
        IReadOnlyDictionary<string, Park> parks,
        bool includesActivity,
        bool includesRatings)
    {
        IReadOnlyDictionary<string, long> completedRidesByPark = rides
            .Where(static ride => ride.Status == RideOccurrenceStatus.Completed)
            .GroupBy(static ride => ride.ParkId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.LongCount(),
                StringComparer.Ordinal);
        return visits.GroupBy(static visit => visit.ParkId, StringComparer.Ordinal)
            .Select(group =>
            {
                PassportVisitStatisticsObservation[] values = group.ToArray();
                double[] ratings = values
                    .Where(static visit => visit.ParkAssessment.HasValue)
                    .Select(static visit => visit.ParkAssessment!.Value.DoubleValue)
                    .ToArray();
                Park park = parks[group.Key];
                return new PassportProfileShareParkResult(
                    park.Name!.Trim(),
                    NormalizeCountryCode(park.CountryCode),
                    values.LongLength,
                    values.Min(static visit => visit.VisitDate.Year),
                    values.Max(static visit => visit.VisitDate.Year),
                    includesActivity ? completedRidesByPark.GetValueOrDefault(group.Key) : null,
                    includesRatings
                        ? new PassportProfileShareRatingSummaryResult(
                            ratings.LongLength,
                            values.LongLength,
                            ratings.Length == 0 ? null : ratings.Average())
                        : null);
            })
            .OrderByDescending(static park => park.VisitCount)
            .ThenBy(static park => park.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PassportProfileShareMissedItemResult[] BuildMissedItems(
        IEnumerable<PassportRideStatisticsObservation> rides,
        IReadOnlyDictionary<string, string?> historicalNames,
        IReadOnlyDictionary<string, VisitTarget> targets)
    {
        return rides.Where(static ride => ride.Status != RideOccurrenceStatus.Completed)
            .GroupBy(
                ride => new
                {
                    Name = ResolveName(ride.ParkItemId, historicalNames, targets),
                    ride.Status,
                })
            .Where(static group => group.Key.Name is not null)
            .Select(group => new PassportProfileShareMissedItemResult(
                group.Key.Name!,
                ToPublicMissedStatus(group.Key.Status),
                group.LongCount()))
            .OrderByDescending(static item => item.OccurrenceCount)
            .ThenBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToArray();
    }

    private static string ToPublicMissedStatus(RideOccurrenceStatus status)
    {
        return status == RideOccurrenceStatus.MissedClosed
            ? "MissedClosure"
            : "MissedOther";
    }

    private static bool CanExposeTarget(
        PassportRideStatisticsObservation ride,
        IReadOnlyDictionary<string, string?> historicalNames,
        IReadOnlyDictionary<string, VisitTarget> targets)
    {
        if (!targets.TryGetValue(ride.ParkItemId, out VisitTarget? target))
        {
            return historicalNames.TryGetValue(ride.ParkItemId, out string? historicalName)
                && !string.IsNullOrWhiteSpace(historicalName);
        }

        return target.IsVisible
            && string.Equals(target.ParkId, ride.ParkId, StringComparison.Ordinal);
    }

    private static string? ResolveName(
        string parkItemId,
        IReadOnlyDictionary<string, string?> historicalNames,
        IReadOnlyDictionary<string, VisitTarget> targets)
    {
        if (targets.TryGetValue(parkItemId, out VisitTarget? target)
            && target.IsVisible
            && !string.IsNullOrWhiteSpace(target.Name))
        {
            return target.Name.Trim();
        }

        return historicalNames.TryGetValue(parkItemId, out string? historicalName)
            ? NormalizeOptional(historicalName)
            : null;
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

    private static string? NormalizeOptional(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }

    private static string? NormalizeCountryCode(string? value)
    {
        string normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        return normalized.Length == 2 ? normalized : null;
    }
}
