namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportHistoricalDisappearedAttractionDto
{
    public string? ParkName { get; init; }
    public string AttractionName { get; init; } = string.Empty;
    public int FirstVisitYear { get; init; }
    public int LastVisitYear { get; init; }
    public long CompletedRideCount { get; init; }
}
