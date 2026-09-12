namespace AmusementPark.Core.Domain.Weather;

public sealed class ParkWeatherDailySnapshot
{
    public string? Id { get; set; }

    public string ParkId { get; set; } = string.Empty;

    public DateOnly LocalDate { get; set; }

    public ParkWeatherDataKind DataKind { get; set; }

    public string SourceProvider { get; set; } = string.Empty;

    public DateTime FetchedAtUtc { get; set; }

    public DateTime? ProviderGeneratedAtUtc { get; set; }

    public string? TimeZone { get; set; }

    public int? UtcOffsetSeconds { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public int? WeatherCode { get; set; }

    public double? TemperatureMinCelsius { get; set; }

    public double? TemperatureMaxCelsius { get; set; }

    public double? ApparentTemperatureMinCelsius { get; set; }

    public double? ApparentTemperatureMaxCelsius { get; set; }

    public int? PrecipitationProbabilityMaxPercent { get; set; }

    public double? PrecipitationSumMillimeters { get; set; }

    public double? WindSpeedMaxKilometersPerHour { get; set; }

    public double? WindGustsMaxKilometersPerHour { get; set; }
}
