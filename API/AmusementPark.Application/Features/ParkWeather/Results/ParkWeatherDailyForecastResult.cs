using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Results;

public sealed class ParkWeatherDailyForecastResult
{
    public DateOnly LocalDate { get; init; }

    public ParkWeatherDataKind DataKind { get; init; }

    public int? WeatherCode { get; init; }

    public double? TemperatureMinCelsius { get; init; }

    public double? TemperatureMaxCelsius { get; init; }

    public double? ApparentTemperatureMinCelsius { get; init; }

    public double? ApparentTemperatureMaxCelsius { get; init; }

    public int? PrecipitationProbabilityMaxPercent { get; init; }

    public double? PrecipitationSumMillimeters { get; init; }

    public double? WindSpeedMaxKilometersPerHour { get; init; }

    public double? WindGustsMaxKilometersPerHour { get; init; }

    public string? TimeZone { get; init; }

    public DateTime FetchedAtUtc { get; init; }
}
