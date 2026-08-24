namespace AmusementPark.Application.Features.StandaloneAttractions.Contracts;

public sealed record StandaloneAttractionSearchCriteria(
    string? SearchTerm,
    IReadOnlyCollection<string> MatchingCountryCodes,
    IReadOnlyCollection<string> RegionCountryCodes)
{
    public bool HasSearchTerm => !string.IsNullOrWhiteSpace(this.SearchTerm);

    public bool HasMatchingCountryCodes => this.MatchingCountryCodes.Count > 0;

    public bool HasRegionCountryCodes => this.RegionCountryCodes.Count > 0;
}
