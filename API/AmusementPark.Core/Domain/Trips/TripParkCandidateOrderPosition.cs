namespace AmusementPark.Core.Domain.Trips;

public sealed record TripParkCandidateOrderPosition(
    TripParkCandidateId CandidateId,
    long SortPosition,
    long ExpectedVersion);
