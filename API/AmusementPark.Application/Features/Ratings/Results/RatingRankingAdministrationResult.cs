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
