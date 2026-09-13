namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportCurrentItemRatingDto
{
    public string ParkItemId { get; init; } = string.Empty;
    public string? ParkItemName { get; init; }
    public double Rating { get; init; }
}
