using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class YearRecapSharePreviewBuilder : IYearRecapSharePreviewBuilder
{
    private readonly IYearRecapSourceReader sourceReader;
    private readonly IYearRecapShareSourceVersionProvider sourceVersionProvider;
    private readonly IVisitRecapPublicParkReader publicParkReader;
    private readonly IVisitTargetResolver targetResolver;
    private readonly TimeProvider timeProvider;

    public YearRecapSharePreviewBuilder(
        IYearRecapSourceReader sourceReader,
        IYearRecapShareSourceVersionProvider sourceVersionProvider,
        IVisitRecapPublicParkReader publicParkReader,
        IVisitTargetResolver targetResolver,
        TimeProvider? timeProvider = null)
    {
        this.sourceReader = sourceReader ?? throw new ArgumentNullException(nameof(sourceReader));
        this.sourceVersionProvider = sourceVersionProvider
            ?? throw new ArgumentNullException(nameof(sourceVersionProvider));
        this.publicParkReader = publicParkReader
            ?? throw new ArgumentNullException(nameof(publicParkReader));
        this.targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public SharePublicationType PublicationType => SharePublicationType.YearRecap;

    public Task<ApplicationResult<SharePublicationPreviewResult>> BuildAsync(
        string ownerUserId,
        string? sourceId,
        ShareContentPolicy contentPolicy,
        CancellationToken cancellationToken)
    {
        if (!YearRecapSharePublicationSource.TryParseYear(sourceId, out int year))
        {
            return Task.FromResult(
                ApplicationResult<SharePublicationPreviewResult>.Failure(
                    SharingApplicationErrors.InvalidSource()));
        }

        return this.BuildAsync(
            ownerUserId,
            year,
            contentPolicy,
            null,
            cancellationToken);
    }

    public async Task<ApplicationResult<SharePublicationPreviewResult>> BuildAsync(
        string ownerUserId,
        int year,
        ShareContentPolicy contentPolicy,
        YearRecapShareInput? input,
        CancellationToken cancellationToken)
    {
        ApplicationResult<YearRecapShareInput> normalizedInputResult =
            YearRecapShareInputNormalizer.Normalize(input, contentPolicy);
        if (!normalizedInputResult.IsSuccess || normalizedInputResult.Value is null)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                normalizedInputResult.Errors);
        }

        ApplicationResult<YearRecapShareSourceRevision> versionBefore =
            await this.sourceVersionProvider.GetOwnedSourceVersionAsync(
                ownerUserId,
                year,
                cancellationToken);
        if (!versionBefore.IsSuccess || versionBefore.Value is null)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(versionBefore.Errors);
        }

        YearRecapSourceData source = await this.sourceReader.ReadOwnedCompletedYearAsync(
            ownerUserId,
            year,
            cancellationToken);
        if (!source.IsStable)
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        string[] parkIds = source.Visits.Select(static value => value.ParkId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyDictionary<string, string> parkNames =
            await this.publicParkReader.GetVisibleNamesAsync(parkIds, cancellationToken);
        HashSet<string> visibleParkIds = parkNames.Keys.ToHashSet(StringComparer.Ordinal);
        PassportVisitStatisticsObservation[] publicVisits = source.Visits
            .Where(visit => visibleParkIds.Contains(visit.ParkId))
            .ToArray();
        HashSet<string> publicVisitIds = publicVisits.Select(static value => value.VisitId)
            .ToHashSet(StringComparer.Ordinal);
        PassportRideStatisticsObservation[] visibleParkRides = source.Rides
            .Where(ride => publicVisitIds.Contains(ride.VisitId))
            .ToArray();
        string[] parkItemIds = visibleParkRides.Select(static value => value.ParkItemId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyDictionary<string, VisitTarget> targets = parkItemIds.Length == 0
            ? new Dictionary<string, VisitTarget>(StringComparer.Ordinal)
            : await this.targetResolver.ResolveAsync(parkItemIds, cancellationToken);
        List<PassportRideStatisticsObservation> publicRides = new List<PassportRideStatisticsObservation>();
        foreach (PassportRideStatisticsObservation ride in visibleParkRides)
        {
            if (!CanExposeTarget(ride, source.HistoricalItemNames, targets))
            {
                continue;
            }

            targets.TryGetValue(ride.ParkItemId, out VisitTarget? target);
            publicRides.Add(new PassportRideStatisticsObservation(
                ride.RideOccurrenceId,
                ride.VisitId,
                ride.ParkId,
                ride.ParkItemId,
                ride.VisitDate,
                ride.Status,
                ride.Assessment,
                ride.HistoricalCategory,
                target?.Category.ToString()));
        }

        ApplicationResult<YearRecapShareSourceRevision> versionAfter =
            await this.sourceVersionProvider.GetOwnedSourceVersionAsync(
                ownerUserId,
                year,
                cancellationToken);
        if (!versionAfter.IsSuccess
            || versionAfter.Value is null
            || versionAfter.Value.Version != versionBefore.Value.Version
            || !string.Equals(
                source.SourceFingerprint,
                versionBefore.Value.SourceFingerprint,
                StringComparison.Ordinal)
            || !string.Equals(
                source.SourceFingerprint,
                versionAfter.Value.SourceFingerprint,
                StringComparison.Ordinal))
        {
            return ApplicationResult<SharePublicationPreviewResult>.Failure(
                SharingApplicationErrors.SourceChangedDuringPreview());
        }

        PassportYearStatistics statistics = PassportScopeStatisticsCalculator.CalculateYear(
            year,
            publicVisits,
            publicRides);
        YearRecapShareInput normalizedInput = normalizedInputResult.Value;
        YearRecapSharePreviewResult recap = this.BuildRecap(
            statistics,
            publicRides,
            parkNames,
            source.HistoricalItemNames,
            targets,
            contentPolicy,
            normalizedInput,
            source.Visits.Count != publicVisits.Length
                || visibleParkRides.Length != publicRides.Count);
        string fingerprint = YearRecapShareInputNormalizer.CreateFingerprint(normalizedInput);
        return ApplicationResult<SharePublicationPreviewResult>.Success(
            new SharePublicationPreviewResult(
                this.PublicationType,
                versionAfter.Value.Version,
                contentPolicy.SchemaVersion,
                contentPolicy.DatePrecision,
                contentPolicy.IncludedFields,
                null,
                YearRecap: recap,
                ContentFingerprint: fingerprint));
    }

    private YearRecapSharePreviewResult BuildRecap(
        PassportYearStatistics statistics,
        IReadOnlyCollection<PassportRideStatisticsObservation> rides,
        IReadOnlyDictionary<string, string> parkNames,
        IReadOnlyDictionary<string, string?> historicalNames,
        IReadOnlyDictionary<string, VisitTarget> targets,
        ShareContentPolicy policy,
        YearRecapShareInput input,
        bool hasIncompleteCatalog)
    {
        PassportStatisticsSummary summary = statistics.Summary;
        bool includesRides = policy.Includes(ShareContentField.RideCount);
        bool includesRatings = policy.Includes(ShareContentField.TemporalRatings);
        bool includesGeography = policy.Includes(ShareContentField.GeographicStatistics);
        PassportRideStatisticsObservation[] completed = rides
            .Where(static value => value.Status == RideOccurrenceStatus.Completed)
            .ToArray();
        IReadOnlyCollection<YearRecapShareParkResult> parks = includesGeography
            ? statistics.ByPark
                .Where(value => parkNames.ContainsKey(value.ParkId))
                .OrderByDescending(static value => value.Summary.VisitCount)
                .ThenByDescending(static value => value.Summary.RideOutcomes.CompletedRideCount)
                .ThenBy(value => parkNames[value.ParkId], StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .Select(value => new YearRecapShareParkResult(
                    parkNames[value.ParkId],
                    value.Summary.VisitCount,
                    includesRides ? value.Summary.RideOutcomes.CompletedRideCount : null))
                .ToArray()
            : Array.Empty<YearRecapShareParkResult>();
        YearRecapShareHighlightResult? mostRepeated = includesRides
            ? completed.GroupBy(static value => value.ParkItemId, StringComparer.Ordinal)
                .Select(group => BuildHighlight(group, historicalNames, targets))
                .Where(static value => value is not null)
                .Select(static value => value!)
                .Where(static value => value.RideCount > 1)
                .OrderByDescending(static value => value.RideCount)
                .ThenBy(static value => value.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault()
            : null;
        YearRecapShareHighlightResult? topRated = includesRatings
            ? completed.Where(static value => value.Assessment.HasValue)
                .GroupBy(static value => value.ParkItemId, StringComparer.Ordinal)
                .Select(group => BuildHighlight(group, historicalNames, targets))
                .Where(static value => value?.AverageRating is not null)
                .Select(static value => value!)
                .OrderByDescending(static value => value.AverageRating)
                .ThenByDescending(static value => value.RatingCount)
                .ThenBy(static value => value.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault()
            : null;
        DateOnly today = DateOnly.FromDateTime(this.timeProvider.GetUtcNow().UtcDateTime);
        YearRecapShareHighlightResult[] nowClosed = includesRides
            ? completed.GroupBy(static value => value.ParkItemId, StringComparer.Ordinal)
                .Where(group => IsNowClosed(group.Key, today, targets))
                .Select(group => BuildHighlight(group, historicalNames, targets, true))
                .Where(static value => value is not null)
                .Select(static value => value!)
                .OrderByDescending(static value => value.RideCount)
                .ThenBy(static value => value.Name, StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray()
            : Array.Empty<YearRecapShareHighlightResult>();
        return new YearRecapSharePreviewResult(
            statistics.Year,
            includesGeography ? statistics.ParkCount : null,
            summary.VisitCount,
            summary.ApproximateVisitCount,
            summary.VisitCount == 0 ? 0d : summary.ApproximateVisitCount / (double)summary.VisitCount,
            includesRides ? summary.RideOutcomes.CompletedRideCount : null,
            includesRides ? summary.DistinctCompletedItemCount : null,
            policy.Includes(ShareContentField.MissedItems)
                ? rides.Where(static value => value.Status != RideOccurrenceStatus.Completed)
                    .Select(static value => value.ParkItemId)
                    .Distinct(StringComparer.Ordinal)
                    .LongCount()
                : null,
            includesRides
                ? summary.CategoryCoverage
                    .Where(static value => value.Category is not null)
                    .Select(static value => value.Category!)
                    .ToArray()
                : Array.Empty<string>(),
            includesRatings
                ? new YearRecapShareRatingSummaryResult(
                    summary.RatedVisitCount,
                    summary.VisitCount,
                    summary.ParkRatings?.Average)
                : null,
            includesRatings
                ? new YearRecapShareRatingSummaryResult(
                    summary.RatedRideCount,
                    summary.RideOutcomes.CompletedRideCount,
                    summary.RideRatings?.Average)
                : null,
            parks,
            mostRepeated,
            topRated,
            includesRatings
                ? BuildRatingEvolution(completed, historicalNames, targets)
                : null,
            nowClosed,
            policy.Includes(ShareContentField.PublicCaption) ? input.PublicCaption : null,
            hasIncompleteCatalog,
            YearRecapShareVersion.CalculationVersion,
            summary.VisitCount == 0);
    }

    private static YearRecapShareTrendResult? BuildRatingEvolution(
        IEnumerable<PassportRideStatisticsObservation> completedRides,
        IReadOnlyDictionary<string, string?> historicalNames,
        IReadOnlyDictionary<string, VisitTarget> targets)
    {
        List<YearRecapShareTrendResult> trends = new List<YearRecapShareTrendResult>();
        foreach (IGrouping<string, PassportRideStatisticsObservation> group in completedRides
                     .GroupBy(static value => value.ParkItemId, StringComparer.Ordinal))
        {
            PassportItemRideObservation[] observations = group
                .Select(static value => new PassportItemRideObservation(
                    value.RideOccurrenceId,
                    value.VisitId,
                    value.VisitDate,
                    0,
                    value.Assessment))
                .ToArray();
            PassportRatingTrend? trend = PassportItemStatisticsCalculator.Calculate(
                observations,
                null).Trend;
            string? name = ResolveName(group.Key, historicalNames, targets);
            if (trend is null || name is null)
            {
                continue;
            }

            trends.Add(new YearRecapShareTrendResult(
                name,
                trend.Kind.ToString(),
                trend.FirstWindowRatingCount,
                trend.LastWindowRatingCount,
                trend.FirstWindowAverage,
                trend.LastWindowAverage,
                trend.Delta));
        }

        return trends.OrderByDescending(static value => Math.Abs(value.Delta))
            .ThenBy(static value => value.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static YearRecapShareHighlightResult? BuildHighlight(
        IGrouping<string, PassportRideStatisticsObservation> group,
        IReadOnlyDictionary<string, string?> historicalNames,
        IReadOnlyDictionary<string, VisitTarget> targets,
        bool forceClosed = false)
    {
        string? name = ResolveName(group.Key, historicalNames, targets);
        if (name is null)
        {
            return null;
        }

        PassportRideStatisticsObservation[] rides = group.ToArray();
        PassportRideStatisticsObservation[] rated = rides
            .Where(static value => value.Assessment.HasValue)
            .ToArray();
        return new YearRecapShareHighlightResult(
            name,
            rides.LongLength,
            rated.LongLength,
            rated.Length == 0
                ? null
                : rated.Average(static value => value.Assessment!.Value.DoubleValue),
            forceClosed);
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
            && !string.IsNullOrWhiteSpace(historicalName)
            ? historicalName.Trim()
            : null;
    }

    private static bool IsNowClosed(
        string parkItemId,
        DateOnly today,
        IReadOnlyDictionary<string, VisitTarget> targets)
    {
        return !targets.TryGetValue(parkItemId, out VisitTarget? target)
            || target.ClosingDate.HasValue && target.ClosingDate.Value <= today;
    }
}
