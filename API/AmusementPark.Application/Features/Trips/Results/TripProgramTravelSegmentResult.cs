namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripProgramTravelSegmentResult(
    DateOnly FromDate,
    string FromParkId,
    string? FromParkName,
    DateOnly ToDate,
    string ToParkId,
    string? ToParkName,
    double DistanceKilometers,
    int EstimatedTravelDurationMinutes,
    string EstimationMethod);
