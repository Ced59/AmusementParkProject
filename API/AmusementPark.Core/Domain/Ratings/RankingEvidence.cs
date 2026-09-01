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
}

/// <summary>
/// Faits déjà collectés pour évaluer une cible simple.
/// </summary>
public sealed record SimpleRankingEvidenceInput(
    int UniqueContributorCount,
    int RatingObservationCount,
    bool TargetCanReceiveVisitorRatings,
    bool IsExcludedByModeration,
    bool AggregateIntegrityIsValid);

/// <summary>
/// Couverture d'une catégorie publique par ses éléments éligibles.
/// </summary>
public sealed record RankingCategoryCoverage(
    int PublicItemCount,
    int EligibleItemCount);

/// <summary>
/// Faits déjà collectés pour évaluer le classement d'un parc composé.
/// </summary>
public sealed record ParkRankingEvidenceInput(
    int UniqueContributorCount,
    int RatingObservationCount,
    int DirectParkContributorCount,
    int ItemContributorCount,
    IReadOnlyCollection<RankingCategoryCoverage> ItemCategories,
    bool IsSingleCategoryParkException,
    bool TargetCanReceiveVisitorRatings,
    bool IsExcludedByModeration,
    bool AggregateIntegrityIsValid);

/// <summary>
/// Verdict autonome sur la couverture du composant éléments d'un parc.
/// </summary>
public sealed record ParkItemComponentEligibility(
    bool IsEligible,
    int EligibleItemCount,
    int EligibleCategoryCount,
    RankingIneligibilityReason? IneligibilityReason);

/// <summary>
/// Verdict sur la possibilité de publier un tableau de classement.
/// </summary>
public sealed record RankingPublicationEligibility(
    bool IsEligible,
    RankingIneligibilityReason? IneligibilityReason);
