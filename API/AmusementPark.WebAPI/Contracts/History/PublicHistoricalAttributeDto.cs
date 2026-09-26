namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicHistoricalAttributeDto
{
    public string Kind { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string? DisplayValue { get; set; }

    public IReadOnlyCollection<string> DisplayCandidates { get; set; } = Array.Empty<string>();

    public bool IsDisplayResolved { get; set; }
}
