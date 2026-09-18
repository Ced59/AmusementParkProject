namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripPreferenceSummaryResult(
    string TripPlanId,
    string TripTitle,
    long PlanVersion,
    int ParticipantCount,
    bool CanDecide,
    IReadOnlyCollection<TripItemPreferenceSummaryResult> Items);
