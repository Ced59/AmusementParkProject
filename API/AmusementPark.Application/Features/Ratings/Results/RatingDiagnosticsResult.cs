namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingDiagnosticsResult(
    DateTime GeneratedAtUtc,
    long ExecutionDurationMilliseconds,
    long TotalRatings,
    long DistinctNumericValueCount,
    IReadOnlyCollection<string> DistinctNumericValueSample,
    bool IsDistinctNumericValueSampleTruncated,
    RatingAnomalySummaryResult Anomalies,
    RatingAggregateIntegrityResult AggregateIntegrity,
    IReadOnlyCollection<RatingTargetDistributionResult> TargetDistribution,
    IReadOnlyCollection<RatingIndexStatusResult> Indexes);
