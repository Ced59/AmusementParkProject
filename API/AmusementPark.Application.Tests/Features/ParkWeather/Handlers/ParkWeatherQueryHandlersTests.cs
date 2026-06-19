using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkWeather.Handlers;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Queries;
using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkWeather.Handlers;

public sealed class ParkWeatherQueryHandlersTests
{
    [Fact]
    public async Task HandleAsync_WhenParkTimezoneIsBehindUtc_ShouldStartForecastAtParkLocalDate()
    {
        Park park = CreatePark("park-1");
        ParkWeatherDailySnapshot latestForecast = CreateSnapshot("park-1", new DateOnly(2026, 6, 18));
        latestForecast.TimeZone = "Unknown/Test";
        latestForecast.UtcOffsetSeconds = -4 * 60 * 60;
        DateOnly capturedFromLocalDate = default;
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        Mock<IParkWeatherRepository> weatherRepository = new Mock<IParkWeatherRepository>(MockBehavior.Strict);
        weatherRepository
            .Setup(repository => repository.GetLatestForecastSnapshotAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(latestForecast);
        weatherRepository
            .Setup(repository => repository.GetForecastAsync("park-1", It.IsAny<DateOnly>(), 7, It.IsAny<CancellationToken>()))
            .Callback<string, DateOnly, int, CancellationToken>((_, fromLocalDate, _, _) => capturedFromLocalDate = fromLocalDate)
            .ReturnsAsync(new[] { latestForecast });
        FixedTimeProvider timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 6, 19, 0, 30, 0, TimeSpan.Zero));
        GetParkWeatherForecastQueryHandler handler = new GetParkWeatherForecastQueryHandler(
            parkRepository.Object,
            weatherRepository.Object,
            new ParkWeatherLocalDateResolver(timeProvider));

