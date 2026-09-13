namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingPolicyImpactDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public RatingRankingPolicyCandidateRequestDto Candidate { get; set; } =
        new RatingRankingPolicyCandidateRequestDto();

    public int GainedEligibilityCount { get; set; }

    public int LostEligibilityCount { get; set; }

    public int ComparedRankCount { get; set; }

    public long TotalAbsoluteRankChange { get; set; }

    public double? AverageRankChange { get; set; }

    public int? MaximumRankChange { get; set; }

    public int ScopeCountBelowMinimum { get; set; }

    public int IncompleteParkCompositionCount { get; set; }

    public int EstimatedTargetCount { get; set; }

    public int EstimatedChunkCount { get; set; }

    public IReadOnlyCollection<RatingRankingPolicyScopeImpactDto> Scopes { get; set; } =
        Array.Empty<RatingRankingPolicyScopeImpactDto>();
}
