namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportGlobalFilterParkDto
{
    public string ParkId { get; init; } = string.Empty;
    public string? ParkName { get; init; }
}
