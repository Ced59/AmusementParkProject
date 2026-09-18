using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripItemDecisionResult(
    TripItemDecisionStatus Status,
    string Reason,
    string DecidedByDisplayName,
    DateTime DecidedAtUtc,
    long Version);
