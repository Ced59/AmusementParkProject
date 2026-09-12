namespace AmusementPark.Application.Features.Passport.Results;

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
