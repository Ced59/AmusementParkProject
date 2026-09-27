namespace AmusementPark.WebAPI.Contracts.History;

public sealed record PublicHistoricalCategoryNetChangeDto
{
    public string Category { get; init; } = string.Empty;

    public int FromCount { get; init; }

    public int ToCount { get; init; }

    public int NetChange { get; init; }
}
