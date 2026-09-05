using AmusementPark.Application.Features.Ratings.Models;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingPolicyImpactResult(
    DateTime GeneratedAtUtc,
    RatingRankingPolicyCandidate Candidate,
    int GainedEligibilityCount,
    int LostEligibilityCount,
    int ComparedRankCount,
    long TotalAbsoluteRankChange,
    double? AverageRankChange,
    int? MaximumRankChange,
    int ScopeCountBelowMinimum,
    int IncompleteParkCompositionCount,
    int EstimatedTargetCount,
    int EstimatedChunkCount,
    IReadOnlyCollection<RatingRankingPolicyScopeImpactResult> Scopes);
