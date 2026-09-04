namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Faits déjà collectés pour évaluer une cible simple.
/// </summary>
public sealed record SimpleRankingEvidenceInput(
    int UniqueContributorCount,
    int RatingObservationCount,
    bool TargetCanReceiveVisitorRatings,
    bool IsExcludedByModeration,
    bool AggregateIntegrityIsValid);
