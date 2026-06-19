namespace AmusementPark.Core.Domain.Weather;

public enum ParkWeatherDataKind
{
    Forecast,
    Observation,
}

public enum ParkWeatherRefreshScope
{
    FullVisibleParks,
    FailedFromRun,
    SinglePark,
}

public enum ParkWeatherRunTrigger
{
    Automatic,
    Manual,
    RetryFailed,
    RetryPark,
}

public enum ParkWeatherRunStatus
{
    Queued,
    Running,
    Completed,
    CompletedWithFailures,
    Failed,
    Skipped,
}

public enum ParkWeatherRunItemStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Skipped,
}

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

public sealed class ParkWeatherRun
{
    public string? Id { get; set; }

    public ParkWeatherRunTrigger Trigger { get; set; }

    public ParkWeatherRefreshScope Scope { get; set; }

    public ParkWeatherRunStatus Status { get; set; }

    public string? SourceRunId { get; set; }

    public string? TargetParkId { get; set; }

    public DateOnly? CancelsAutomaticRunLocalDate { get; set; }

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

public sealed class ParkWeatherRunItem
{
    public string? Id { get; set; }

    public string RunId { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public ParkWeatherRunItemStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public int ForecastDayCount { get; set; }

    public int ObservationDayCount { get; set; }

    public string? WarningMessage { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }
}
