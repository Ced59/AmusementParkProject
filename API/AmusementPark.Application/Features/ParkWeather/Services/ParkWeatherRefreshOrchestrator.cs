using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Services;

public sealed class ParkWeatherRefreshOrchestrator
{
    private readonly IParkRepository parkRepository;
    private readonly IParkWeatherRepository weatherRepository;
    private readonly IParkWeatherRunRepository runRepository;
    private readonly IParkWeatherProviderStrategyResolver providerStrategyResolver;
    private readonly IParkWeatherRefreshSettings settings;
    private readonly IParkWeatherCacheInvalidator cacheInvalidator;
    private readonly IParkWeatherNotificationService notificationService;
    private readonly ParkWeatherHistoricalComparisonDateResolver historicalComparisonDateResolver;

    public ParkWeatherRefreshOrchestrator(
        IParkRepository parkRepository,
        IParkWeatherRepository weatherRepository,
        IParkWeatherRunRepository runRepository,
        IParkWeatherProviderStrategyResolver providerStrategyResolver,
        IParkWeatherRefreshSettings settings,
        IParkWeatherCacheInvalidator cacheInvalidator,
        IParkWeatherNotificationService notificationService,
        ParkWeatherHistoricalComparisonDateResolver historicalComparisonDateResolver)
    {
        this.parkRepository = parkRepository;
        this.weatherRepository = weatherRepository;
        this.runRepository = runRepository;
        this.providerStrategyResolver = providerStrategyResolver;
        this.settings = settings;
        this.cacheInvalidator = cacheInvalidator;
        this.notificationService = notificationService;
        this.historicalComparisonDateResolver = historicalComparisonDateResolver;
    }

