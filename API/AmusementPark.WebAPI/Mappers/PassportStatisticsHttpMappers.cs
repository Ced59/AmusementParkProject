using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.WebAPI.Contracts.Passport;

namespace AmusementPark.WebAPI.Mappers;

internal static class PassportStatisticsHttpMappers
{
    public static PassportItemStatisticsDto ToHttp(
        this PassportItemStatisticsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new PassportItemStatisticsDto
        {
            ParkItemId = result.ParkItemId,
            RideCount = result.RideCount,
            VisitCount = result.VisitCount,
            RatingCoverage = ToHttp(result.RatingCoverage),
            FirstExperience = ToHttp(result.FirstExperience),
            LastExperience = ToHttp(result.LastExperience),
            HistoricalRatings = ToHttp(result.HistoricalRatings),
            CurrentGlobalRating = result.CurrentGlobalRating,
            CurrentGlobalMinusHistoricalAverage =
                result.CurrentGlobalMinusHistoricalAverage,
            ByVisit = result.ByVisit.Select(static item => new PassportItemVisitStatisticsDto
            {
                VisitId = item.VisitId,
                Date = ToHttp(item.Date),
                RideCount = item.RideCount,
                RatingCoverage = ToHttp(item.RatingCoverage),
                HistoricalRatings = ToHttp(item.HistoricalRatings),
            }).ToArray(),
            ByYear = result.ByYear.Select(static item => new PassportItemYearStatisticsDto
            {
                Year = item.Year,
                RideCount = item.RideCount,
                VisitCount = item.VisitCount,
                RatingCoverage = ToHttp(item.RatingCoverage),
                HistoricalRatings = ToHttp(item.HistoricalRatings),
            }).ToArray(),
            RatingTimeline = result.RatingTimeline.Select(static point =>
                new PassportItemRatingPointDto
                {
                    RideOccurrenceId = point.RideOccurrenceId,
                    VisitId = point.VisitId,
                    Date = ToHttp(point.Date),
                    SortPosition = point.SortPosition,
                    Rating = point.Rating,
                }).ToArray(),
            Trend = result.Trend is null
                ? null
                : new PassportRatingTrendDto
                {
                    Kind = (PassportRatingTrendKindDto)result.Trend.Kind,
                    FirstWindowRatingCount = result.Trend.FirstWindowRatingCount,
                    LastWindowRatingCount = result.Trend.LastWindowRatingCount,
                    FirstWindowAverage = result.Trend.FirstWindowAverage,
                    LastWindowAverage = result.Trend.LastWindowAverage,
                    Delta = result.Trend.Delta,
                },
        };
    }

    public static PassportParkStatisticsDto ToHttp(
        this PassportParkStatisticsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new PassportParkStatisticsDto
        {
            ParkId = result.ParkId,
            Summary = ToHttp(result.Summary),
            CurrentGlobalRating = result.CurrentGlobalRating,
            CurrentGlobalMinusHistoricalAverage =
                result.CurrentGlobalMinusHistoricalAverage,
            AssessmentTimeline = result.AssessmentTimeline.Select(static point =>
                new PassportParkAssessmentPointDto
                {
                    VisitId = point.VisitId,
                    Date = ToHttp(point.Date),
                    Rating = point.Rating,
                }).ToArray(),
            ByYear = result.ByYear.Select(static item => new PassportYearBreakdownDto
            {
                Year = item.Year,
                Summary = ToHttp(item.Summary),
            }).ToArray(),
            CurrentTopItems = result.CurrentTopItems.Select(static item =>
                new PassportCurrentItemRatingDto
                {
                    ParkItemId = item.ParkItemId,
                    Rating = item.Rating,
                }).ToArray(),
            HistoricalTopItems = result.HistoricalTopItems.Select(static item =>
                new PassportHistoricalItemRatingDto
                {
                    ParkItemId = item.ParkItemId,
                    RatingCount = item.RatingCount,
                    Average = item.Average,
                }).ToArray(),
        };
    }

    public static PassportYearStatisticsDto ToHttp(
        this PassportYearStatisticsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new PassportYearStatisticsDto
        {
            Year = result.Year,
            ParkCount = result.ParkCount,
            Summary = ToHttp(result.Summary),
            ByPark = result.ByPark.Select(static item => new PassportParkBreakdownDto
            {
                ParkId = item.ParkId,
                Summary = ToHttp(item.Summary),
            }).ToArray(),
        };
    }

