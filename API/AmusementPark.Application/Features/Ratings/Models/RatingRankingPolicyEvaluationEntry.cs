using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingRankingPolicyEvaluationEntry(
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    ParkItemCategory? ParkItemCategory,
    double Score,
    RankingEvidence? Evidence,
    ParkItemComponentEligibility? ParkItemComponent = null);
