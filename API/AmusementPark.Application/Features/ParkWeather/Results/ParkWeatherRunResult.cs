using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Results;

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
