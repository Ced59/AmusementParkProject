using System.Net;
using System.Text;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Configuration.Weather;
using AmusementPark.Infrastructure.Services.Weather;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Weather;

public sealed class OpenMeteoWeatherProviderStrategyTests
{
    [Fact]
    public async Task FetchDailyForecastAsync_WhenObservationIsRequested_ShouldUseForecastLocalDateForYesterday()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler();
        TestHttpClientFactory httpClientFactory = new TestHttpClientFactory(new HttpClient(handler));
        OpenMeteoWeatherProviderStrategy strategy = new OpenMeteoWeatherProviderStrategy(
            httpClientFactory,
            new ParkWeatherSettings
            {
                OpenMeteoForecastBaseUrl = "https://weather.test/forecast",
                OpenMeteoArchiveBaseUrl = "https://weather.test/archive",
                MinimumDelayBetweenProviderRequestsMilliseconds = 0,
            });
        Park park = CreatePark();

        ParkWeatherProviderResult result = await strategy.FetchDailyForecastAsync(
            park,
            7,
            includeYesterdayObservation: true,
            CancellationToken.None);

        Uri archiveRequest = handler.Requests.Single(uri => uri.AbsolutePath == "/archive");
        Assert.Contains("start_date=2026-06-17", archiveRequest.Query, StringComparison.Ordinal);
        Assert.Contains("end_date=2026-06-17", archiveRequest.Query, StringComparison.Ordinal);
        Assert.Contains(result.Snapshots, static snapshot =>
            snapshot.DataKind == ParkWeatherDataKind.Observation &&
            snapshot.LocalDate == new DateOnly(2026, 6, 17) &&
            snapshot.TimeZone == "America/New_York");
    }

    [Fact]
    public async Task FetchDailyForecastAsync_WhenYesterdayObservationTimesOut_ShouldReturnForecastWithWarning()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler
        {
            ThrowArchiveTimeout = true,
        };
        TestHttpClientFactory httpClientFactory = new TestHttpClientFactory(new HttpClient(handler));
        OpenMeteoWeatherProviderStrategy strategy = new OpenMeteoWeatherProviderStrategy(
            httpClientFactory,
            new ParkWeatherSettings
            {
                OpenMeteoForecastBaseUrl = "https://weather.test/forecast",
                OpenMeteoArchiveBaseUrl = "https://weather.test/archive",
                MinimumDelayBetweenProviderRequestsMilliseconds = 0,
            });
        Park park = CreatePark();

        ParkWeatherProviderResult result = await strategy.FetchDailyForecastAsync(
            park,
            7,
            includeYesterdayObservation: true,
            CancellationToken.None);

        Assert.Equal(2, result.Snapshots.Count);
        Assert.All(result.Snapshots, static snapshot => Assert.Equal(ParkWeatherDataKind.Forecast, snapshot.DataKind));
        Assert.Contains(result.Warnings, static warning => warning.Contains("Yesterday observation could not be fetched", StringComparison.Ordinal));
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task FetchDailyObservationsAsync_WhenDatesSpanSeparateRanges_ShouldRequestEachContiguousRange()
    {
        RecordingHttpMessageHandler handler = new RecordingHttpMessageHandler();
        TestHttpClientFactory httpClientFactory = new TestHttpClientFactory(new HttpClient(handler));
        OpenMeteoWeatherProviderStrategy strategy = new OpenMeteoWeatherProviderStrategy(
            httpClientFactory,
            new ParkWeatherSettings
            {
                OpenMeteoForecastBaseUrl = "https://weather.test/forecast",
                OpenMeteoArchiveBaseUrl = "https://weather.test/archive",
                MinimumDelayBetweenProviderRequestsMilliseconds = 0,
            });
        Park park = CreatePark();

        await strategy.FetchDailyObservationsAsync(
            park,
            new[]
            {
                new DateOnly(2025, 6, 20),
                new DateOnly(2025, 6, 21),
                new DateOnly(2024, 6, 20),
                new DateOnly(2024, 6, 21),
            },
            CancellationToken.None);

        List<Uri> archiveRequests = handler.Requests.Where(uri => uri.AbsolutePath == "/archive").ToList();
        Assert.Equal(2, archiveRequests.Count);
        Assert.Contains(archiveRequests, static uri =>
            uri.Query.Contains("start_date=2024-06-20", StringComparison.Ordinal) &&
            uri.Query.Contains("end_date=2024-06-21", StringComparison.Ordinal));
        Assert.Contains(archiveRequests, static uri =>
            uri.Query.Contains("start_date=2025-06-20", StringComparison.Ordinal) &&
            uri.Query.Contains("end_date=2025-06-21", StringComparison.Ordinal));
    }

    private static Park CreatePark()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Timezone Park",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
        park.SetPosition(40.71, -74.01);
        return park;
    }





}
