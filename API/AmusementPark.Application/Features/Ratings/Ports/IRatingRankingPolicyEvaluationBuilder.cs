using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankingPolicyEvaluationBuilder
{
    Task<RatingRankingPolicyEvaluationPlan> EvaluateAsync(
        RankingScopeDefinition scope,
        RankingEligibilityPolicy eligibilityPolicy,
        CancellationToken cancellationToken);
}
