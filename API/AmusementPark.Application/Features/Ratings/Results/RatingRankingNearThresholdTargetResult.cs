using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingNearThresholdTargetResult(
    string ScopeKey,
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    int UniqueContributorCount,
    int EligibilityThreshold,
    int RemainingContributorCount);
