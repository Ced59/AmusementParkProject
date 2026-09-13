namespace AmusementPark.WebAPI.Contracts.ParkWeather;

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
