namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportHistoricalTransformationDto
{
    public string? ParkName { get; init; }
    public string CurrentName { get; init; } = string.Empty;
    public string CurrentCategory { get; init; } = string.Empty;
    public IReadOnlyCollection<string> NamesAtVisit { get; init; } = Array.Empty<string>();
    public IReadOnlyCollection<string> CategoriesAtVisit { get; init; } = Array.Empty<string>();
    public int FirstVisitYear { get; init; }
    public int LastVisitYear { get; init; }
    public long CompletedRideCount { get; init; }
}
