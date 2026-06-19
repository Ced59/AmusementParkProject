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

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient httpClient;

        public TestHttpClientFactory(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public HttpClient CreateClient(string name)
        {
            return this.httpClient;
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = new List<Uri>();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri requestUri = request.RequestUri ?? throw new InvalidOperationException("Request URI is required.");
            this.Requests.Add(requestUri);

            string content = requestUri.AbsolutePath == "/forecast"
                ? ForecastResponse
                : ArchiveResponse;

            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
            };

            return Task.FromResult(response);
        }
    }

    private const string ForecastResponse = """
        {
          "latitude": 40.71,
          "longitude": -74.01,
          "timezone": "America/New_York",
          "utc_offset_seconds": -14400,
          "daily": {
            "time": ["2026-06-18", "2026-06-19"],
            "weather_code": [1, 3],
            "temperature_2m_max": [24.5, 26.0],
            "temperature_2m_min": [14.0, 16.0],
            "apparent_temperature_max": [25.0, 27.0],
            "apparent_temperature_min": [13.5, 16.5],
            "precipitation_probability_max": [20, 35],
            "precipitation_sum": [0.0, 1.2],
            "wind_speed_10m_max": [12.0, 14.0],
            "wind_gusts_10m_max": [24.0, 28.0]
          }
        }
        """;

    private const string ArchiveResponse = """
        {
          "latitude": 40.71,
          "longitude": -74.01,
          "timezone": "America/New_York",
          "utc_offset_seconds": -14400,
          "daily": {
            "time": ["2026-06-17"],
            "weather_code": [61],
            "temperature_2m_max": [22.0],
            "temperature_2m_min": [15.0],
            "apparent_temperature_max": [23.0],
            "apparent_temperature_min": [15.5],
            "precipitation_sum": [4.2],
            "wind_speed_10m_max": [18.0],
            "wind_gusts_10m_max": [32.0]
          }
        }
        """;
}
