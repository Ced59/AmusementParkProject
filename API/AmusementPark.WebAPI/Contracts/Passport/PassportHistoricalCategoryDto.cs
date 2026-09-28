namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportHistoricalCategoryDto
{
    public string Category { get; init; } = string.Empty;
    public long CompletedRideCount { get; init; }
    public long DistinctAttractionCount { get; init; }
}
