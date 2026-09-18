namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripProgramTravelSegmentDto
{
    public DateOnly FromDate { get; init; }

    public string FromParkId { get; init; } = string.Empty;

    public string? FromParkName { get; init; }

    public DateOnly ToDate { get; init; }

    public string ToParkId { get; init; } = string.Empty;

    public string? ToParkName { get; init; }

    public double DistanceKilometers { get; init; }

    public int EstimatedTravelDurationMinutes { get; init; }

    public string EstimationMethod { get; init; } = string.Empty;
}
