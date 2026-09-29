using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class PublicLiveHistoryReader
{
    private static readonly TimeSpan DefaultPeriod = TimeSpan.FromDays(30);
    private static readonly TimeSpan MaximumPeriod = TimeSpan.FromDays(90);
    private static readonly TimeSpan MinimumPeriod = TimeSpan.FromHours(1);
    private static readonly TimeSpan AcceptedFutureSkew = TimeSpan.FromMinutes(5);

    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly ILiveHistoryStatisticsRepository historyRepository;
    private readonly ILiveTargetMappingRepository mappingRepository;
    private readonly ILiveDataSourceCatalog sourceCatalog;
    private readonly ILiveOperationalGate operationalGate;
    private readonly LiveWaitHistoryStatisticsCalculator calculator;
    private readonly TimeProvider timeProvider;

    public PublicLiveHistoryReader(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        ILiveHistoryStatisticsRepository historyRepository,
        ILiveTargetMappingRepository mappingRepository,
        ILiveDataSourceCatalog sourceCatalog,
        ILiveOperationalGate operationalGate,
        LiveWaitHistoryStatisticsCalculator calculator,
        TimeProvider? timeProvider = null)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.historyRepository = historyRepository;
        this.mappingRepository = mappingRepository;
        this.sourceCatalog = sourceCatalog;
        this.operationalGate = operationalGate;
        this.calculator = calculator;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PublicLiveHistoryResult>> ReadParkItemAsync(
        string parkItemId,
        DateTimeOffset? requestedFrom,
        DateTimeOffset? requestedTo,
        string? bucket,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(bucket)
            && !string.Equals(bucket.Trim(), "hour", StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult<PublicLiveHistoryResult>.Failure(
                LiveDataApplicationErrors.InvalidHistoryPeriod(
                    "Only hourly live history is currently available."));
        }

        if (!this.sourceCatalog.IsPublicReadEnabled)
        {
            return ApplicationResult<PublicLiveHistoryResult>.Failure(
                LiveDataApplicationErrors.PublicReadDisabled());
        }

        LivePollingTarget? publicTarget = this.sourceCatalog.PublicPollingTarget;
        LiveDataSourcePresentation? presentation = publicTarget is null
            ? null
            : this.sourceCatalog.Find(publicTarget.SourceId);
        LiveHistoryRetentionPolicy? retentionPolicy = presentation?.Source.HistoryRetentionPolicy;
        if (publicTarget is null
            || presentation is null
            || presentation.Source.Status != LiveDataSourceStatus.Active
            || !presentation.Source.UsagePolicy.HistoricalStorageAllowed
            || retentionPolicy is null)
        {
            return ApplicationResult<PublicLiveHistoryResult>.Failure(
                LiveDataApplicationErrors.PublicReadDisabled());
        }

        string? normalizedParkItemId = NormalizeIdentifier(parkItemId);
        if (normalizedParkItemId is null)
        {
            return ApplicationResult<PublicLiveHistoryResult>.Failure(
                ApplicationErrors.Required("parkItemId"));
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        DateTime toUtc = requestedTo?.UtcDateTime ?? nowUtc;
        DateTime fromUtc = requestedFrom?.UtcDateTime ?? toUtc.Subtract(DefaultPeriod);
        TimeSpan duration = toUtc - fromUtc;
        if (duration < MinimumPeriod
            || duration > MaximumPeriod
            || toUtc > nowUtc.Add(AcceptedFutureSkew))
        {
            return ApplicationResult<PublicLiveHistoryResult>.Failure(
                LiveDataApplicationErrors.InvalidHistoryPeriod(
                    "The live history period must be between one hour and ninety days and cannot be in the future."));
        }

        ParkItem? item = await this.parkItemRepository.GetByIdAsync(
            normalizedParkItemId,
            false,
            cancellationToken);
        if (item is null)
        {
            return ApplicationResult<PublicLiveHistoryResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(ParkItem), normalizedParkItemId));
        }

        Park? park = await this.parkRepository.GetByIdAsync(item.ParkId, false, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<PublicLiveHistoryResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(ParkItem), normalizedParkItemId));
        }

        LiveOperationalGateSnapshot gate = await this.operationalGate.LoadAsync(
            publicTarget.SourceId,
            publicTarget.ExternalEntityId,
            cancellationToken);
        if (!gate.AllowsPublicRead(item.ParkId, LiveTargetType.ParkItem, normalizedParkItemId))
        {
            return ApplicationResult<PublicLiveHistoryResult>.Failure(
                LiveDataApplicationErrors.PublicReadTemporarilySuspended());
        }

        IReadOnlyCollection<LivePublicTargetCoverage> coverage =
            await this.mappingRepository.GetEligiblePublicTargetCoverageByParkAsync(
                publicTarget.SourceId,
                publicTarget.ExternalEntityId,
                item.ParkId,
                cancellationToken);
        string[] eligibleExternalTargetIds = coverage
            .Where(candidate => string.Equals(
                candidate.InternalTargetId,
                normalizedParkItemId,
                StringComparison.Ordinal))
            .Select(static candidate => candidate.ExternalTargetId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (eligibleExternalTargetIds.Length == 0)
        {
            return ApplicationResult<PublicLiveHistoryResult>.Failure(
                LiveDataApplicationErrors.PublicReadDisabled());
        }

        IReadOnlyCollection<LiveWaitHistoryObservation> observations =
            await this.historyRepository.GetAsync(
                publicTarget.SourceId,
                LiveTargetType.ParkItem,
                normalizedParkItemId,
                presentation.Source.UsagePolicy.Version,
                retentionPolicy.StorageKey,
                retentionPolicy.BucketDuration,
                fromUtc,
                toUtc,
                cancellationToken);
        HashSet<string> eligibleExternalTargets = eligibleExternalTargetIds
            .ToHashSet(StringComparer.Ordinal);
        IReadOnlyCollection<LiveWaitHistoryObservation> eligibleObservations = observations
            .Where(observation => eligibleExternalTargets.Contains(observation.ExternalTargetId))
            .ToList()
            .AsReadOnly();
        LiveWaitHistoryStatistics statistics = this.calculator.Calculate(
            eligibleObservations,
            fromUtc,
            toUtc,
            publicTarget.ActiveWindow,
            publicTarget.Policy.PollingInterval);
        return ApplicationResult<PublicLiveHistoryResult>.Success(new PublicLiveHistoryResult(
            normalizedParkItemId,
            item.Name,
            park.Id,
            park.Name ?? string.Empty,
            statistics.FromUtc,
            statistics.ToUtc,
            statistics.TimeZoneId,
            statistics.DataStatus,
            statistics.ExpectedObservationCount,
            statistics.ObservationCount,
            statistics.UsableWaitCount,
            statistics.DaysCovered,
            statistics.ComparableDays,
            statistics.CoveragePercent,
            statistics.TruncatedObservationCount,
            new PublicLiveHistoryExclusionsResult(
                statistics.Exclusions.DuplicateObservations,
                statistics.Exclusions.OutsideActiveWindow,
                statistics.Exclusions.NonOperatingStatus,
                statistics.Exclusions.MissingStandbyWait,
                statistics.Exclusions.Total),
            statistics.Hours.Select(static hour => new PublicLiveHistoryHourResult(
                hour.LocalHour,
                hour.DataStatus,
                hour.ExpectedObservationCount,
                hour.ObservationCount,
                hour.UsableWaitCount,
                hour.DaysCovered,
                hour.ComparableDays,
                hour.CoveragePercent,
                hour.RobustMinimumMinutes,
                hour.FirstQuartileMinutes,
                hour.MedianMinutes,
                hour.ThirdQuartileMinutes,
                hour.RobustMaximumMinutes)).ToList().AsReadOnly(),
            new PublicLiveSourceResult(
                presentation.Source.Id.Value,
                presentation.Source.DisplayName,
                presentation.Source.Type,
                presentation.AttributionText,
                presentation.AttributionUrl)));
    }

    private static string? NormalizeIdentifier(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
