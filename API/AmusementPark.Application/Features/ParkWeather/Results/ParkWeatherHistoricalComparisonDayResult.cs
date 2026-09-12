using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Results;

public sealed class ParkWeatherHistoricalComparisonDayResult
{
    public DateOnly ForecastLocalDate { get; init; }

    public DateOnly LocalDate { get; init; }

    public int? WeatherCode { get; init; }

    public double? TemperatureMinCelsius { get; init; }

    public double? TemperatureMaxCelsius { get; init; }

    public double? ApparentTemperatureMinCelsius { get; init; }

    public double? ApparentTemperatureMaxCelsius { get; init; }

    public double? PrecipitationSumMillimeters { get; init; }

    public double? WindSpeedMaxKilometersPerHour { get; init; }

    public double? WindGustsMaxKilometersPerHour { get; init; }
}
