namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripPreferenceBoardResult(
    string TripPlanId,
    string TripTitle,
    long PlanVersion,
    bool CanVote,
    IReadOnlyCollection<TripItemPreferenceResult> Items);
