using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkWeather.Services;

public sealed class ParkWeatherRefreshOrchestratorTests
{
    [Fact]
    public async Task ProcessRunAsync_WhenFullRunHasSuccessAndFailure_ShouldPersistCleanupAndInvalidateSuccessfulParks()
    {
        DateOnly observationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        Park successPark = CreatePark("park-1", "Magic Park", true, 48.86, 2.35);
        Park failedPark = CreatePark("park-2", "Broken Park", true, 50.63, 3.06);
        ParkWeatherRun run = new ParkWeatherRun
        {
            Id = "run-1",
            Scope = ParkWeatherRefreshScope.FullVisibleParks,
            Status = ParkWeatherRunStatus.Queued,
            Trigger = ParkWeatherRunTrigger.Manual,
        };
        ParkWeatherDailySnapshot observation = CreateSnapshot("park-1", observationDate, ParkWeatherDataKind.Observation);
        ParkWeatherDailySnapshot forecast = CreateSnapshot("park-1", observationDate.AddDays(1), ParkWeatherDataKind.Forecast);
        List<ParkWeatherDailySnapshot> persistedSnapshots = new List<ParkWeatherDailySnapshot>();
        IReadOnlyCollection<DateOnly> cleanedObservationDates = Array.Empty<DateOnly>();
        IReadOnlyCollection<Park> invalidatedParks = Array.Empty<Park>();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetVisibleWithValidCoordinatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { successPark, failedPark });
        Mock<IParkWeatherRepository> weatherRepository = new Mock<IParkWeatherRepository>(MockBehavior.Strict);
        weatherRepository
            .Setup(repository => repository.UpsertSnapshotsAsync(It.IsAny<IReadOnlyCollection<ParkWeatherDailySnapshot>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<ParkWeatherDailySnapshot>, CancellationToken>((snapshots, _) => persistedSnapshots.AddRange(snapshots))
            .Returns(Task.CompletedTask);
        weatherRepository
            .Setup(repository => repository.DeleteForecastsCoveredByObservationsAsync("park-1", It.IsAny<IReadOnlyCollection<DateOnly>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyCollection<DateOnly>, CancellationToken>((_, dates, _) => cleanedObservationDates = dates)
            .Returns(Task.CompletedTask);
        weatherRepository
            .Setup(repository => repository.DeleteExpiredForecastsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        weatherRepository
            .Setup(repository => repository.DeleteExpiredObservationsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IParkWeatherRunRepository> runRepository = CreateRunRepository(run);
        Mock<IParkWeatherProviderStrategy> providerStrategy = new Mock<IParkWeatherProviderStrategy>(MockBehavior.Strict);
        providerStrategy
            .Setup(strategy => strategy.FetchDailyForecastAsync(
                It.Is<Park>(park => park.Id == "park-1"),
                7,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkWeatherProviderResult
            {
                Snapshots = new[] { observation, forecast },
                Warnings = new[] { "Archive data was delayed." },
            });
        providerStrategy
            .Setup(strategy => strategy.FetchDailyForecastAsync(
                It.Is<Park>(park => park.Id == "park-2"),
                7,
                true,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Provider unavailable"));
        Mock<IParkWeatherProviderStrategyResolver> providerStrategyResolver = new Mock<IParkWeatherProviderStrategyResolver>(MockBehavior.Strict);
        providerStrategyResolver
            .Setup(resolver => resolver.Resolve())
            .Returns(providerStrategy.Object);
        Mock<IParkWeatherCacheInvalidator> cacheInvalidator = new Mock<IParkWeatherCacheInvalidator>(MockBehavior.Strict);
        cacheInvalidator
            .Setup(invalidator => invalidator.InvalidateUpdatedWeatherAsync(It.IsAny<IReadOnlyCollection<Park>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Park>, CancellationToken>((parks, _) => invalidatedParks = parks)
            .Returns(Task.CompletedTask);
        ParkWeatherRefreshOrchestrator orchestrator = new ParkWeatherRefreshOrchestrator(
            parkRepository.Object,
            weatherRepository.Object,
            runRepository.Object,
            providerStrategyResolver.Object,
            new TestRefreshSettings(),
            cacheInvalidator.Object,
            new ParkWeatherHistoricalComparisonDateResolver());

        await orchestrator.ProcessRunAsync("run-1", CancellationToken.None);

        Assert.Equal(ParkWeatherRunStatus.CompletedWithFailures, run.Status);
        Assert.Equal(2, run.TotalParkCount);
        Assert.Equal(1, run.SucceededParkCount);
        Assert.Equal(1, run.FailedParkCount);
        Assert.Equal(1, run.WarningParkCount);
        Assert.Equal(new[] { observation, forecast }, persistedSnapshots);
        Assert.Equal(new[] { observationDate }, cleanedObservationDates);
        Assert.Equal(new[] { successPark }, invalidatedParks);
        parkRepository.VerifyAll();
        weatherRepository.VerifyAll();
        runRepository.VerifyAll();
        providerStrategyResolver.VerifyAll();
        providerStrategy.VerifyAll();
        cacheInvalidator.VerifyAll();
    }

    [Fact]
    public async Task ProcessRunAsync_WhenRetryingFailedRun_ShouldKeepOnlyVisibleParksWithCoordinates()
    {
        Park validPark = CreatePark("park-1", "Magic Park", true, 48.86, 2.35);
        Park hiddenPark = CreatePark("park-hidden", "Hidden Park", false, 50.63, 3.06);
        Park invalidCoordinatePark = CreatePark("park-zero", "Zero Park", true, 0d, 0d);
        ParkWeatherRun run = new ParkWeatherRun
        {
            Id = "run-2",
            Scope = ParkWeatherRefreshScope.FailedFromRun,
            SourceRunId = "source-run",
            Status = ParkWeatherRunStatus.Queued,
            Trigger = ParkWeatherRunTrigger.RetryFailed,
        };
        ParkWeatherDailySnapshot forecast = CreateSnapshot("park-1", DateOnly.FromDateTime(DateTime.UtcNow), ParkWeatherDataKind.Forecast);
        IReadOnlyCollection<Park> invalidatedParks = Array.Empty<Park>();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1", "park-hidden", "park-zero" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { validPark, hiddenPark, invalidCoordinatePark });
        Mock<IParkWeatherRepository> weatherRepository = new Mock<IParkWeatherRepository>(MockBehavior.Strict);
        weatherRepository
            .Setup(repository => repository.UpsertSnapshotsAsync(It.Is<IReadOnlyCollection<ParkWeatherDailySnapshot>>(snapshots => snapshots.Single() == forecast), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        weatherRepository
            .Setup(repository => repository.DeleteExpiredForecastsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        weatherRepository
            .Setup(repository => repository.DeleteExpiredObservationsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IParkWeatherRunRepository> runRepository = CreateRunRepository(run);
        runRepository
            .Setup(repository => repository.GetRunItemsAsync("source-run", ParkWeatherRunItemStatus.Failed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new ParkWeatherRunItem { ParkId = "park-1", Status = ParkWeatherRunItemStatus.Failed },
                new ParkWeatherRunItem { ParkId = "park-hidden", Status = ParkWeatherRunItemStatus.Failed },
                new ParkWeatherRunItem { ParkId = "park-zero", Status = ParkWeatherRunItemStatus.Failed },
            });
        Mock<IParkWeatherProviderStrategy> providerStrategy = new Mock<IParkWeatherProviderStrategy>(MockBehavior.Strict);
        providerStrategy
            .Setup(strategy => strategy.FetchDailyForecastAsync(
                It.Is<Park>(park => park.Id == "park-1"),
                7,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkWeatherProviderResult { Snapshots = new[] { forecast } });
        Mock<IParkWeatherProviderStrategyResolver> providerStrategyResolver = new Mock<IParkWeatherProviderStrategyResolver>(MockBehavior.Strict);
        providerStrategyResolver
            .Setup(resolver => resolver.Resolve())
            .Returns(providerStrategy.Object);
        Mock<IParkWeatherCacheInvalidator> cacheInvalidator = new Mock<IParkWeatherCacheInvalidator>(MockBehavior.Strict);
        cacheInvalidator
            .Setup(invalidator => invalidator.InvalidateUpdatedWeatherAsync(It.IsAny<IReadOnlyCollection<Park>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Park>, CancellationToken>((parks, _) => invalidatedParks = parks)
            .Returns(Task.CompletedTask);
        ParkWeatherRefreshOrchestrator orchestrator = new ParkWeatherRefreshOrchestrator(
            parkRepository.Object,
            weatherRepository.Object,
            runRepository.Object,
            providerStrategyResolver.Object,
            new TestRefreshSettings(),
            cacheInvalidator.Object,
            new ParkWeatherHistoricalComparisonDateResolver());

        await orchestrator.ProcessRunAsync("run-2", CancellationToken.None);

        Assert.Equal(ParkWeatherRunStatus.Completed, run.Status);
        Assert.Equal(1, run.TotalParkCount);
        Assert.Equal(1, run.SucceededParkCount);
        Assert.Equal(new[] { validPark }, invalidatedParks);
        parkRepository.VerifyAll();
        weatherRepository.VerifyAll();
        runRepository.VerifyAll();
        providerStrategyResolver.VerifyAll();
        providerStrategy.VerifyAll();
        cacheInvalidator.VerifyAll();
    }

    [Fact]
    public async Task ProcessRunAsync_WhenHistoricalBackfillIsEnabled_ShouldFetchOnlyMissingComparisonObservations()
    {
        Park park = CreatePark("park-1", "Magic Park", true, 48.86, 2.35);
        ParkWeatherRun run = new ParkWeatherRun
        {
            Id = "run-history",
            Scope = ParkWeatherRefreshScope.FullVisibleParks,
            Status = ParkWeatherRunStatus.Queued,
            Trigger = ParkWeatherRunTrigger.Manual,
        };
        ParkWeatherDailySnapshot firstForecast = CreateSnapshot("park-1", new DateOnly(2026, 6, 20), ParkWeatherDataKind.Forecast);
        ParkWeatherDailySnapshot secondForecast = CreateSnapshot("park-1", new DateOnly(2026, 6, 21), ParkWeatherDataKind.Forecast);
        List<ParkWeatherDailySnapshot> persistedSnapshots = new List<ParkWeatherDailySnapshot>();
        IReadOnlyCollection<DateOnly> requestedHistoricalDates = Array.Empty<DateOnly>();
        IReadOnlyCollection<DateOnly> checkedObservationDates = Array.Empty<DateOnly>();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetVisibleWithValidCoordinatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        Mock<IParkWeatherRepository> weatherRepository = new Mock<IParkWeatherRepository>(MockBehavior.Strict);
        weatherRepository
            .Setup(repository => repository.GetExistingObservationDatesAsync("park-1", It.IsAny<IReadOnlyCollection<DateOnly>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyCollection<DateOnly>, CancellationToken>((_, dates, _) => checkedObservationDates = dates)
            .ReturnsAsync(new[] { new DateOnly(2025, 6, 20) });
        weatherRepository
            .Setup(repository => repository.UpsertSnapshotsAsync(It.IsAny<IReadOnlyCollection<ParkWeatherDailySnapshot>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<ParkWeatherDailySnapshot>, CancellationToken>((snapshots, _) => persistedSnapshots.AddRange(snapshots))
            .Returns(Task.CompletedTask);
        weatherRepository
            .Setup(repository => repository.DeleteForecastsCoveredByObservationsAsync("park-1", It.IsAny<IReadOnlyCollection<DateOnly>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        weatherRepository
            .Setup(repository => repository.DeleteExpiredForecastsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        weatherRepository
            .Setup(repository => repository.DeleteExpiredObservationsAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<IParkWeatherRunRepository> runRepository = CreateRunRepository(run);
        Mock<IParkWeatherProviderStrategy> providerStrategy = new Mock<IParkWeatherProviderStrategy>(MockBehavior.Strict);
        providerStrategy
            .Setup(strategy => strategy.FetchDailyForecastAsync(
                park,
                7,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkWeatherProviderResult
            {
                Snapshots = new[] { firstForecast, secondForecast },
            });
        providerStrategy
            .Setup(strategy => strategy.FetchDailyObservationsAsync(
                park,
                It.IsAny<IReadOnlyCollection<DateOnly>>(),
                It.IsAny<CancellationToken>()))
            .Callback<Park, IReadOnlyCollection<DateOnly>, CancellationToken>((_, dates, _) => requestedHistoricalDates = dates)
            .ReturnsAsync((Park _, IReadOnlyCollection<DateOnly> dates, CancellationToken _) => new ParkWeatherProviderResult
            {
                Snapshots = dates.Select(date => CreateSnapshot("park-1", date, ParkWeatherDataKind.Observation)).ToList(),
            });
        Mock<IParkWeatherProviderStrategyResolver> providerStrategyResolver = new Mock<IParkWeatherProviderStrategyResolver>(MockBehavior.Strict);
        providerStrategyResolver
            .Setup(resolver => resolver.Resolve())
            .Returns(providerStrategy.Object);
        Mock<IParkWeatherCacheInvalidator> cacheInvalidator = new Mock<IParkWeatherCacheInvalidator>(MockBehavior.Strict);
        cacheInvalidator
            .Setup(invalidator => invalidator.InvalidateUpdatedWeatherAsync(It.IsAny<IReadOnlyCollection<Park>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        ParkWeatherRefreshOrchestrator orchestrator = new ParkWeatherRefreshOrchestrator(
            parkRepository.Object,
            weatherRepository.Object,
            runRepository.Object,
            providerStrategyResolver.Object,
            new TestRefreshSettings { HistoricalBackfillYears = 3 },
            cacheInvalidator.Object,
            new ParkWeatherHistoricalComparisonDateResolver());

        await orchestrator.ProcessRunAsync("run-history", CancellationToken.None);

        Assert.Contains(new DateOnly(2025, 6, 20), checkedObservationDates);
        Assert.Contains(new DateOnly(2025, 6, 21), checkedObservationDates);
        Assert.Contains(new DateOnly(2024, 6, 20), checkedObservationDates);
        Assert.DoesNotContain(new DateOnly(2025, 6, 20), requestedHistoricalDates);
        Assert.Contains(new DateOnly(2025, 6, 21), requestedHistoricalDates);
        Assert.Contains(new DateOnly(2024, 6, 20), requestedHistoricalDates);
        Assert.Equal(2, persistedSnapshots.Count(static snapshot => snapshot.DataKind == ParkWeatherDataKind.Forecast));
        Assert.Equal(5, persistedSnapshots.Count(static snapshot => snapshot.DataKind == ParkWeatherDataKind.Observation));
        parkRepository.VerifyAll();
        weatherRepository.VerifyAll();
        runRepository.VerifyAll();
        providerStrategyResolver.VerifyAll();
        providerStrategy.VerifyAll();
        cacheInvalidator.VerifyAll();
    }

    private static Mock<IParkWeatherRunRepository> CreateRunRepository(ParkWeatherRun run)
    {
        Mock<IParkWeatherRunRepository> runRepository = new Mock<IParkWeatherRunRepository>(MockBehavior.Strict);
        runRepository
            .Setup(repository => repository.GetByIdAsync(run.Id ?? string.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(run);
        runRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<ParkWeatherRun>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        runRepository
            .Setup(repository => repository.UpsertItemAsync(It.IsAny<ParkWeatherRunItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return runRepository;
    }

    private static Park CreatePark(string id, string name, bool isVisible, double latitude, double longitude)
    {
        Park park = new Park
        {
            Id = id,
            Name = name,
            IsVisible = isVisible,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        park.SetPosition(latitude, longitude);
        return park;
    }

    private static ParkWeatherDailySnapshot CreateSnapshot(string parkId, DateOnly localDate, ParkWeatherDataKind dataKind)
    {
        return new ParkWeatherDailySnapshot
        {
            ParkId = parkId,
            LocalDate = localDate,
            DataKind = dataKind,
            SourceProvider = "test",
            FetchedAtUtc = DateTime.UtcNow,
            Latitude = 48.86,
            Longitude = 2.35,
            TemperatureMinCelsius = 10d,
            TemperatureMaxCelsius = 20d,
        };
    }

    private sealed class TestRefreshSettings : IParkWeatherRefreshSettings
    {
        public bool IsAutomaticRefreshEnabled => true;

        public int ForecastDays => 7;

        public int ForecastPastRetentionDays => 3;

        public bool IncludeYesterdayObservation => true;

        public int HistoricalBackfillYears { get; init; }

        public int HistoricalComparisonYearsLimit { get; init; } = 10;

        public int DelayBetweenParksMilliseconds => 0;

        public string AutomaticRefreshTimeZoneId => "UTC";

        public int AutomaticRefreshHour => 2;

        public int AutomaticRefreshMinute => 15;
    }
}
