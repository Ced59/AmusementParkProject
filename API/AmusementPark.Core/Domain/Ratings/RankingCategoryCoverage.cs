namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Couverture d'une catégorie publique par ses éléments éligibles.
/// </summary>
public sealed record RankingCategoryCoverage(
    int PublicItemCount,
    int EligibleItemCount);
