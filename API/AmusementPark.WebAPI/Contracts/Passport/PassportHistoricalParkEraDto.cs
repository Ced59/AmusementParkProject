namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportHistoricalParkEraDto
{
    public string? ParkName { get; init; }
    public int FirstVisitYear { get; init; }
    public int LastVisitYear { get; init; }
    public long VisitCount { get; init; }
    public long CanonicalEraCount { get; init; }
}
