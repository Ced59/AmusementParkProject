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
