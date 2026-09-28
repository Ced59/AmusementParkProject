namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PreviewHistoricalImpactRequestDto
{
    public string ResourceType { get; init; } = string.Empty;

    public string ResourceId { get; init; } = string.Empty;

    public int? Year { get; init; }
}
