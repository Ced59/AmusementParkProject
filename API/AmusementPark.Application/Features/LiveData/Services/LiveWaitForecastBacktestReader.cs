using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LiveWaitForecastBacktestReader
{
    private static readonly TimeSpan DefaultEvaluationPeriod = TimeSpan.FromDays(90);
    private static readonly TimeSpan MinimumEvaluationPeriod = TimeSpan.FromDays(14);
    private static readonly TimeSpan MaximumEvaluationPeriod = TimeSpan.FromDays(180);
    private static readonly TimeSpan AcceptedFutureSkew = TimeSpan.FromMinutes(5);

    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly ILiveHistoryStatisticsRepository historyRepository;
    private readonly ILiveTargetMappingRepository mappingRepository;
    private readonly ILiveDataSourceCatalog sourceCatalog;
    private readonly LiveWaitForecastBacktestCalculator calculator;
    private readonly LiveWaitForecastBacktestPolicy policy;
    private readonly TimeProvider timeProvider;

    public LiveWaitForecastBacktestReader(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        ILiveHistoryStatisticsRepository historyRepository,
        ILiveTargetMappingRepository mappingRepository,
        ILiveDataSourceCatalog sourceCatalog,
        LiveWaitForecastBacktestCalculator calculator,
        LiveWaitForecastBacktestPolicy policy,
        TimeProvider? timeProvider = null)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.historyRepository = historyRepository;
        this.mappingRepository = mappingRepository;
        this.sourceCatalog = sourceCatalog;
        this.calculator = calculator;
        this.policy = policy;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<LiveWaitForecastBacktestResult>> ReadAsync(
        string parkItemId,
        DateTimeOffset? requestedFrom,
        DateTimeOffset? requestedTo,
        CancellationToken cancellationToken)
    {
        string? normalizedParkItemId = NormalizeIdentifier(parkItemId);
        if (normalizedParkItemId is null)
        {
            return ApplicationResult<LiveWaitForecastBacktestResult>.Failure(
                ApplicationErrors.Required("parkItemId"));
        }

        LivePollingTarget? pollingTarget = this.sourceCatalog.ConfiguredPollingTarget;
        LiveDataSourcePresentation? presentation = pollingTarget is null
            ? null
            : this.sourceCatalog.Find(pollingTarget.SourceId);
        LiveHistoryRetentionPolicy? retentionPolicy = presentation?.Source.HistoryRetentionPolicy;
        if (pollingTarget is null
            || presentation is null
            || !presentation.Source.UsagePolicy.HistoricalStorageAllowed
            || retentionPolicy is null)
        {
            return ApplicationResult<LiveWaitForecastBacktestResult>.Failure(
                LiveDataApplicationErrors.OperationsUnavailable());
        }

        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        DateTime toUtc = requestedTo?.UtcDateTime ?? nowUtc;
        if (!requestedFrom.HasValue && toUtc.Ticks < DefaultEvaluationPeriod.Ticks)
        {
            return InvalidPeriod();
        }

        DateTime fromUtc = requestedFrom?.UtcDateTime ?? toUtc.Subtract(DefaultEvaluationPeriod);
        TimeSpan evaluationPeriod = toUtc - fromUtc;
        if (evaluationPeriod < MinimumEvaluationPeriod
            || evaluationPeriod > MaximumEvaluationPeriod
            || toUtc > nowUtc.Add(AcceptedFutureSkew)
            || fromUtc.Ticks < TimeSpan.FromDays(this.policy.TrainingWindowDays).Ticks)
        {
            return InvalidPeriod();
        }

        ParkItem? item = await this.parkItemRepository.GetByIdAsync(
            normalizedParkItemId,
            false,
            cancellationToken);
        if (item is null)
        {
            return ApplicationResult<LiveWaitForecastBacktestResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(ParkItem), normalizedParkItemId));
        }

        Park? park = await this.parkRepository.GetByIdAsync(item.ParkId, false, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<LiveWaitForecastBacktestResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(ParkItem), normalizedParkItemId));
        }

        IReadOnlyCollection<LivePublicTargetCoverage> coverage =
            await this.mappingRepository.GetEligiblePublicTargetCoverageByParkAsync(
                pollingTarget.SourceId,
                pollingTarget.ExternalEntityId,
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
            return ApplicationResult<LiveWaitForecastBacktestResult>.Failure(
                LiveDataApplicationErrors.BacktestUnavailable());
        }

        DateTime historyFromUtc = fromUtc.AddDays(-this.policy.TrainingWindowDays);
        IReadOnlyCollection<LiveWaitHistoryObservation> observations =
            await this.historyRepository.GetAsync(
                pollingTarget.SourceId,
                LiveTargetType.ParkItem,
                normalizedParkItemId,
                presentation.Source.UsagePolicy.Version,
                retentionPolicy.StorageKey,
                retentionPolicy.BucketDuration,
                historyFromUtc,
                toUtc,
                cancellationToken);
        IReadOnlyCollection<LiveWaitHistoryObservation> eligibleObservations = observations
            .Where(observation => eligibleExternalTargets.Contains(observation.ExternalTargetId))
            .ToList()
            .AsReadOnly();
        LiveWaitForecastBacktestReport report = this.calculator.Calculate(
            eligibleObservations,
            fromUtc,
            toUtc,
            pollingTarget.ActiveWindow);
        return ApplicationResult<LiveWaitForecastBacktestResult>.Success(
            new LiveWaitForecastBacktestResult(
                item.Name,
                park.Name ?? string.Empty,
                report,
                nowUtc));
    }

    private static ApplicationResult<LiveWaitForecastBacktestResult> InvalidPeriod()
    {
        return ApplicationResult<LiveWaitForecastBacktestResult>.Failure(
            LiveDataApplicationErrors.InvalidBacktestPeriod(
                "The backtest evaluation period must be between fourteen and one hundred eighty days and cannot be in the future."));
    }

    private static string? NormalizeIdentifier(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
