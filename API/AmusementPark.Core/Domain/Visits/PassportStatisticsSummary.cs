namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportStatisticsSummary(
    long VisitCount,
    long ApproximateVisitCount,
    long RatedVisitCount,
    double ParkRatingCoverageRate,
    PassportRatingStatistics? ParkRatings,
    PassportVisitExperience? FirstVisit,
    PassportVisitExperience? LastVisit,
    PassportRideOutcomeStatistics RideOutcomes,
    long RatedRideCount,
    double RideRatingCoverageRate,
    PassportRatingStatistics? RideRatings,
    long DistinctCompletedItemCount,
    long RepeatedCompletedItemCount,
    IReadOnlyCollection<PassportCategoryCoverage> CategoryCoverage);
