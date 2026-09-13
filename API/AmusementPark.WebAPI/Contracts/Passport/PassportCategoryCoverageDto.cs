namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportCategoryCoverageDto
{
    public string? Category { get; init; }
    public long CompletedRideCount { get; init; }
    public long DistinctItemCount { get; init; }
    public long HistoricalReferenceRideCount { get; init; }
    public long CurrentReferenceRideCount { get; init; }
    public long UnknownReferenceRideCount { get; init; }
    public double CompletedRideRate { get; init; }
}
