using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripActivityEntryResult(
    long Sequence,
    TripActivityKind Kind,
    string ActorDisplayName,
    bool IsCurrentUser,
    int AffectedCount,
    DateTime OccurredAtUtc);
