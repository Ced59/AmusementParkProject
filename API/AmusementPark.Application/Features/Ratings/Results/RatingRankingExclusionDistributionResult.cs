using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingExclusionDistributionResult(
    RatingTargetType TargetType,
    RankingIneligibilityReason Reason,
    int TargetCount);
