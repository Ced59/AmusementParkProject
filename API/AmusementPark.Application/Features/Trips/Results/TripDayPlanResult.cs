namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripDayPlanResult(
    string DayPlanId,
    DateOnly LocalDate,
    string ParkCandidateId,
    string ParkId,
    string ParkName,
    TimeOnly? DesiredArrivalTime,
    string? GroupNote,
    IReadOnlyCollection<TripDayBlockResult> Blocks,
    long Version,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
