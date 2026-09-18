namespace AmusementPark.Core.Domain.Trips;

public sealed record TripProgramAttractionFact(
    string ParkItemId,
    string ParkId,
    TripItemDecisionStatus DecisionStatus,
    bool IsAvailable,
    string? OfficialStatus,
    int NotForMeCount,
    DateTime? LatestNotForMeAtUtc,
    DateTime DecisionUpdatedAtUtc);
