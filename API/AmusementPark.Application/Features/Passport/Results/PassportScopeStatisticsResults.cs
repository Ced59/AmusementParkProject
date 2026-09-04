namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportRatingCoverageResult(
    long RatedCount,
    long TotalCount,
    double Rate);

public sealed record PassportVisitExperienceResult(
    string VisitId,
    string ParkId,
    VisitDateResult Date);

public sealed record PassportRideOutcomeStatisticsResult(
    long RecordedOutcomeCount,
    long CompletedRideCount,
    long AttemptedCount,
    long MissedClosedCount,
    long MissedUnavailableCount,
    long SkippedByChoiceCount);

public sealed record PassportCategoryCoverageResult(
    string? Category,
    long CompletedRideCount,
    long DistinctItemCount,
    long HistoricalReferenceRideCount,
    long CurrentReferenceRideCount,
    long UnknownReferenceRideCount,
    double CompletedRideRate);

public sealed record PassportStatisticsSummaryResult(
    long VisitCount,
    long ApproximateVisitCount,
    PassportRatingCoverageResult ParkRatingCoverage,
    PassportRatingDistributionResult? HistoricalParkRatings,
    PassportVisitExperienceResult? FirstVisit,
    PassportVisitExperienceResult? LastVisit,
    PassportRideOutcomeStatisticsResult RideOutcomes,
    PassportRatingCoverageResult RideRatingCoverage,
    PassportRatingDistributionResult? HistoricalRideRatings,
    long DistinctCompletedItemCount,
    long RepeatedCompletedItemCount,
    IReadOnlyCollection<PassportCategoryCoverageResult> CategoryCoverage);

public sealed record PassportParkAssessmentPointResult(
    string VisitId,
    VisitDateResult Date,
    double Rating);

public sealed record PassportCurrentItemRatingResult(
    string ParkItemId,
    double Rating,
    string? ParkItemName = null);

public sealed record PassportHistoricalItemRatingResult(
    string ParkItemId,
    long RatingCount,
    double Average,
    string? ParkItemName = null);

public sealed record PassportYearBreakdownResult(
    int Year,
    PassportStatisticsSummaryResult Summary);

public sealed record PassportParkBreakdownResult(
    string ParkId,
    PassportStatisticsSummaryResult Summary,
    string? ParkName = null);

public sealed record PassportParkStatisticsResult(
    string ParkId,
    PassportStatisticsSummaryResult Summary,
    double? CurrentGlobalRating,
    double? CurrentGlobalMinusHistoricalAverage,
    IReadOnlyCollection<PassportParkAssessmentPointResult> AssessmentTimeline,
    IReadOnlyCollection<PassportYearBreakdownResult> ByYear,
    IReadOnlyCollection<PassportCurrentItemRatingResult> CurrentTopItems,
    IReadOnlyCollection<PassportHistoricalItemRatingResult> HistoricalTopItems,
    string? ParkName = null);

public sealed record PassportYearStatisticsResult(
    int Year,
    long ParkCount,
    PassportStatisticsSummaryResult Summary,
    IReadOnlyCollection<PassportParkBreakdownResult> ByPark);
