namespace AmusementPark.WebAPI.Contracts.ParkWeather;

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
