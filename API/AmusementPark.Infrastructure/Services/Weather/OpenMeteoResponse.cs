using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Configuration.Weather;

namespace AmusementPark.Infrastructure.Services.Weather;

internal sealed class OpenMeteoResponse
{
    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("timezone")]
    public string? TimeZone { get; set; }

    [JsonPropertyName("utc_offset_seconds")]
    public int? UtcOffsetSeconds { get; set; }

    [JsonPropertyName("daily")]
    public OpenMeteoDailyResponse? Daily { get; set; }
}
