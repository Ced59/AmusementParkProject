namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Verdict atomique sur l'éligibilité, la composition et la note d'un parc.
/// </summary>
public sealed record ParkRankingEvaluation(
    RankingEvidence Evidence,
    ParkItemComponentEligibility ItemComponent,
    ParkRankingCompositionMode CompositionMode,
    double Score);
