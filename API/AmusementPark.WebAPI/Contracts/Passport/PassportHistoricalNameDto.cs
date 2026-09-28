namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportHistoricalNameDto
{
    public string? ParkName { get; init; }
    public string NameAtVisit { get; init; } = string.Empty;
    public string? CurrentName { get; init; }
    public int FirstVisitYear { get; init; }
    public int LastVisitYear { get; init; }
    public long CompletedRideCount { get; init; }
}
