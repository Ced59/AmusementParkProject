namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Verdict autonome sur la couverture du composant éléments d'un parc.
/// </summary>
public sealed record ParkItemComponentEligibility(
    bool IsEligible,
    int EligibleItemCount,
    int EligibleCategoryCount,
    RankingIneligibilityReason? IneligibilityReason);
