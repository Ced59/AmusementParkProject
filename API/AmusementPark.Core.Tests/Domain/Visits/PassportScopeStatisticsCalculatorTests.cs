using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class PassportScopeStatisticsCalculatorTests
{
    [Fact]
    public void CalculatePark_ShouldExposeDenominatorsDistributionsAndSeparatedTops()
    {
        PassportVisitStatisticsObservation[] visits =
        {
            Visit("visit-1", "park-1", VisitDate.ForYear(2024, true), 6),
            Visit("visit-2", "park-1", VisitDate.ForDay(2025, 6, 2), 8),
            Visit("visit-3", "park-1", VisitDate.ForMonth(2025, 8), null),
        };
        PassportRideStatisticsObservation[] rides =
        {
            Ride("occ-1", "visit-1", "park-1", "item-a", VisitDate.ForYear(2024, true), RideOccurrenceStatus.Completed, 8, null, "Attraction"),
            Ride("occ-2", "visit-2", "park-1", "item-a", VisitDate.ForDay(2025, 6, 2), RideOccurrenceStatus.Completed, 10, "Historic", "Attraction"),
            Ride("occ-3", "visit-2", "park-1", "item-b", VisitDate.ForDay(2025, 6, 2), RideOccurrenceStatus.Completed, null, null, null),
            Ride("occ-4", "visit-3", "park-1", "item-c", VisitDate.ForMonth(2025, 8), RideOccurrenceStatus.MissedClosed, null, null, "Attraction"),
            Ride("occ-5", "visit-3", "park-1", "item-d", VisitDate.ForMonth(2025, 8), RideOccurrenceStatus.MissedUnavailable, null, null, "Attraction"),
        };

        PassportParkStatistics result = PassportScopeStatisticsCalculator.CalculatePark(
            "park-1",
            visits,
            rides,
            RatingValue.FromHalfSteps(9),
            new[]
            {
                new PassportCurrentItemRatingObservation("item-b", RatingValue.FromHalfSteps(10)),
                new PassportCurrentItemRatingObservation("item-a", RatingValue.FromHalfSteps(7)),
            });

        Assert.Equal(3, result.Summary.VisitCount);
        Assert.Equal(1, result.Summary.ApproximateVisitCount);
        Assert.Equal(2, result.Summary.RatedVisitCount);
        Assert.Equal(2d / 3d, result.Summary.ParkRatingCoverageRate, 12);
        Assert.Equal(3.5d, result.Summary.ParkRatings?.Average);
        Assert.Equal(5, result.Summary.RideOutcomes.RecordedOutcomeCount);
        Assert.Equal(3, result.Summary.RideOutcomes.CompletedRideCount);
        Assert.Equal(1, result.Summary.RideOutcomes.MissedClosedCount);
        Assert.Equal(1, result.Summary.RideOutcomes.MissedUnavailableCount);
        Assert.Equal(2, result.Summary.RatedRideCount);
        Assert.Equal(2d / 3d, result.Summary.RideRatingCoverageRate, 12);
        Assert.Equal(2, result.Summary.DistinctCompletedItemCount);
        Assert.Equal(1, result.Summary.RepeatedCompletedItemCount);
        Assert.Equal(1d, result.CurrentGlobalMinusHistoricalAverage);
        Assert.Equal(new[] { 2024, 2025 }, result.ByYear.Select(static value => value.Year));
        Assert.Equal("item-b", result.CurrentTopItems.First().ParkItemId);
        Assert.Equal("item-a", result.HistoricalTopItems.First().ParkItemId);
        Assert.Equal(2, result.AssessmentTimeline.Count);

        PassportCategoryCoverage historical = Assert.Single(
            result.Summary.CategoryCoverage,
            static value => value.Category == "Historic");
        Assert.Equal(1, historical.HistoricalReferenceRideCount);
        PassportCategoryCoverage current = Assert.Single(
            result.Summary.CategoryCoverage,
            static value => value.Category == "Attraction");
        Assert.Equal(1, current.CurrentReferenceRideCount);
        PassportCategoryCoverage unknown = Assert.Single(
            result.Summary.CategoryCoverage,
            static value => value.Category is null);
        Assert.Equal(1, unknown.UnknownReferenceRideCount);
    }

    [Fact]
    public void CalculateYear_ShouldGroupParksAndKeepZeroDenominators()
    {
        PassportVisitStatisticsObservation[] visits =
        {
            Visit("visit-a", "park-a", VisitDate.ForMonth(2025, 4), null),
            Visit("visit-b", "park-b", VisitDate.ForDay(2025, 7, 1), null),
        };

        PassportYearStatistics result = PassportScopeStatisticsCalculator.CalculateYear(
            2025,
            visits,
            Array.Empty<PassportRideStatisticsObservation>());

        Assert.Equal(2, result.ParkCount);
        Assert.Equal(2, result.Summary.VisitCount);
        Assert.Equal(0d, result.Summary.ParkRatingCoverageRate);
        Assert.Equal(0d, result.Summary.RideRatingCoverageRate);
        Assert.Equal(new[] { "park-a", "park-b" }, result.ByPark.Select(static value => value.ParkId));
    }

    [Fact]
    public void CalculateYear_WithObservationOutsideTheYear_ShouldRejectTheScope()
    {
        PassportVisitStatisticsObservation[] visits =
        {
            Visit("visit-a", "park-a", VisitDate.ForYear(2024), null),
        };

        Assert.Throws<ArgumentException>(() =>
            PassportScopeStatisticsCalculator.CalculateYear(
                2025,
                visits,
                Array.Empty<PassportRideStatisticsObservation>()));
    }

    [Fact]
    public void CalculatePark_WithRideOutsideTheVisitSet_ShouldRejectTheEvidence()
    {
        PassportVisitStatisticsObservation[] visits =
        {
            Visit("visit-a", "park-a", VisitDate.ForYear(2025), null),
        };
        PassportRideStatisticsObservation[] rides =
        {
            Ride(
                "occ-a",
                "visit-missing",
                "park-a",
                "item-a",
                VisitDate.ForYear(2025),
                RideOccurrenceStatus.Completed,
                null,
                null,
                "Attraction"),
        };

        Assert.Throws<ArgumentException>(() =>
            PassportScopeStatisticsCalculator.CalculatePark(
                "park-a",
                visits,
                rides,
                null,
                Array.Empty<PassportCurrentItemRatingObservation>()));
    }

    private static PassportVisitStatisticsObservation Visit(
        string visitId,
        string parkId,
        VisitDate date,
        byte? ratingHalfSteps)
    {
        return new PassportVisitStatisticsObservation(
            visitId,
            parkId,
            date,
            ToRating(ratingHalfSteps));
    }

    private static PassportRideStatisticsObservation Ride(
        string occurrenceId,
        string visitId,
        string parkId,
        string parkItemId,
        VisitDate date,
        RideOccurrenceStatus status,
        byte? ratingHalfSteps,
        string? historicalCategory,
        string? currentCategory)
    {
        return new PassportRideStatisticsObservation(
            occurrenceId,
            visitId,
            parkId,
            parkItemId,
            date,
            status,
            ToRating(ratingHalfSteps),
            historicalCategory,
            currentCategory);
    }

    private static RatingValue? ToRating(byte? halfSteps)
    {
        return halfSteps.HasValue ? RatingValue.FromHalfSteps(halfSteps.Value) : null;
    }
}
