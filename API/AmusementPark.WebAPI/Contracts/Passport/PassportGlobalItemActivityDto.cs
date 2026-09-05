namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportGlobalItemActivityDto
{
    public string ParkItemId { get; init; } = string.Empty;
    public string? ParkItemName { get; init; }
    public string ParkId { get; init; } = string.Empty;
    public string? ParkName { get; init; }
    public long CompletedRideCount { get; init; }
}
