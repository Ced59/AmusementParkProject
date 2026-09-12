namespace AmusementPark.Core.Domain.Weather;

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
