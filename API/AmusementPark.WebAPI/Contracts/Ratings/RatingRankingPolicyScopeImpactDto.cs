namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingPolicyScopeImpactDto
{
    public string ScopeKey { get; set; } = string.Empty;

    public string TargetFamily { get; set; } = string.Empty;

    public string? ParkItemCategory { get; set; }

    public bool HasCurrentSnapshot { get; set; }

    public bool IsImpactAvailable { get; set; }

    public bool IsSourceTruncated { get; set; }

    public int CurrentEligibleCount { get; set; }

    public int CandidateEligibleCount { get; set; }

    public int GainedEligibilityCount { get; set; }

    public int LostEligibilityCount { get; set; }

    public int ComparedRankCount { get; set; }

    public long TotalAbsoluteRankChange { get; set; }

    public double? AverageRankChange { get; set; }

    public int? MaximumRankChange { get; set; }

    public bool HasMinimumComparableEntries { get; set; }

    public int IncompleteParkCompositionCount { get; set; }

    public int EstimatedTargetCount { get; set; }

    public int EstimatedChunkCount { get; set; }

    public IReadOnlyCollection<RatingRankingPolicyTargetChangeDto> GainedTargets { get; set; } =
        Array.Empty<RatingRankingPolicyTargetChangeDto>();

    public IReadOnlyCollection<RatingRankingPolicyTargetChangeDto> LostTargets { get; set; } =
        Array.Empty<RatingRankingPolicyTargetChangeDto>();
}
