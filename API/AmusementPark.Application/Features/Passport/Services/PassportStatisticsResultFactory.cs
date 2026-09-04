using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportStatisticsResultFactory
{
    public static PassportItemStatisticsResult CreateItem(
        string parkItemId,
        PassportItemStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        return new PassportItemStatisticsResult(
            parkItemId,
            statistics.RideCount,
            statistics.VisitCount,
            new PassportItemRatingCoverageResult(
                statistics.RatedRideCount,
                statistics.RideCount,
                statistics.RatingCoverageRate),
            ToResult(statistics.FirstExperience),
            ToResult(statistics.LastExperience),
            ToResult(statistics.Ratings),
            statistics.CurrentGlobalRating?.DoubleValue,
            statistics.CurrentGlobalMinusHistoricalAverage,
            statistics.ByVisit.Select(static item => new PassportItemVisitStatisticsResult(
                item.VisitId,
                ToResult(item.VisitDate),
                item.RideCount,
                new PassportItemRatingCoverageResult(
                    item.RatedRideCount,
                    item.RideCount,
                    item.RatingCoverageRate),
                ToResult(item.Ratings))).ToArray(),
            statistics.ByYear.Select(static item => new PassportItemYearStatisticsResult(
                item.Year,
                item.RideCount,
                item.VisitCount,
                new PassportItemRatingCoverageResult(
                    item.RatedRideCount,
                    item.RideCount,
                    item.RatingCoverageRate),
                ToResult(item.Ratings))).ToArray(),
            statistics.RatingTimeline.Select(static point => new PassportItemRatingPointResult(
                point.RideOccurrenceId,
                point.VisitId,
                ToResult(point.VisitDate),
                point.SortPosition,
                point.Rating.DoubleValue)).ToArray(),
            statistics.Trend is null
                ? null
                : new PassportRatingTrendResult(
                    statistics.Trend.Kind,
                    statistics.Trend.FirstWindowRatingCount,
                    statistics.Trend.LastWindowRatingCount,
                    statistics.Trend.FirstWindowAverage,
                    statistics.Trend.LastWindowAverage,
                    statistics.Trend.Delta));
    }

    public static PassportParkStatisticsResult CreatePark(PassportParkStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        return new PassportParkStatisticsResult(
            statistics.ParkId,
            ToResult(statistics.Summary),
            statistics.CurrentGlobalRating?.DoubleValue,
            statistics.CurrentGlobalMinusHistoricalAverage,
            statistics.AssessmentTimeline.Select(static point =>
                new PassportParkAssessmentPointResult(
                    point.VisitId,
                    ToResult(point.VisitDate),
                    point.Rating.DoubleValue)).ToArray(),
            statistics.ByYear.Select(static item => new PassportYearBreakdownResult(
                item.Year,
                ToResult(item.Summary))).ToArray(),
            statistics.CurrentTopItems.Select(static item =>
                new PassportCurrentItemRatingResult(
                    item.ParkItemId,
                    item.Rating.DoubleValue)).ToArray(),
            statistics.HistoricalTopItems.Select(static item =>
                new PassportHistoricalItemRatingResult(
                    item.ParkItemId,
                    item.RatingCount,
                    item.Average)).ToArray());
    }

    public static PassportYearStatisticsResult CreateYear(PassportYearStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        return new PassportYearStatisticsResult(
            statistics.Year,
            statistics.ParkCount,
            ToResult(statistics.Summary),
            statistics.ByPark.Select(static item => new PassportParkBreakdownResult(
                item.ParkId,
                ToResult(item.Summary))).ToArray());
    }

    private static PassportItemExperienceResult? ToResult(
        PassportItemExperience? experience)
    {
        return experience is null
            ? null
            : new PassportItemExperienceResult(
                experience.VisitId,
                ToResult(experience.VisitDate));
    }

    private static PassportVisitExperienceResult? ToResult(
        PassportVisitExperience? experience)
    {
        return experience is null
            ? null
            : new PassportVisitExperienceResult(
                experience.VisitId,
                experience.ParkId,
                ToResult(experience.VisitDate));
    }

    private static PassportStatisticsSummaryResult ToResult(
        PassportStatisticsSummary summary)
    {
        return new PassportStatisticsSummaryResult(
            summary.VisitCount,
            summary.ApproximateVisitCount,
            new PassportRatingCoverageResult(
                summary.RatedVisitCount,
                summary.VisitCount,
                summary.ParkRatingCoverageRate),
            ToResult(summary.ParkRatings),
            ToResult(summary.FirstVisit),
            ToResult(summary.LastVisit),
            new PassportRideOutcomeStatisticsResult(
                summary.RideOutcomes.RecordedOutcomeCount,
                summary.RideOutcomes.CompletedRideCount,
                summary.RideOutcomes.AttemptedCount,
                summary.RideOutcomes.MissedClosedCount,
                summary.RideOutcomes.MissedUnavailableCount,
                summary.RideOutcomes.SkippedByChoiceCount),
            new PassportRatingCoverageResult(
                summary.RatedRideCount,
                summary.RideOutcomes.CompletedRideCount,
                summary.RideRatingCoverageRate),
            ToResult(summary.RideRatings),
            summary.DistinctCompletedItemCount,
            summary.RepeatedCompletedItemCount,
            summary.CategoryCoverage.Select(static category =>
                new PassportCategoryCoverageResult(
                    category.Category,
                    category.CompletedRideCount,
                    category.DistinctItemCount,
                    category.HistoricalReferenceRideCount,
                    category.CurrentReferenceRideCount,
                    category.UnknownReferenceRideCount,
                    category.CompletedRideRate)).ToArray());
    }

    private static PassportRatingDistributionResult? ToResult(
        PassportRatingStatistics? ratings)
    {
        return ratings is null
            ? null
            : new PassportRatingDistributionResult(
                ratings.RatingCount,
                ratings.Average,
                ratings.Median,
                ratings.Minimum,
                ratings.Maximum,
                ratings.PopulationStandardDeviation);
    }

    private static VisitDateResult ToResult(VisitDate date)
    {
        return new VisitDateResult(
            date.Year,
            date.Month,
            date.Day,
            date.Precision,
            date.IsApproximate);
    }
}