    private static PassportItemRatingCoverageDto ToHttp(
        PassportItemRatingCoverageResult result)
    {
        return new PassportItemRatingCoverageDto
        {
            RatedRideCount = result.RatedRideCount,
            TotalRideCount = result.TotalRideCount,
            Rate = result.Rate,
        };
    }

    private static PassportItemHistoricalRatingsDto? ToHttp(
        PassportRatingDistributionResult? result)
    {
        return result is null
            ? null
            : new PassportItemHistoricalRatingsDto
            {
                RatingCount = result.RatingCount,
                Average = result.Average,
                Median = result.Median,
                Minimum = result.Minimum,
                Maximum = result.Maximum,
                PopulationStandardDeviation = result.PopulationStandardDeviation,
            };
    }

    private static PassportItemExperienceDto? ToHttp(
        PassportItemExperienceResult? result)
    {
        return result is null
            ? null
            : new PassportItemExperienceDto
            {
                VisitId = result.VisitId,
                Date = ToHttp(result.Date),
            };
    }

    private static PassportStatisticsSummaryDto ToHttp(
        PassportStatisticsSummaryResult result)
    {
        return new PassportStatisticsSummaryDto
        {
            VisitCount = result.VisitCount,
            ApproximateVisitCount = result.ApproximateVisitCount,
            ParkRatingCoverage = ToHttp(result.ParkRatingCoverage),
            HistoricalParkRatings = ToDistributionHttp(result.HistoricalParkRatings),
            FirstVisit = ToHttp(result.FirstVisit),
            LastVisit = ToHttp(result.LastVisit),
            RideOutcomes = new PassportRideOutcomeStatisticsDto
            {
                RecordedOutcomeCount = result.RideOutcomes.RecordedOutcomeCount,
                CompletedRideCount = result.RideOutcomes.CompletedRideCount,
                AttemptedCount = result.RideOutcomes.AttemptedCount,
                MissedClosedCount = result.RideOutcomes.MissedClosedCount,
                MissedUnavailableCount = result.RideOutcomes.MissedUnavailableCount,
                SkippedByChoiceCount = result.RideOutcomes.SkippedByChoiceCount,
            },
            RideRatingCoverage = ToHttp(result.RideRatingCoverage),
            HistoricalRideRatings = ToDistributionHttp(result.HistoricalRideRatings),
            DistinctCompletedItemCount = result.DistinctCompletedItemCount,
            RepeatedCompletedItemCount = result.RepeatedCompletedItemCount,
            CategoryCoverage = result.CategoryCoverage.Select(static category =>
                new PassportCategoryCoverageDto
                {
                    Category = category.Category,
                    CompletedRideCount = category.CompletedRideCount,
                    DistinctItemCount = category.DistinctItemCount,
                    HistoricalReferenceRideCount = category.HistoricalReferenceRideCount,
                    CurrentReferenceRideCount = category.CurrentReferenceRideCount,
                    UnknownReferenceRideCount = category.UnknownReferenceRideCount,
                    CompletedRideRate = category.CompletedRideRate,
                }).ToArray(),
        };
    }

    private static PassportRatingCoverageDto ToHttp(PassportRatingCoverageResult result)
    {
        return new PassportRatingCoverageDto
        {
            RatedCount = result.RatedCount,
            TotalCount = result.TotalCount,
            Rate = result.Rate,
        };
    }

    private static PassportRatingDistributionDto? ToDistributionHttp(
        PassportRatingDistributionResult? result)
    {
        return result is null
            ? null
            : new PassportRatingDistributionDto
            {
                RatingCount = result.RatingCount,
                Average = result.Average,
                Median = result.Median,
                Minimum = result.Minimum,
                Maximum = result.Maximum,
                PopulationStandardDeviation = result.PopulationStandardDeviation,
            };
    }

    private static PassportVisitExperienceDto? ToHttp(
        PassportVisitExperienceResult? result)
    {
        return result is null
            ? null
            : new PassportVisitExperienceDto
            {
                VisitId = result.VisitId,
                ParkId = result.ParkId,
                Date = ToHttp(result.Date),
            };
    }

    private static PassportVisitDateDto ToHttp(VisitDateResult result)
    {
        return new PassportVisitDateDto
        {
            Year = result.Year,
            Month = result.Month,
            Day = result.Day,
            Precision = (PassportVisitDatePrecisionDto)result.Precision,
            IsApproximate = result.IsApproximate,
        };
    }
}
