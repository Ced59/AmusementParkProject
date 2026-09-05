using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingPolicyScopeImpactResult(
    string ScopeKey,
    RankingTargetFamily TargetFamily,
    ParkItemCategory? ParkItemCategory,
    bool HasCurrentSnapshot,
    bool IsImpactAvailable,
    bool IsSourceTruncated,
    int CurrentEligibleCount,
    int CandidateEligibleCount,
    int GainedEligibilityCount,
    int LostEligibilityCount,
    int ComparedRankCount,
    long TotalAbsoluteRankChange,
    double? AverageRankChange,
    int? MaximumRankChange,
    bool HasMinimumComparableEntries,
    int IncompleteParkCompositionCount,
    int EstimatedTargetCount,
    int EstimatedChunkCount,
    IReadOnlyCollection<RatingRankingPolicyTargetChangeResult> GainedTargets,
    IReadOnlyCollection<RatingRankingPolicyTargetChangeResult> LostTargets);
