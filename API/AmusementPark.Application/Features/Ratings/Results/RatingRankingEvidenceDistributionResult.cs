using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingEvidenceDistributionResult(
    RatingTargetType TargetType,
    RankingEvidenceLevel Level,
    int TargetCount,
    long UniqueContributorCount,
    long RatingObservationCount);