    public async Task ProcessRunAsync(string runId, CancellationToken cancellationToken)
    {
        ParkWeatherRun? run = await this.runRepository.GetByIdAsync(runId, cancellationToken);
        if (run is null)
        {
            return;
        }

        run.Status = ParkWeatherRunStatus.Running;
        run.StartedAtUtc = DateTime.UtcNow;
        run.Message = "Weather refresh running.";
        await this.runRepository.UpdateAsync(run, cancellationToken);
        List<string> runWarnings = new List<string>();

        try
        {
            await this.NotifyAutomaticRunStartedSafelyAsync(run, runWarnings, cancellationToken);
            IReadOnlyCollection<Park> parks = await this.ResolveTargetParksAsync(run, cancellationToken);
            run.TotalParkCount = parks.Count;
            await this.runRepository.UpdateAsync(run, cancellationToken);

            IParkWeatherProviderStrategy providerStrategy = this.providerStrategyResolver.Resolve();
            List<Park> updatedParks = new List<Park>();
            foreach (Park park in parks)
            {
                bool hasUpdatedWeather = await this.ProcessParkAsync(run, park, providerStrategy, cancellationToken);
                if (hasUpdatedWeather)
                {
                    updatedParks.Add(park);
                }

                await this.DelayBetweenParksAsync(cancellationToken);
            }

            await this.CleanupExpiredWeatherDataSafelyAsync(runWarnings, cancellationToken);
            await this.InvalidateUpdatedWeatherSafelyAsync(updatedParks, runWarnings, cancellationToken);

            run.CompletedAtUtc = DateTime.UtcNow;
            run.Status = run.FailedParkCount == 0 && runWarnings.Count == 0
                ? ParkWeatherRunStatus.Completed
                : ParkWeatherRunStatus.CompletedWithFailures;
            run.Message = BuildCompletionMessage(run, runWarnings);

            await this.runRepository.UpdateAsync(run, cancellationToken);
            await this.NotifyAutomaticRunCompletedSafelyAsync(run, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            run.CompletedAtUtc = DateTime.UtcNow;
            run.Status = ParkWeatherRunStatus.Failed;
            run.Message = "Weather refresh canceled.";
            await this.runRepository.UpdateAsync(run, CancellationToken.None);
            throw;
        }
        catch (Exception exception)
        {
            run.CompletedAtUtc = DateTime.UtcNow;
            run.Status = ParkWeatherRunStatus.Failed;
            run.Message = SanitizeMessage(exception.Message);
            await this.runRepository.UpdateAsync(run, cancellationToken);
            await this.NotifyAutomaticRunCompletedSafelyAsync(run, cancellationToken);
        }
    }

    private async Task NotifyAutomaticRunStartedSafelyAsync(ParkWeatherRun run, List<string> runWarnings, CancellationToken cancellationToken)
    {
        if (run.Trigger == ParkWeatherRunTrigger.Automatic)
        {
            try
            {
                await this.notificationService.NotifyRunStartedAsync(run, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                runWarnings.Add($"Start notification failed: {SanitizeMessage(exception.Message)}");
            }
        }
    }

    private async Task NotifyAutomaticRunCompletedSafelyAsync(ParkWeatherRun run, CancellationToken cancellationToken)
    {
        if (run.Trigger == ParkWeatherRunTrigger.Automatic)
        {
            try
            {
                await this.notificationService.NotifyRunCompletedAsync(run, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // The run status is already persisted; notification failures must stay isolated.
            }
        }
    }

    private async Task<IReadOnlyCollection<Park>> ResolveTargetParksAsync(ParkWeatherRun run, CancellationToken cancellationToken)
    {
        if (run.Scope == ParkWeatherRefreshScope.FullVisibleParks)
        {
            return await this.parkRepository.GetVisibleWithValidCoordinatesAsync(cancellationToken);
        }

        if (run.Scope == ParkWeatherRefreshScope.SinglePark)
        {
            Park? park = await this.parkRepository.GetByIdAsync(run.TargetParkId ?? string.Empty, includeHidden: false, cancellationToken);
            if (park is null || !HasValidCoordinates(park))
            {
                return Array.Empty<Park>();
            }

            return new[] { park };
        }

        IReadOnlyCollection<ParkWeatherRunItem> failedItems = await this.runRepository.GetRunItemsAsync(run.SourceRunId ?? string.Empty, ParkWeatherRunItemStatus.Failed, cancellationToken);
        IReadOnlyCollection<Park> parks = await this.parkRepository.GetByIdsAsync(
            failedItems.Select(static item => item.ParkId),
            cancellationToken);

        return parks
            .Where(static park => park.IsVisible)
            .Where(HasValidCoordinates)
            .ToList();
    }

    private async Task<bool> ProcessParkAsync(
        ParkWeatherRun run,
        Park park,
        IParkWeatherProviderStrategy providerStrategy,
        CancellationToken cancellationToken)
    {
        ParkWeatherRunItem item = new ParkWeatherRunItem
        {
            RunId = run.Id ?? string.Empty,
            ParkId = park.Id ?? string.Empty,
            ParkName = park.Name,
            Status = ParkWeatherRunItemStatus.Running,
            AttemptCount = 1,
            StartedAtUtc = DateTime.UtcNow,
        };

        await this.runRepository.UpsertItemAsync(item, cancellationToken);

        try
        {
            ParkWeatherProviderResult providerResult = await providerStrategy.FetchDailyForecastAsync(
                park,
                Math.Max(1, this.settings.ForecastDays),
                this.settings.IncludeYesterdayObservation,
                cancellationToken);

            List<ParkWeatherDailySnapshot> snapshots = new List<ParkWeatherDailySnapshot>(providerResult.Snapshots);
            List<string> warnings = new List<string>(providerResult.Warnings);

            IReadOnlyCollection<ParkWeatherDailySnapshot> historicalSnapshots = await this.FetchMissingHistoricalComparisonObservationsAsync(
                park,
                providerStrategy,
                snapshots,
                warnings,
                cancellationToken);
            snapshots.AddRange(historicalSnapshots);

            await this.weatherRepository.UpsertSnapshotsAsync(snapshots, cancellationToken);
            await this.CleanupForecastsCoveredByObservationsAsync(item.ParkId, snapshots, cancellationToken);

            item.Status = ParkWeatherRunItemStatus.Succeeded;
            item.CompletedAtUtc = DateTime.UtcNow;
            item.ForecastDayCount = snapshots.Count(static snapshot => snapshot.DataKind == ParkWeatherDataKind.Forecast);
            item.ObservationDayCount = snapshots.Count(static snapshot => snapshot.DataKind == ParkWeatherDataKind.Observation);
            item.WarningMessage = string.Join(" ", warnings.Where(static warning => !string.IsNullOrWhiteSpace(warning)));
            await this.runRepository.UpsertItemAsync(item, cancellationToken);

            run.SucceededParkCount += 1;
            if (!string.IsNullOrWhiteSpace(item.WarningMessage))
            {
                run.WarningParkCount += 1;
            }

            await this.runRepository.UpdateAsync(run, cancellationToken);
            return snapshots.Count > 0;
        }
        catch (Exception exception) when (ShouldHandleParkFailure(exception, cancellationToken))
        {
            item.Status = ParkWeatherRunItemStatus.Failed;
            item.CompletedAtUtc = DateTime.UtcNow;
            item.ErrorCode = "park-weather.provider.failed";
            item.ErrorMessage = SanitizeMessage(exception.Message);
            await this.runRepository.UpsertItemAsync(item, cancellationToken);

            run.FailedParkCount += 1;
        }

        await this.runRepository.UpdateAsync(run, cancellationToken);
        return false;
    }

    private async Task DelayBetweenParksAsync(CancellationToken cancellationToken)
    {
        int delayMilliseconds = Math.Max(0, this.settings.DelayBetweenParksMilliseconds);
        if (delayMilliseconds > 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(delayMilliseconds), cancellationToken);
        }
    }

    private async Task CleanupForecastsCoveredByObservationsAsync(string parkId, IReadOnlyCollection<ParkWeatherDailySnapshot> snapshots, CancellationToken cancellationToken)
    {
        List<DateOnly> observationDates = snapshots
            .Where(static snapshot => snapshot.DataKind == ParkWeatherDataKind.Observation)
            .Select(static snapshot => snapshot.LocalDate)
            .Distinct()
            .ToList();

        if (observationDates.Count > 0)
        {
            await this.weatherRepository.DeleteForecastsCoveredByObservationsAsync(parkId, observationDates, cancellationToken);
        }
    }

    private async Task<IReadOnlyCollection<ParkWeatherDailySnapshot>> FetchMissingHistoricalComparisonObservationsAsync(
        Park park,
        IParkWeatherProviderStrategy providerStrategy,
        IReadOnlyCollection<ParkWeatherDailySnapshot> currentSnapshots,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        int historicalBackfillYears = Math.Clamp(this.settings.HistoricalBackfillYears, 0, 3);
        if (historicalBackfillYears == 0)
        {
            return Array.Empty<ParkWeatherDailySnapshot>();
        }

        List<DateOnly> forecastDates = currentSnapshots
            .Where(static snapshot => snapshot.DataKind == ParkWeatherDataKind.Forecast)
            .Select(static snapshot => snapshot.LocalDate)
            .Distinct()
            .OrderBy(static date => date)
            .ToList();

        IReadOnlyCollection<DateOnly> comparisonDates = this.historicalComparisonDateResolver.ResolveComparisonDates(forecastDates, historicalBackfillYears);
        if (comparisonDates.Count == 0)
        {
            return Array.Empty<ParkWeatherDailySnapshot>();
        }

        IReadOnlyCollection<DateOnly> existingObservationDates = await this.weatherRepository.GetExistingObservationDatesAsync(
            park.Id ?? string.Empty,
            comparisonDates,
            cancellationToken);
        HashSet<DateOnly> existingObservationDateSet = existingObservationDates.ToHashSet();
        List<DateOnly> missingDates = comparisonDates
            .Where(date => !existingObservationDateSet.Contains(date))
            .OrderBy(static date => date)
            .ToList();

        if (missingDates.Count == 0)
        {
            return Array.Empty<ParkWeatherDailySnapshot>();
        }

        try
        {
            ParkWeatherProviderResult historicalProviderResult = await providerStrategy.FetchDailyObservationsAsync(
                park,
                missingDates,
                cancellationToken);
            warnings.AddRange(historicalProviderResult.Warnings);
            return historicalProviderResult.Snapshots;
        }
        catch (Exception exception) when (ShouldHandleParkFailure(exception, cancellationToken))
        {
            warnings.Add($"Historical comparison observations could not be fetched: {SanitizeMessage(exception.Message)}");
            return Array.Empty<ParkWeatherDailySnapshot>();
        }
    }

    private async Task CleanupExpiredWeatherDataAsync(CancellationToken cancellationToken)
    {
        int retentionDays = Math.Clamp(this.settings.ForecastPastRetentionDays, 0, 30);
        DateOnly oldestLocalDateToKeep = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-retentionDays);
        await this.weatherRepository.DeleteExpiredForecastsAsync(oldestLocalDateToKeep, cancellationToken);

        int historicalRetentionYears = Math.Clamp(this.settings.HistoricalComparisonYearsLimit, 0, 10);
        DateOnly oldestObservationLocalDateToKeep = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-historicalRetentionYears);
        await this.weatherRepository.DeleteExpiredObservationsAsync(oldestObservationLocalDateToKeep, cancellationToken);
    }

