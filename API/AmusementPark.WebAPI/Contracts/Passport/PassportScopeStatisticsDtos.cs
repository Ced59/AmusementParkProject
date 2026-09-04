namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportRatingDistributionDto
{
    public long RatingCount { get; init; }
    public double Average { get; init; }
    public double Median { get; init; }
    public double Minimum { get; init; }
    public double Maximum { get; init; }
    public double PopulationStandardDeviation { get; init; }
}

public sealed class PassportRatingCoverageDto
{
    public long RatedCount { get; init; }
    public long TotalCount { get; init; }
    public double Rate { get; init; }
}

public sealed class PassportVisitExperienceDto
{
    public string VisitId { get; init; } = string.Empty;
    public string ParkId { get; init; } = string.Empty;
    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();
}

public sealed class PassportRideOutcomeStatisticsDto
{
    public long RecordedOutcomeCount { get; init; }
    public long CompletedRideCount { get; init; }
    public long AttemptedCount { get; init; }
    public long MissedClosedCount { get; init; }
    public long MissedUnavailableCount { get; init; }
    public long SkippedByChoiceCount { get; init; }
}

public sealed class PassportCategoryCoverageDto
{
    public string? Category { get; init; }
    public long CompletedRideCount { get; init; }
    public long DistinctItemCount { get; init; }
    public long HistoricalReferenceRideCount { get; init; }
    public long CurrentReferenceRideCount { get; init; }
    public long UnknownReferenceRideCount { get; init; }
    public double CompletedRideRate { get; init; }
}

public sealed class PassportStatisticsSummaryDto
{
    public long VisitCount { get; init; }
    public long ApproximateVisitCount { get; init; }
    public PassportRatingCoverageDto ParkRatingCoverage { get; init; } =
        new PassportRatingCoverageDto();
    public PassportRatingDistributionDto? HistoricalParkRatings { get; init; }
    public PassportVisitExperienceDto? FirstVisit { get; init; }
    public PassportVisitExperienceDto? LastVisit { get; init; }
    public PassportRideOutcomeStatisticsDto RideOutcomes { get; init; } =
        new PassportRideOutcomeStatisticsDto();
    public PassportRatingCoverageDto RideRatingCoverage { get; init; } =
        new PassportRatingCoverageDto();
    public PassportRatingDistributionDto? HistoricalRideRatings { get; init; }
    public long DistinctCompletedItemCount { get; init; }
    public long RepeatedCompletedItemCount { get; init; }
    public IReadOnlyCollection<PassportCategoryCoverageDto> CategoryCoverage { get; init; } =
        Array.Empty<PassportCategoryCoverageDto>();
}

public sealed class PassportParkAssessmentPointDto
{
    public string VisitId { get; init; } = string.Empty;
    public PassportVisitDateDto Date { get; init; } = new PassportVisitDateDto();
    public double Rating { get; init; }
}

public sealed class PassportCurrentItemRatingDto
{
    public string ParkItemId { get; init; } = string.Empty;
    public double Rating { get; init; }
}

public sealed class PassportHistoricalItemRatingDto
{
    public string ParkItemId { get; init; } = string.Empty;
    public long RatingCount { get; init; }
    public double Average { get; init; }
}

public sealed class PassportYearBreakdownDto
{
    public int Year { get; init; }
    public PassportStatisticsSummaryDto Summary { get; init; } =
        new PassportStatisticsSummaryDto();
}

public sealed class PassportParkBreakdownDto
{
    public string ParkId { get; init; } = string.Empty;
    public PassportStatisticsSummaryDto Summary { get; init; } =
        new PassportStatisticsSummaryDto();
}

public sealed class PassportParkStatisticsDto
{
    public string ParkId { get; init; } = string.Empty;
    public PassportStatisticsSummaryDto Summary { get; init; } =
        new PassportStatisticsSummaryDto();
    public double? CurrentGlobalRating { get; init; }
    public double? CurrentGlobalMinusHistoricalAverage { get; init; }
    public IReadOnlyCollection<PassportParkAssessmentPointDto> AssessmentTimeline { get; init; } =
        Array.Empty<PassportParkAssessmentPointDto>();
    public IReadOnlyCollection<PassportYearBreakdownDto> ByYear { get; init; } =
        Array.Empty<PassportYearBreakdownDto>();
    public IReadOnlyCollection<PassportCurrentItemRatingDto> CurrentTopItems { get; init; } =
        Array.Empty<PassportCurrentItemRatingDto>();
    public IReadOnlyCollection<PassportHistoricalItemRatingDto> HistoricalTopItems { get; init; } =
        Array.Empty<PassportHistoricalItemRatingDto>();
}

public sealed class PassportYearStatisticsDto
{
    public int Year { get; init; }
    public long ParkCount { get; init; }
    public PassportStatisticsSummaryDto Summary { get; init; } =
        new PassportStatisticsSummaryDto();
    public IReadOnlyCollection<PassportParkBreakdownDto> ByPark { get; init; } =
        Array.Empty<PassportParkBreakdownDto>();
}
