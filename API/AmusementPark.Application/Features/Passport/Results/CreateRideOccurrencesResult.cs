namespace AmusementPark.Application.Features.Passport.Results;

public sealed record CreateRideOccurrencesResult(
    IReadOnlyCollection<RideOccurrenceResult> Occurrences,
    bool WasReplayed,
    bool WasNormalized);
