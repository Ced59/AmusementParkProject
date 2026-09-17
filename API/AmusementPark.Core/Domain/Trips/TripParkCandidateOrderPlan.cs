namespace AmusementPark.Core.Domain.Trips;

public sealed record TripParkCandidateOrderPlan(
    IReadOnlyCollection<TripParkCandidateOrderPosition> Changes,
    IReadOnlyCollection<TripParkCandidateOrderGuard> Guards,
    bool WasRenormalized);
