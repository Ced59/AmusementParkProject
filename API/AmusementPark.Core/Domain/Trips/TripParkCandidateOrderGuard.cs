namespace AmusementPark.Core.Domain.Trips;

public sealed record TripParkCandidateOrderGuard(
    TripParkCandidateId CandidateId,
    long SortPosition,
    long Version);
