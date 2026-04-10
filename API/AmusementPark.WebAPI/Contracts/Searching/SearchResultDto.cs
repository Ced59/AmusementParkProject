namespace AmusementPark.WebAPI.Contracts.Searching;

/// <summary>
/// Résultat HTTP de la recherche transverse, aligné sur le contrat legacy.
/// </summary>
public sealed class SearchResultDto
{
    public string OriginalId { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
