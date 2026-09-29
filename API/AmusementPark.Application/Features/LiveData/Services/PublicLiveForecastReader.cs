using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class PublicLiveForecastReader
{
    private static readonly TimeSpan EvaluationPeriod = TimeSpan.FromDays(90);

    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly ILiveHistoryStatisticsRepository historyRepository;
    private readonly ILiveLatestObservationRepository latestObservationRepository;
    private readonly ILiveTargetMappingRepository mappingRepository;
    private readonly ILiveDataSourceCatalog sourceCatalog;
    private readonly ILiveOperationalGate operationalGate;
    private readonly IPublicLiveForecastComputationCache computationCache;
    private readonly LiveWaitForecastBacktestCalculator backtestCalculator;
    private readonly LiveWaitForecastCalculator forecastCalculator;
    private readonly LiveWaitForecastBacktestPolicy policy;
    private readonly LiveLatestObservationSelectionPolicy latestSelectionPolicy;
    private readonly TimeProvider timeProvider;

    public PublicLiveForecastReader(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        ILiveHistoryStatisticsRepository historyRepository,
        ILiveLatestObservationRepository latestObservationRepository,
        ILiveTargetMappingRepository mappingRepository,
        ILiveDataSourceCatalog sourceCatalog,
        ILiveOperationalGate operationalGate,
        IPublicLiveForecastComputationCache computationCache,
        LiveWaitForecastBacktestCalculator backtestCalculator,
        LiveWaitForecastCalculator forecastCalculator,
        LiveWaitForecastBacktestPolicy policy,
        LiveLatestObservationSelectionPolicy latestSelectionPolicy,
        TimeProvider? timeProvider = null)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.historyRepository = historyRepository;
        this.latestObservationRepository = latestObservationRepository;
        this.mappingRepository = mappingRepository;
        this.sourceCatalog = sourceCatalog;
        this.operationalGate = operationalGate;
        this.computationCache = computationCache;
        this.backtestCalculator = backtestCalculator;
        this.forecastCalculator = forecastCalculator;
        this.policy = policy;
        this.latestSelectionPolicy = latestSelectionPolicy;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<PublicLiveForecastResult>> ReadParkItemAsync(
        string parkItemId,
        CancellationToken cancellationToken)
    {
        string? normalizedParkItemId = NormalizeIdentifier(parkItemId);
        if (normalizedParkItemId is null)
        {
            return ApplicationResult<PublicLiveForecastResult>.Failure(
                ApplicationErrors.Required("parkItemId"));
        }

        if (!this.sourceCatalog.IsPublicReadEnabled)
        {
            return Unavailable();
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
            return Unavailable();
        }

        ParkItem? item = await this.parkItemRepository.GetByIdAsync(
            normalizedParkItemId,
            false,
            cancellationToken);
        if (item is null)
        {
            return ApplicationResult<PublicLiveForecastResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(ParkItem), normalizedParkItemId));
        }

        Park? park = await this.parkRepository.GetByIdAsync(item.ParkId, false, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<PublicLiveForecastResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(ParkItem), normalizedParkItemId));
        }

        LiveOperationalGateSnapshot gate = await this.operationalGate.LoadAsync(
            publicTarget.SourceId,
            publicTarget.ExternalEntityId,
            cancellationToken);
        if (!gate.AllowsPublicRead(item.ParkId, LiveTargetType.ParkItem, normalizedParkItemId))
        {
            return Unavailable();
        }

        IReadOnlyCollection<LivePublicTargetCoverage> coverage =
            await this.mappingRepository.GetEligiblePublicTargetCoverageByParkAsync(
                publicTarget.SourceId,
                publicTarget.ExternalEntityId,
                item.ParkId,
                cancellationToken);
        HashSet<string> eligibleExternalTargets = coverage
            .Where(candidate => string.Equals(
                candidate.InternalTargetId,
                normalizedParkItemId,
                StringComparison.Ordinal))
            .Select(static candidate => candidate.ExternalTargetId)
            .ToHashSet(StringComparer.Ordinal);
        if (eligibleExternalTargets.Count == 0)
        {
            return Unavailable();
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyCollection<LiveLatestObservation> latestObservations =
            await this.latestObservationRepository.GetByTargetAsync(
                LiveTargetType.ParkItem,
                normalizedParkItemId,
                item.ParkId,
                cancellationToken);
        IReadOnlyCollection<LiveLatestObservation> coveredLatestObservations = latestObservations
            .Where(observation => observation.Provenance.SourceId == publicTarget.SourceId
                && eligibleExternalTargets.Contains(observation.Provenance.ExternalTargetId))
            .ToList()
            .AsReadOnly();
        LiveLatestObservation? latestObservation = this.latestSelectionPolicy.Select(
            coveredLatestObservations,
            new Dictionary<LiveDataSourceId, int>
            {
                [publicTarget.SourceId] = presentation.Priority,
            },
            nowUtc);
        LiveFreshnessAssessment? latestFreshness = latestObservation?.FreshnessPolicy.Assess(
            latestObservation.Provenance.ObservedAtUtc,
            nowUtc);
        if (latestObservation is null
            || latestFreshness is null
            || !latestFreshness.CanBePresentedAsCurrent
            || !latestFreshness.ExpiresAtUtc.HasValue
            || latestObservation.Status is not (LiveOperationalStatus.Open
                or LiveOperationalStatus.OperatingWithLimitations))
        {
            return Unavailable();
        }

        string computationCacheKey = BuildComputationCacheKey(
            publicTarget.SourceId,
            normalizedParkItemId,
            presentation.Source.UsagePolicy.Version,
            retentionPolicy.StorageKey,
            eligibleExternalTargets,
            nowUtc);
        PublicLiveForecastComputation? computation =
            await this.computationCache.GetOrCreateAsync(
                computationCacheKey,
                token => this.CalculateComputationAsync(
                    publicTarget,
                    presentation,
                    retentionPolicy,
                    normalizedParkItemId,
                    eligibleExternalTargets,
                    nowUtc,
                    token),
                cancellationToken);
        if (computation is null)
        {
            return Unavailable();
        }

        return ApplicationResult<PublicLiveForecastResult>.Success(
            new PublicLiveForecastResult(
                item.Name,
                park.Name ?? string.Empty,
                computation.TimeZoneId,
                computation.Forecast,
                computation.StudyVersion,
                computation.Method,
                computation.IntervalMethod,
                computation.MeanAbsoluteErrorMinutes,
                computation.IntervalCoveragePercent,
                computation.EvaluationFromUtc,
                computation.EvaluationToUtc,
                computation.EvaluationPointCount,
                latestFreshness.ExpiresAtUtc.Value,
                new PublicLiveSourceResult(
                    presentation.Source.Id.Value,
                    presentation.Source.DisplayName,
                    presentation.Source.Type,
                    presentation.AttributionText,
                    presentation.AttributionUrl)));
    }

    private async Task<PublicLiveForecastComputation?> CalculateComputationAsync(
        LivePollingTarget publicTarget,
        LiveDataSourcePresentation presentation,
        LiveHistoryRetentionPolicy retentionPolicy,
        string parkItemId,
        IReadOnlySet<string> eligibleExternalTargets,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        DateTime evaluationFromUtc = nowUtc.Subtract(EvaluationPeriod);
        DateTime historyFromUtc = evaluationFromUtc.AddDays(-this.policy.TrainingWindowDays);
        IReadOnlyCollection<LiveWaitHistoryObservation> observations =
            await this.historyRepository.GetAsync(
                publicTarget.SourceId,
                LiveTargetType.ParkItem,
                parkItemId,
                presentation.Source.UsagePolicy.Version,
                retentionPolicy.StorageKey,
                retentionPolicy.BucketDuration,
                historyFromUtc,
                nowUtc,
                cancellationToken);
        LiveWaitHistoryObservation[] eligibleObservations = observations
            .Where(observation => eligibleExternalTargets.Contains(observation.ExternalTargetId))
            .ToArray();
        LiveWaitForecastBacktestReport report = this.backtestCalculator.Calculate(
            eligibleObservations,
            evaluationFromUtc,
            nowUtc,
            publicTarget.ActiveWindow);
        if (report.Verdict != LiveWaitForecastBacktestVerdict.EligibleForPilot
            || report.Candidate is null
            || !report.IntervalCoveragePercent.HasValue)
        {
            return null;
        }

        LiveWaitForecast? forecast = this.forecastCalculator.Calculate(
            eligibleObservations,
            nowUtc,
            publicTarget.ActiveWindow);
        return forecast is null
            ? null
            : new PublicLiveForecastComputation(
                report.TimeZoneId,
                forecast,
                report.StudyVersion,
                report.Candidate.Method,
                LiveWaitForecastBacktestPolicy.IntervalMethod,
                report.Candidate.MeanAbsoluteErrorMinutes,
                report.IntervalCoveragePercent.Value,
                report.EvaluationFromUtc,
                report.EvaluationToUtc,
                report.EvaluationPointCount);
    }

    private static string BuildComputationCacheKey(
        LiveDataSourceId sourceId,
        string parkItemId,
        string usagePolicyVersion,
        string storageKey,
        IReadOnlySet<string> eligibleExternalTargets,
        DateTime nowUtc)
    {
        long fifteenMinuteWindow = nowUtc.Ticks / TimeSpan.FromMinutes(15).Ticks;
        string externalTargets = string.Join(
            '\u001f',
            eligibleExternalTargets.Order(StringComparer.Ordinal));
        return string.Join(
            ':',
            "public-live-forecast",
            sourceId.Value,
            parkItemId,
            usagePolicyVersion,
            storageKey,
            fifteenMinuteWindow,
            externalTargets);
    }

    private static ApplicationResult<PublicLiveForecastResult> Unavailable()
    {
        return ApplicationResult<PublicLiveForecastResult>.Failure(
            LiveDataApplicationErrors.ForecastUnavailable());
    }

    private static string? NormalizeIdentifier(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
