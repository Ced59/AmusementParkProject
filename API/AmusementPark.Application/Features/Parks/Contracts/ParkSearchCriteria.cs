namespace AmusementPark.Application.Features.Parks.Contracts;

/// <summary>
/// Critères applicatifs communs pour la liste publique et la carte des parcs.
/// </summary>
public sealed record ParkSearchCriteria(
    string? SearchTerm,
    IReadOnlyCollection<string> MatchingCountryCodes,
    IReadOnlyCollection<string> RegionCountryCodes)
{
    public static ParkSearchCriteria Empty { get; } = new ParkSearchCriteria(null, Array.Empty<string>(), Array.Empty<string>());

    public bool HasSearchTerm => !string.IsNullOrWhiteSpace(SearchTerm);

    public bool HasMatchingCountryCodes => MatchingCountryCodes.Count > 0;

    public bool HasRegionCountryCodes => RegionCountryCodes.Count > 0;

    public bool HasAnyFilter => HasSearchTerm || HasMatchingCountryCodes || HasRegionCountryCodes;
}
