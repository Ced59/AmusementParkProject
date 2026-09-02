namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingAdministrationDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public RatingMethodologyDto CurrentMethodology { get; set; } = new RatingMethodologyDto();

    public RatingMethodologyDto? PreparingMethodology { get; set; }

    public RatingDiagnosticsDto DataDiagnostics { get; set; } = new RatingDiagnosticsDto();

    public IReadOnlyCollection<RatingRankingScopeDiagnosticsDto> Scopes { get; set; } =
        Array.Empty<RatingRankingScopeDiagnosticsDto>();

    public IReadOnlyCollection<RatingRankingEvidenceDistributionDto> EvidenceDistribution { get; set; } =
        Array.Empty<RatingRankingEvidenceDistributionDto>();

    public IReadOnlyCollection<RatingRankingNearThresholdTargetDto> NearThresholdTargets { get; set; } =
        Array.Empty<RatingRankingNearThresholdTargetDto>();

    public IReadOnlyCollection<RatingRankingExclusionDistributionDto> Exclusions { get; set; } =
        Array.Empty<RatingRankingExclusionDistributionDto>();

    public IReadOnlyCollection<RatingRankingCategoryCoverageDto> CategoryCoverage { get; set; } =
        Array.Empty<RatingRankingCategoryCoverageDto>();
}

public sealed class RatingRankingScopeDiagnosticsDto
{
    public string ScopeKey { get; set; } = string.Empty;

    public string TargetFamily { get; set; } = string.Empty;

    public string? ParkItemCategory { get; set; }

    public string MethodologyVersion { get; set; } = string.Empty;

    public string? CurrentSnapshotId { get; set; }

    public DateTime? GeneratedAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public long? RebuildDurationMilliseconds { get; set; }

    public int TotalEntryCount { get; set; }

    public int EligibleEntryCount { get; set; }

    public long SourceRevision { get; set; }

    public long? PublishedSourceRevision { get; set; }

    public bool IsRebuildOutstanding { get; set; }

    public bool IsDiagnosticSourceTruncated { get; set; }

    public string? LastJobStatus { get; set; }

    public string? LastErrorCode { get; set; }

    public DateTime? LastJobUpdatedAtUtc { get; set; }
}

public sealed class RatingRankingEvidenceDistributionDto
{
    public string TargetType { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public int TargetCount { get; set; }

    public long UniqueContributorCount { get; set; }

    public long RatingObservationCount { get; set; }
}

public sealed class RatingRankingNearThresholdTargetDto
{
    public string ScopeKey { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public int UniqueContributorCount { get; set; }

    public int EligibilityThreshold { get; set; }

    public int RemainingContributorCount { get; set; }
}

public sealed class RatingRankingExclusionDistributionDto
{
    public string TargetType { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public int TargetCount { get; set; }
}

public sealed class RatingRankingCategoryCoverageDto
{
    public string ScopeKey { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int CandidateCount { get; set; }

    public int EligibleCount { get; set; }

    public bool HasMinimumComparableEntries { get; set; }
}

public sealed class RatingRankingPolicyCandidateRequestDto
{
    public string Version { get; set; } = string.Empty;

    public int ProvisionalMinUniqueContributors { get; set; }

    public int EligibleMinUniqueContributors { get; set; }

    public int EstablishedMinUniqueContributors { get; set; }

    public int StrongEvidenceMinUniqueContributors { get; set; }

    public int MinimumEligibleEntriesPerRanking { get; set; }

    public int MinimumEligibleItemsForParkItemComponent { get; set; }

    public int MinimumEligibleItemsPerCategory { get; set; }

    public int MinimumEligibleCategories { get; set; }

    public decimal ScoreTieEpsilon { get; set; }
}

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

public sealed class RatingRankingPolicyTargetChangeDto
{
    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public int? PreviousRank { get; set; }

    public int? CandidateRank { get; set; }
}

public sealed class RatingRankingRebuildRequestDto
{
    public bool Confirmed { get; set; }
}

public sealed class RatingRankingRebuildRequestResultDto
{
    public DateTime RequestedAtUtc { get; set; }

    public int ScheduledScopeCount { get; set; }

    public IReadOnlyCollection<RatingRankingScheduledScopeDto> Scopes { get; set; } =
        Array.Empty<RatingRankingScheduledScopeDto>();
}

public sealed class RatingRankingScheduledScopeDto
{
    public string ScopeKey { get; set; } = string.Empty;

    public long RequestedSourceRevision { get; set; }
}
