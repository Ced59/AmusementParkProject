namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportGlobalParkActivityDto
{
    public string ParkId { get; init; } = string.Empty;
    public string? ParkName { get; init; }
    public long VisitCount { get; init; }
    public long RecordedRideCount { get; init; }
}
