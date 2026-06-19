namespace AmusementPark.WebAPI.Contracts.ParkWeather;

public sealed class ParkWeatherAttributionDto
{
    public string ProviderName { get; set; } = string.Empty;

    public string ProviderUrl { get; set; } = string.Empty;

    public string LicenseName { get; set; } = string.Empty;

    public string LicenseUrl { get; set; } = string.Empty;
}

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

public sealed class ParkWeatherForecastDto
{
    public string ParkId { get; set; } = string.Empty;

    public IReadOnlyCollection<ParkWeatherDailyForecastDto> Days { get; set; } = Array.Empty<ParkWeatherDailyForecastDto>();

    public ParkWeatherAttributionDto Attribution { get; set; } = new ParkWeatherAttributionDto();
}

public sealed class ParkWeatherRunDto
{
    public string Id { get; set; } = string.Empty;

    public string Trigger { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? SourceRunId { get; set; }

    public string? TargetParkId { get; set; }

    public string? CancelsAutomaticRunLocalDate { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public int TotalParkCount { get; set; }

    public int SucceededParkCount { get; set; }

    public int FailedParkCount { get; set; }

    public int SkippedParkCount { get; set; }

    public int WarningParkCount { get; set; }

    public string? Message { get; set; }
}

public sealed class ParkWeatherRunItemDto
{
    public string Id { get; set; } = string.Empty;

    public string RunId { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public string Status { get; set; } = string.Empty;

    public int AttemptCount { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public int ForecastDayCount { get; set; }

    public int ObservationDayCount { get; set; }

    public string? WarningMessage { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }
}
