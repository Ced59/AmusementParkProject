using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Results;

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
