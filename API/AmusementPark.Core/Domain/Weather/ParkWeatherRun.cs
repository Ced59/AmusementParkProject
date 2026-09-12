namespace AmusementPark.Core.Domain.Weather;

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
