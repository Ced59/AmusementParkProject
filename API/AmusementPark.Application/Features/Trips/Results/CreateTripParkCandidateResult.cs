namespace AmusementPark.Application.Features.Trips.Results;

public sealed record CreateTripParkCandidateResult(
    TripParkCandidateResult Candidate,
    bool WasReplayed);
