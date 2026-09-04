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
            Create("visit-b", VisitDate.ForDay(2025, 7, 2), 10),
            Create("visit-a", VisitDate.ForYear(2024, true), 2),
            Create("visit-b", VisitDate.ForDay(2025, 7, 2), 7),
            Create("visit-c", VisitDate.ForMonth(2026, 3), null),
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

        PassportItemRatingStatistics ratings = Assert.IsType<PassportItemRatingStatistics>(result.Ratings);
        Assert.Equal(3, ratings.RatingCount);
        Assert.Equal(19, ratings.HalfStepSum);
        Assert.Equal(19d / 6d, ratings.Average, 12);
        Assert.Equal(3.5d, ratings.Median);
        Assert.Equal(1d, ratings.Minimum);
        Assert.Equal(5d, ratings.Maximum);
        Assert.Equal(Math.Sqrt(32.666666666666664d / 3d) / 2d, ratings.PopulationStandardDeviation, 12);
        Assert.Equal(4.5d, result.CurrentGlobalRating?.DoubleValue);
        Assert.Equal(4.5d - (19d / 6d), result.CurrentGlobalMinusHistoricalAverage);
    }

    [Fact]
    public void Calculate_WithEvenRatingCount_ShouldAllowADerivedQuarterPointMedian()
    {
        PassportItemRideObservation[] observations =
        {
            Create("visit-a", VisitDate.ForDay(2025, 1, 1), 7),
            Create("visit-b", VisitDate.ForDay(2025, 1, 2), 8),
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
            Create("visit-day", VisitDate.ForDay(2025, 6, 15), null),
            Create("visit-year", VisitDate.ForYear(2025), null),
            Create("visit-month", VisitDate.ForMonth(2025, 6), null),
        };

        PassportItemStatistics result = PassportItemStatisticsCalculator.Calculate(
            observations,
            null);

        Assert.Equal(VisitDatePrecision.Year, result.FirstExperience?.VisitDate.Precision);
        Assert.Null(result.FirstExperience?.VisitDate.Month);
        Assert.Equal(VisitDatePrecision.Day, result.LastExperience?.VisitDate.Precision);
        Assert.Equal(15, result.LastExperience?.VisitDate.Day);
    }

    private static PassportItemRideObservation Create(
        string visitId,
        VisitDate visitDate,
        byte? ratingHalfSteps)
    {
        return new PassportItemRideObservation(
            visitId,
            visitDate,
            ratingHalfSteps.HasValue
                ? RatingValue.FromHalfSteps(ratingHalfSteps.Value)
                : null);
    }
}
