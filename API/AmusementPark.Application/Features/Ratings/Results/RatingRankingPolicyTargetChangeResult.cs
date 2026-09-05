using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingPolicyTargetChangeResult(
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    int? PreviousRank,
    int? CandidateRank);
