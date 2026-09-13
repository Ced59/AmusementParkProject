namespace AmusementPark.WebAPI.Contracts.ParkWeather;

public sealed class ParkWeatherHistoricalComparisonDayDto
{
    public string ForecastLocalDate { get; set; } = string.Empty;

    public string LocalDate { get; set; } = string.Empty;

    public int? WeatherCode { get; set; }

    public double? TemperatureMinCelsius { get; set; }

    public double? TemperatureMaxCelsius { get; set; }

    public double? ApparentTemperatureMinCelsius { get; set; }

    public double? ApparentTemperatureMaxCelsius { get; set; }

    public double? PrecipitationSumMillimeters { get; set; }

    public double? WindSpeedMaxKilometersPerHour { get; set; }

    public double? WindGustsMaxKilometersPerHour { get; set; }
}