    private async Task CleanupExpiredWeatherDataSafelyAsync(List<string> runWarnings, CancellationToken cancellationToken)
    {
        try
        {
            await this.CleanupExpiredWeatherDataAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            runWarnings.Add($"Expired weather cleanup failed: {SanitizeMessage(exception.Message)}");
        }
    }

    private async Task InvalidateUpdatedWeatherSafelyAsync(
        IReadOnlyCollection<Park> updatedParks,
        List<string> runWarnings,
        CancellationToken cancellationToken)
    {
        try
        {
            await this.cacheInvalidator.InvalidateUpdatedWeatherAsync(updatedParks, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            runWarnings.Add($"Weather cache invalidation failed: {SanitizeMessage(exception.Message)}");
        }
    }

    private static bool HasValidCoordinates(Park park)
    {
        return park.Position is not null
            && (park.Position.Latitude != 0d || park.Position.Longitude != 0d);
    }

    private static string SanitizeMessage(string message)
    {
        string normalizedMessage = string.IsNullOrWhiteSpace(message) ? "Unexpected weather refresh error." : message.Trim();
        return normalizedMessage.Length <= 300 ? normalizedMessage : normalizedMessage[..300];
    }

    private static bool ShouldHandleParkFailure(Exception exception, CancellationToken cancellationToken)
    {
        return exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested;
    }

    private static string BuildCompletionMessage(ParkWeatherRun run, IReadOnlyCollection<string> runWarnings)
    {
        string message = run.FailedParkCount > 0
            ? "Weather refresh completed with failures."
            : runWarnings.Count > 0
                ? "Weather refresh completed with warnings."
                : "Weather refresh completed.";

        if (runWarnings.Count == 0)
        {
            return message;
        }

        return SanitizeMessage($"{message} {string.Join(" ", runWarnings)}");
    }
}
