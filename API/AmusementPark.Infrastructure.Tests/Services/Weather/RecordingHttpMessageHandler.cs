using System.Net;
using System.Text;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Configuration.Weather;
using AmusementPark.Infrastructure.Services.Weather;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Weather;

internal sealed class RecordingHttpMessageHandler : HttpMessageHandler
{
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

    public List<Uri> Requests { get; } = new List<Uri>();

    public bool ThrowArchiveTimeout { get; init; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Uri requestUri = request.RequestUri ?? throw new InvalidOperationException("Request URI is required.");
        this.Requests.Add(requestUri);

        if (this.ThrowArchiveTimeout && requestUri.AbsolutePath == "/archive")
        {
            throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout of 20 seconds elapsing.");
        }

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
