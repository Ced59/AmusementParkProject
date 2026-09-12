namespace AmusementPark.Application.Features.Passport.Models;

public sealed record RideOccurrenceAppendState(
    long? LastSortPosition,
    bool WasNormalizedForOperation);
