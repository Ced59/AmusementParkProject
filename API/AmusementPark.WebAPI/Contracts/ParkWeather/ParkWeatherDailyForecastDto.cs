namespace AmusementPark.WebAPI.Contracts.ParkWeather;

public sealed class ParkWeatherDailyForecastDto
{
    public string LocalDate { get; set; } = string.Empty;

    public string DataKind { get; set; } = string.Empty;

    public int? WeatherCode { get; set; }

    public double? TemperatureMinCelsius { get; set; }

    public double? TemperatureMaxCelsius { get; set; }

    public double? ApparentTemperatureMinCelsius { get; set; }

    public double? ApparentTemperatureMaxCelsius { get; set; }

    public int? PrecipitationProbabilityMaxPercent { get; set; }

    public double? PrecipitationSumMillimeters { get; set; }

    public double? WindSpeedMaxKilometersPerHour { get; set; }

    public double? WindGustsMaxKilometersPerHour { get; set; }

    public string? TimeZone { get; set; }

    public DateTime FetchedAtUtc { get; set; }
}
