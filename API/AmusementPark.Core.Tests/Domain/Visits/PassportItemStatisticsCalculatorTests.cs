using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class PassportItemStatisticsCalculatorTests
{
    [Fact]
    public void Calculate_WhenNoRideExists_ShouldReturnExplicitZeroDenominators()
    {
        PassportItemStatistics result = PassportItemStatisticsCalculator.Calculate(
            Array.Empty<PassportItemRideObservation>(),
            RatingValue.FromHalfSteps(9));

        Assert.Equal(0, result.RideCount);
        Assert.Equal(0, result.VisitCount);
        Assert.Equal(0, result.RatedRideCount);
        Assert.Equal(0d, result.RatingCoverageRate);
        Assert.Null(result.FirstExperience);
        Assert.Null(result.LastExperience);
        Assert.Null(result.Ratings);
        Assert.Equal(4.5d, result.CurrentGlobalRating?.DoubleValue);
        Assert.Null(result.CurrentGlobalMinusHistoricalAverage);
    }

    [Fact]
    public void Calculate_ShouldKeepExactHalfStepsAndMatchIndependentFixture()
    {
        PassportItemRideObservation[] observations =
        {
            Create("occ-b-2", "visit-b", VisitDate.ForDay(2025, 7, 2), 2048, 10),
            Create("occ-a-1", "visit-a", VisitDate.ForYear(2024, true), 1024, 2),
            Create("occ-b-1", "visit-b", VisitDate.ForDay(2025, 7, 2), 1024, 7),
            Create("occ-c-1", "visit-c", VisitDate.ForMonth(2026, 3), 1024, null),
        };

        PassportItemStatistics result = PassportItemStatisticsCalculator.Calculate(
            observations,
            RatingValue.FromHalfSteps(9));

        Assert.Equal(4, result.RideCount);
        Assert.Equal(3, result.VisitCount);
        Assert.Equal(3, result.RatedRideCount);
        Assert.Equal(0.75d, result.RatingCoverageRate);
        Assert.Equal("visit-a", result.FirstExperience?.VisitId);
        Assert.Equal(VisitDatePrecision.Year, result.FirstExperience?.VisitDate.Precision);
        Assert.True(result.FirstExperience?.VisitDate.IsApproximate);
        Assert.Equal("visit-c", result.LastExperience?.VisitId);
        Assert.Equal(VisitDatePrecision.Month, result.LastExperience?.VisitDate.Precision);

        PassportRatingStatistics ratings = Assert.IsType<PassportRatingStatistics>(result.Ratings);
        Assert.Equal(3, ratings.RatingCount);
        Assert.Equal(19, ratings.HalfStepSum);
        Assert.Equal(19d / 6d, ratings.Average, 12);
        Assert.Equal(3.5d, ratings.Median);
        Assert.Equal(1d, ratings.Minimum);
        Assert.Equal(5d, ratings.Maximum);
        Assert.Equal(Math.Sqrt(32.666666666666664d / 3d) / 2d, ratings.PopulationStandardDeviation, 12);
        Assert.Equal(4.5d, result.CurrentGlobalRating?.DoubleValue);
        Assert.Equal(4.5d - (19d / 6d), result.CurrentGlobalMinusHistoricalAverage);
        Assert.Equal(3, result.ByVisit.Count);
        PassportItemVisitStatistics visitB = Assert.Single(
            result.ByVisit,
            static visit => visit.VisitId == "visit-b");
        Assert.Equal(2, visitB.RideCount);
        Assert.Equal(2, visitB.RatedRideCount);
        Assert.Equal(4.25d, visitB.Ratings!.Average);
        Assert.Equal(new[] { 2024, 2025, 2026 }, result.ByYear.Select(static year => year.Year));
        Assert.Collection(
            result.RatingTimeline,
            point => Assert.Equal("occ-a-1", point.RideOccurrenceId),
            point => Assert.Equal("occ-b-1", point.RideOccurrenceId),
            point => Assert.Equal("occ-b-2", point.RideOccurrenceId));
        Assert.Equal(PassportRatingTrendKind.Rising, result.Trend?.Kind);
        Assert.Equal(1d, result.Trend?.FirstWindowAverage);
        Assert.Equal(5d, result.Trend?.LastWindowAverage);
    }

    [Fact]
    public void Calculate_WithEvenRatingCount_ShouldAllowADerivedQuarterPointMedian()
    {
        PassportItemRideObservation[] observations =
        {
            Create("occ-a", "visit-a", VisitDate.ForDay(2025, 1, 1), 1024, 7),
            Create("occ-b", "visit-b", VisitDate.ForDay(2025, 1, 2), 1024, 8),
        };

        PassportItemStatistics result = PassportItemStatisticsCalculator.Calculate(
            observations,
            null);

        Assert.Equal(3.75d, result.Ratings?.Median);
        Assert.Equal(3.75d, result.Ratings?.Average);
        Assert.Equal(0.25d, result.Ratings?.PopulationStandardDeviation);
    }

    [Fact]
    public void Calculate_ShouldOrderPartialDatesWithoutInventingMissingCalendarParts()
    {
        PassportItemRideObservation[] observations =
        {
            Create("occ-day", "visit-day", VisitDate.ForDay(2025, 6, 15), 1024, null),
            Create("occ-year", "visit-year", VisitDate.ForYear(2025), 1024, null),
            Create("occ-month", "visit-month", VisitDate.ForMonth(2025, 6), 1024, null),
        };

        PassportItemStatistics result = PassportItemStatisticsCalculator.Calculate(
            observations,
            null);

        Assert.Equal(VisitDatePrecision.Year, result.FirstExperience?.VisitDate.Precision);
        Assert.Null(result.FirstExperience?.VisitDate.Month);
        Assert.Equal(VisitDatePrecision.Day, result.LastExperience?.VisitDate.Precision);
        Assert.Equal(15, result.LastExperience?.VisitDate.Day);
    }

    [Fact]
    public void Calculate_WithTooFewRatingsOrOneVisit_ShouldNotInferATrend()
    {
        PassportItemStatistics tooFew = PassportItemStatisticsCalculator.Calculate(
            new[]
            {
                Create("occ-a", "visit-a", VisitDate.ForYear(2024), 1024, 6),
                Create("occ-b", "visit-b", VisitDate.ForYear(2025), 1024, 8),
            },
            null);
        PassportItemStatistics oneVisit = PassportItemStatisticsCalculator.Calculate(
            new[]
            {
                Create("occ-a", "visit-a", VisitDate.ForYear(2025), 1024, 6),
                Create("occ-b", "visit-a", VisitDate.ForYear(2025), 2048, 8),
                Create("occ-c", "visit-a", VisitDate.ForYear(2025), 3072, 10),
            },
            null);

        Assert.Null(tooFew.Trend);
        Assert.Null(oneVisit.Trend);
    }

    private static PassportItemRideObservation Create(
        string occurrenceId,
        string visitId,
        VisitDate visitDate,
        long sortPosition,
        byte? ratingHalfSteps)
    {
        return new PassportItemRideObservation(
            occurrenceId,
            visitId,
            visitDate,
            sortPosition,
            ratingHalfSteps.HasValue
                ? RatingValue.FromHalfSteps(ratingHalfSteps.Value)
                : null);
    }
}
