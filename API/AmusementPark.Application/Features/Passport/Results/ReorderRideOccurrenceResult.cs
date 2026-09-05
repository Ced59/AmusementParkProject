namespace AmusementPark.Application.Features.Passport.Results;

public sealed record ReorderRideOccurrenceResult(
    RideOccurrenceResult Occurrence,
    bool WasReplayed,
    bool WasNormalized);
