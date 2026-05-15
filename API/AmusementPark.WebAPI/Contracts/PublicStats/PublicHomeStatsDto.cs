namespace AmusementPark.WebAPI.Contracts.PublicStats;

/// <summary>
/// Statistiques publiques affichées dans le hero de la home.
/// </summary>
public sealed record PublicHomeStatsDto(
    long ParksCount,
    long AttractionsCount,
    int CountriesCount);
