namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportHistoricalItemRatingDto
{
    public string ParkItemId { get; init; } = string.Empty;
    public string? ParkItemName { get; init; }
    public long RatingCount { get; init; }
    public double Average { get; init; }
}
