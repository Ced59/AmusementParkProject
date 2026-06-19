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

    public ParkWeatherRefreshOrchestrator(
        IParkRepository parkRepository,
        IParkWeatherRepository weatherRepository,
        IParkWeatherRunRepository runRepository,
        IParkWeatherProviderStrategyResolver providerStrategyResolver,
        IParkWeatherRefreshSettings settings,
        IParkWeatherCacheInvalidator cacheInvalidator)
    {
        this.parkRepository = parkRepository;
        this.weatherRepository = weatherRepository;
        this.runRepository = runRepository;
        this.providerStrategyResolver = providerStrategyResolver;
        this.settings = settings;
        this.cacheInvalidator = cacheInvalidator;
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

        try
        {
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

            await this.CleanupExpiredForecastsAsync(cancellationToken);
            await this.cacheInvalidator.InvalidateUpdatedWeatherAsync(updatedParks, cancellationToken);

            run.CompletedAtUtc = DateTime.UtcNow;
            run.Status = run.FailedParkCount == 0
                ? ParkWeatherRunStatus.Completed
                : ParkWeatherRunStatus.CompletedWithFailures;
            run.Message = run.FailedParkCount == 0
                ? "Weather refresh completed."
                : "Weather refresh completed with failures.";

            await this.runRepository.UpdateAsync(run, cancellationToken);
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

            await this.weatherRepository.UpsertSnapshotsAsync(providerResult.Snapshots, cancellationToken);
            await this.CleanupForecastsCoveredByObservationsAsync(item.ParkId, providerResult.Snapshots, cancellationToken);

            item.Status = ParkWeatherRunItemStatus.Succeeded;
            item.CompletedAtUtc = DateTime.UtcNow;
            item.ForecastDayCount = providerResult.Snapshots.Count(static snapshot => snapshot.DataKind == ParkWeatherDataKind.Forecast);
            item.ObservationDayCount = providerResult.Snapshots.Count(static snapshot => snapshot.DataKind == ParkWeatherDataKind.Observation);
            item.WarningMessage = string.Join(" ", providerResult.Warnings.Where(static warning => !string.IsNullOrWhiteSpace(warning)));
            await this.runRepository.UpsertItemAsync(item, cancellationToken);

            run.SucceededParkCount += 1;
            if (!string.IsNullOrWhiteSpace(item.WarningMessage))
            {
                run.WarningParkCount += 1;
            }

            await this.runRepository.UpdateAsync(run, cancellationToken);
            return providerResult.Snapshots.Count > 0;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
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

    private async Task CleanupExpiredForecastsAsync(CancellationToken cancellationToken)
    {
        int retentionDays = Math.Clamp(this.settings.ForecastPastRetentionDays, 0, 30);
        DateOnly oldestLocalDateToKeep = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-retentionDays);
        await this.weatherRepository.DeleteExpiredForecastsAsync(oldestLocalDateToKeep, cancellationToken);
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
}
