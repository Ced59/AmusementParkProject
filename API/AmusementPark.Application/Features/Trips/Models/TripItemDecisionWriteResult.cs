using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripItemDecisionWriteResult(
    TripChildWriteOutcome Outcome,
    TripItemDecision? Decision = null,
    long? CurrentVersion = null);
