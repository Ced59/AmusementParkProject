using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Results;

public sealed class ParkWeatherAttributionResult
{
    public string ProviderName { get; init; } = "Open-Meteo";

    public string ProviderUrl { get; init; } = "https://open-meteo.com/";

    public string LicenseName { get; init; } = "CC BY 4.0";

    public string LicenseUrl { get; init; } = "https://creativecommons.org/licenses/by/4.0/";
}

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

public sealed class ParkWeatherForecastResult
{
    public string ParkId { get; init; } = string.Empty;

    public IReadOnlyCollection<ParkWeatherDailyForecastResult> Days { get; init; } = Array.Empty<ParkWeatherDailyForecastResult>();

    public ParkWeatherAttributionResult Attribution { get; init; } = new ParkWeatherAttributionResult();
}

public sealed class ParkWeatherHistoricalComparisonsResult
{
    public string ParkId { get; init; } = string.Empty;

    public IReadOnlyCollection<ParkWeatherHistoricalComparisonResult> Years { get; init; } = Array.Empty<ParkWeatherHistoricalComparisonResult>();

    public ParkWeatherAttributionResult Attribution { get; init; } = new ParkWeatherAttributionResult();
}

public sealed class ParkWeatherHistoricalComparisonResult
{
    public int YearsBack { get; init; }

    public IReadOnlyCollection<ParkWeatherHistoricalComparisonDayResult> Days { get; init; } = Array.Empty<ParkWeatherHistoricalComparisonDayResult>();
}

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

public sealed class ParkWeatherRunResult
{
    public string Id { get; init; } = string.Empty;

    public ParkWeatherRunTrigger Trigger { get; init; }

    public ParkWeatherRefreshScope Scope { get; init; }

    public ParkWeatherRunStatus Status { get; init; }

    public string? SourceRunId { get; init; }

    public string? TargetParkId { get; init; }

    public DateOnly? CancelsAutomaticRunLocalDate { get; init; }

    public DateTime RequestedAtUtc { get; init; }

    public DateTime? StartedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public int TotalParkCount { get; init; }

    public int SucceededParkCount { get; init; }

    public int FailedParkCount { get; init; }

    public int SkippedParkCount { get; init; }

    public int WarningParkCount { get; init; }

    public string? Message { get; init; }
}

public sealed class ParkWeatherRunItemResult
{
    public string Id { get; init; } = string.Empty;

    public string RunId { get; init; } = string.Empty;

    public string ParkId { get; init; } = string.Empty;

    public string? ParkName { get; init; }

    public ParkWeatherRunItemStatus Status { get; init; }

    public int AttemptCount { get; init; }

    public DateTime? StartedAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public int ForecastDayCount { get; init; }

    public int ObservationDayCount { get; init; }

    public string? WarningMessage { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }
}
