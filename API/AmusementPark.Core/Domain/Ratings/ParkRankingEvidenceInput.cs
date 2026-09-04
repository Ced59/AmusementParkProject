namespace AmusementPark.Core.Domain.Ratings;

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
