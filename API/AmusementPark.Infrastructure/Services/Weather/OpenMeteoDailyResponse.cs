using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Configuration.Weather;

namespace AmusementPark.Infrastructure.Services.Weather;

internal sealed class OpenMeteoDailyResponse
{
    [JsonPropertyName("time")]
    public List<string>? Time { get; set; }

    [JsonPropertyName("weather_code")]
    public List<int?>? WeatherCode { get; set; }

    [JsonPropertyName("temperature_2m_min")]
    public List<double?>? TemperatureMinCelsius { get; set; }

    [JsonPropertyName("temperature_2m_max")]
    public List<double?>? TemperatureMaxCelsius { get; set; }

    [JsonPropertyName("apparent_temperature_min")]
    public List<double?>? ApparentTemperatureMinCelsius { get; set; }

    [JsonPropertyName("apparent_temperature_max")]
    public List<double?>? ApparentTemperatureMaxCelsius { get; set; }

    [JsonPropertyName("precipitation_probability_max")]
    public List<int?>? PrecipitationProbabilityMaxPercent { get; set; }

    [JsonPropertyName("precipitation_sum")]
    public List<double?>? PrecipitationSumMillimeters { get; set; }

    [JsonPropertyName("wind_speed_10m_max")]
    public List<double?>? WindSpeedMaxKilometersPerHour { get; set; }

    [JsonPropertyName("wind_gusts_10m_max")]
    public List<double?>? WindGustsMaxKilometersPerHour { get; set; }
}
