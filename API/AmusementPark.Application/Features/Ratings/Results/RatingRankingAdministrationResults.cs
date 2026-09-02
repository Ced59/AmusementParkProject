using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingAdministrationResult(
    DateTime GeneratedAtUtc,
    RatingMethodologyResult CurrentMethodology,
    RatingMethodologyResult? PreparingMethodology,
    RatingDiagnosticsResult DataDiagnostics,
    IReadOnlyCollection<RatingRankingScopeDiagnosticsResult> Scopes,
    IReadOnlyCollection<RatingRankingEvidenceDistributionResult> EvidenceDistribution,
    IReadOnlyCollection<RatingRankingNearThresholdTargetResult> NearThresholdTargets,
    IReadOnlyCollection<RatingRankingExclusionDistributionResult> Exclusions,
    IReadOnlyCollection<RatingRankingCategoryCoverageResult> CategoryCoverage);

public sealed record RatingRankingScopeDiagnosticsResult(
    string ScopeKey,
    RankingTargetFamily TargetFamily,
    ParkItemCategory? ParkItemCategory,
    string MethodologyVersion,
    string? CurrentSnapshotId,
    DateTime? GeneratedAtUtc,
    DateTime? PublishedAtUtc,
    long? RebuildDurationMilliseconds,
    int TotalEntryCount,
    int EligibleEntryCount,
    long SourceRevision,
    long? PublishedSourceRevision,
    bool IsRebuildOutstanding,
    bool IsDiagnosticSourceTruncated,
    string? LastJobStatus,
    string? LastErrorCode,
    DateTime? LastJobUpdatedAtUtc);

public sealed record RatingRankingEvidenceDistributionResult(
    RatingTargetType TargetType,
    RankingEvidenceLevel Level,
    int TargetCount,
    long UniqueContributorCount,
    long RatingObservationCount);

public sealed record RatingRankingNearThresholdTargetResult(
    string ScopeKey,
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    int UniqueContributorCount,
    int EligibilityThreshold,
    int RemainingContributorCount);

public sealed record RatingRankingExclusionDistributionResult(
    RatingTargetType TargetType,
    RankingIneligibilityReason Reason,
    int TargetCount);

public sealed record RatingRankingCategoryCoverageResult(
    string ScopeKey,
    ParkItemCategory Category,
    int CandidateCount,
    int EligibleCount,
    bool HasMinimumComparableEntries);

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

public sealed record RatingRankingPolicyTargetChangeResult(
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    int? PreviousRank,
    int? CandidateRank);

public sealed record RatingRankingRebuildRequestResult(
    DateTime RequestedAtUtc,
    int ScheduledScopeCount,
    IReadOnlyCollection<RatingRankingScheduledScopeResult> Scopes);

public sealed record RatingRankingScheduledScopeResult(
    string ScopeKey,
    long RequestedSourceRevision);
