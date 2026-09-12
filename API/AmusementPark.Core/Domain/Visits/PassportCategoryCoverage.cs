namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportCategoryCoverage(
    string? Category,
    long CompletedRideCount,
    long DistinctItemCount,
    long HistoricalReferenceRideCount,
    long CurrentReferenceRideCount,
    long UnknownReferenceRideCount,
    double CompletedRideRate);
