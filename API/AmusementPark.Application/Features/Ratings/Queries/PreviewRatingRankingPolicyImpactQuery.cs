using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Results;

namespace AmusementPark.Application.Features.Ratings.Queries;

public sealed record PreviewRatingRankingPolicyImpactQuery(
    RatingRankingPolicyCandidate Candidate)
    : IQuery<ApplicationResult<RatingRankingPolicyImpactResult>>;
