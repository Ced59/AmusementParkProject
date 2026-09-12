namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportCategoryCoverageResult(
    string? Category,
    long CompletedRideCount,
    long DistinctItemCount,
    long HistoricalReferenceRideCount,
    long CurrentReferenceRideCount,
    long UnknownReferenceRideCount,
    double CompletedRideRate);
