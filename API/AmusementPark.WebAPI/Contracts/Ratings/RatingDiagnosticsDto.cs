namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingDiagnosticsDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public long ExecutionDurationMilliseconds { get; set; }

    public long TotalRatings { get; set; }

    public long DistinctNumericValueCount { get; set; }

    public IReadOnlyCollection<string> DistinctNumericValueSample { get; set; } = Array.Empty<string>();

    public bool IsDistinctNumericValueSampleTruncated { get; set; }

    public RatingAnomalySummaryDto Anomalies { get; set; } = new RatingAnomalySummaryDto();

    public RatingAggregateIntegrityDto AggregateIntegrity { get; set; } = new RatingAggregateIntegrityDto();

    public IReadOnlyCollection<RatingTargetDistributionDto> TargetDistribution { get; set; } =
        Array.Empty<RatingTargetDistributionDto>();

    public IReadOnlyCollection<RatingIndexStatusDto> Indexes { get; set; } = Array.Empty<RatingIndexStatusDto>();
}
