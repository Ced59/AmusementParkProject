namespace AmusementPark.WebAPI.Contracts.Searching;

/// <summary>
/// Résultat HTTP de la recherche transverse, aligné sur le contrat legacy.
/// </summary>
public sealed class SearchResultDto
{
    public string OriginalId { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string Description { get; set; } = string.Empty;

    public string? City { get; set; }

    public string? CountryCode { get; set; }

    public string? LogoImageId { get; set; }

    public int? AttractionCount { get; set; }

    public string? ParentParkId { get; set; }

    public string? ParentParkName { get; set; }
}
