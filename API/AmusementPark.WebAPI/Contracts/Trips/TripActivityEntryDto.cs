namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed record TripActivityEntryDto(
    long Sequence,
    string Kind,
    string ActorDisplayName,
    bool IsCurrentUser,
    int AffectedCount,
    DateTime OccurredAtUtc);
