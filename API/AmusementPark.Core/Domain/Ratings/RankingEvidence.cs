namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Preuve métier accompagnant une note et son éventuel rang communautaire.
/// </summary>
public sealed record RankingEvidence(
    RankingEvidenceLevel Level,
    bool IsEligibleForMainRanking,
    int UniqueContributorCount,
    int RatingObservationCount,
    int? DirectParkContributorCount,
    int? ItemContributorCount,
    int? EligibleItemCount,
    int? EligibleCategoryCount,
    RatingMethodologyVersion MethodologyVersion,
    RankingIneligibilityReason? IneligibilityReason)
{
    public int? NextContributorThreshold { get; init; }

    public bool? IsSingleCategoryParkException { get; init; }

    public int? PublicItemCategoryCount { get; init; }
}
