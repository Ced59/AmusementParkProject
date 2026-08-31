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

public sealed class RatingAnomalySummaryDto
{
    public long NonNumericValueCount { get; set; }

    public long UnexpectedValueStorageTypeCount { get; set; }

    public long OutOfRangeValueCount { get; set; }

    public long NonHalfStepValueCount { get; set; }

    public long NearHalfStepValueCount { get; set; }

    public long MissingUserIdCount { get; set; }

    public long MissingTargetCount { get; set; }

    public long DuplicateVoteKeyCount { get; set; }

    public long ExtraDuplicateDocumentCount { get; set; }
}

public sealed class RatingAggregateIntegrityDto
{
    public bool IsSourceComparisonEvaluated { get; set; }

    public bool IsOrphanCheckEvaluated { get; set; }

    public long SourceTargetCount { get; set; }

    public long MissingAggregateCount { get; set; }

    public long DivergentAggregateCount { get; set; }

    public long ContributorCountMismatchCount { get; set; }

    public long DerivedScoreMismatchCount { get; set; }

    public long OrphanAggregateCount { get; set; }
}

public sealed class RatingTargetDistributionDto
{
    public string TargetType { get; set; } = string.Empty;

    public string EvidenceBand { get; set; } = string.Empty;

    public long TargetCount { get; set; }

    public long RatingObservationCount { get; set; }

    public long UniqueContributorCount { get; set; }
}

public sealed class RatingIndexStatusDto
{
    public string Collection { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsPresent { get; set; }

    public bool IsUnique { get; set; }

    public bool IsHidden { get; set; }

    public bool HasUnexpectedOptions { get; set; }

    public bool SupportsExpectedQueries { get; set; }

    public bool MatchesExpectedDefinition { get; set; }

    public string ExpectedKeys { get; set; } = string.Empty;

    public string? ActualKeys { get; set; }
}