        ApplicationResult<ParkWeatherForecastResult> result = await handler.HandleAsync(
            new GetParkWeatherForecastQuery("park-1", 7),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2026, 6, 18), capturedFromLocalDate);
        parkRepository.VerifyAll();
        weatherRepository.VerifyAll();
    }

    [Fact]
    public void ResolveLocalDate_WhenOffsetMovesDateForward_ShouldReturnOffsetLocalDate()
    {
        DateOnly localDate = ParkWeatherLocalDateResolver.ResolveLocalDate(
            new DateTime(2026, 6, 18, 12, 30, 0, DateTimeKind.Utc),
            "Unknown/Test",
            14 * 60 * 60);

        Assert.Equal(new DateOnly(2026, 6, 19), localDate);
    }

    [Fact]
    public async Task HandleAsync_WhenHistoricalComparisonsAreRequested_ShouldReturnStoredObservationDatesAlignedWithForecastDates()
    {
        Park park = CreatePark("park-1");
        ParkWeatherDailySnapshot latestForecast = CreateSnapshot("park-1", new DateOnly(2028, 2, 29));
        latestForecast.UtcOffsetSeconds = 0;
        ParkWeatherDailySnapshot leapForecast = CreateSnapshot("park-1", new DateOnly(2028, 2, 29));
        ParkWeatherDailySnapshot marchForecast = CreateSnapshot("park-1", new DateOnly(2028, 3, 1));
        IReadOnlyCollection<DateOnly> requestedObservationDates = Array.Empty<DateOnly>();
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        Mock<IParkWeatherRepository> weatherRepository = new Mock<IParkWeatherRepository>(MockBehavior.Strict);
        weatherRepository
            .Setup(repository => repository.GetLatestForecastSnapshotAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(latestForecast);
        weatherRepository
            .Setup(repository => repository.GetForecastAsync("park-1", new DateOnly(2028, 2, 29), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { leapForecast, marchForecast });
        weatherRepository
            .Setup(repository => repository.GetObservationsByDatesAsync("park-1", It.IsAny<IReadOnlyCollection<DateOnly>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyCollection<DateOnly>, CancellationToken>((_, dates, _) => requestedObservationDates = dates)
            .ReturnsAsync(new[]
            {
                CreateObservation("park-1", new DateOnly(2027, 2, 28), 6d, 12d),
                CreateObservation("park-1", new DateOnly(2027, 3, 1), 7d, 13d),
                CreateObservation("park-1", new DateOnly(2026, 2, 28), 8d, 14d),
            });
        GetParkWeatherHistoricalComparisonsQueryHandler handler = new GetParkWeatherHistoricalComparisonsQueryHandler(
            parkRepository.Object,
            weatherRepository.Object,
            new ParkWeatherLocalDateResolver(new FixedTimeProvider(new DateTimeOffset(2028, 2, 29, 8, 0, 0, TimeSpan.Zero))),
            new ParkWeatherHistoricalComparisonDateResolver(),
            new TestRefreshSettings());

        ApplicationResult<ParkWeatherHistoricalComparisonsResult> result = await handler.HandleAsync(
            new GetParkWeatherHistoricalComparisonsQuery("park-1", 7, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        ParkWeatherHistoricalComparisonsResult value = result.Value ?? throw new InvalidOperationException("A successful result should expose comparisons.");
        Assert.Equal(2, value.Years.Count);
        Assert.Contains(new DateOnly(2027, 2, 28), requestedObservationDates);
        Assert.Contains(new DateOnly(2027, 3, 1), requestedObservationDates);
        Assert.Contains(new DateOnly(2026, 2, 28), requestedObservationDates);
        Assert.DoesNotContain(new DateOnly(2025, 2, 28), requestedObservationDates);
        ParkWeatherHistoricalComparisonDayResult leapDayComparison = value.Years.Single(year => year.YearsBack == 1).Days.Single(day => day.ForecastLocalDate == new DateOnly(2028, 2, 29));
        Assert.Equal(new DateOnly(2027, 2, 28), leapDayComparison.LocalDate);
        Assert.Equal(6d, leapDayComparison.TemperatureMinCelsius);
        parkRepository.VerifyAll();
        weatherRepository.VerifyAll();
    }

    private static Park CreatePark(string id)
    {
        Park park = new Park
        {
            Id = id,
            Name = "Timezone Park",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        park.SetPosition(40.71, -74.01);
        return park;
    }

    private static ParkWeatherDailySnapshot CreateSnapshot(string parkId, DateOnly localDate)
    {
        return new ParkWeatherDailySnapshot
        {
            ParkId = parkId,
            LocalDate = localDate,
            DataKind = ParkWeatherDataKind.Forecast,
            SourceProvider = "open-meteo",
            FetchedAtUtc = new DateTime(2026, 6, 18, 8, 0, 0, DateTimeKind.Utc),
            Latitude = 40.71,
            Longitude = -74.01,
            TemperatureMinCelsius = 14d,
            TemperatureMaxCelsius = 24d,
        };
    }

    private static ParkWeatherDailySnapshot CreateObservation(string parkId, DateOnly localDate, double minTemperature, double maxTemperature)
    {
        ParkWeatherDailySnapshot snapshot = CreateSnapshot(parkId, localDate);
        snapshot.DataKind = ParkWeatherDataKind.Observation;
        snapshot.TemperatureMinCelsius = minTemperature;
        snapshot.TemperatureMaxCelsius = maxTemperature;
        return snapshot;
    }

    private sealed class TestRefreshSettings : IParkWeatherRefreshSettings
    {
        public bool IsAutomaticRefreshEnabled => true;

        public int ForecastDays => 7;

        public int ForecastPastRetentionDays => 3;

        public bool IncludeYesterdayObservation => true;

        public int HistoricalBackfillYears => 3;

        public int HistoricalComparisonYearsLimit => 2;

        public int DelayBetweenParksMilliseconds => 0;

        public string AutomaticRefreshTimeZoneId => "UTC";

        public int AutomaticRefreshHour => 2;

        public int AutomaticRefreshMinute => 15;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return this.utcNow;
        }
    }
}
