namespace AmusementPark.Application.Features.Passport.Results;

public sealed record RideOccurrenceTargetResult(
    string Name,
    string? Category,
    string? LifecycleStatus,
    bool IsHistoricalSnapshot,
    DateOnly? OpeningDate = null,
    DateOnly? ClosingDate = null);
