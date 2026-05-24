namespace AmusementPark.Application.Features.Parks.Results;

/// <summary>
/// Statistiques publiques affichées sur la home MVP.
/// </summary>
public sealed record PublicHomeStatsResult(
    long ParksCount,
    long AttractionsCount,
    int CountriesCount);
