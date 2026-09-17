using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripParkCandidateWriteResult(
    TripChildWriteOutcome Outcome,
    TripParkCandidate? Candidate = null,
    long? CurrentVersion = null,
    bool WasReplayed = false);
