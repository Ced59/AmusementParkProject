using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class PassportGlobalStatisticsCalculatorTests
{
    [Fact]
    public void Calculate_ShouldAggregateActivityRankingsOutcomesAndNullableRatings()
    {
        PassportVisitStatisticsObservation[] visits =
        {
            Visit("visit-1", "park-a", 2024, 8),
            Visit("visit-2", "park-a", 2025, null),
            Visit("visit-3", "park-b", 2025, 10),
        };
        PassportRideStatisticsObservation[] rides =
        {
            Ride("ride-1", "visit-1", "park-a", "item-a", 2024, RideOccurrenceStatus.Completed, 6),
            Ride("ride-2", "visit-2", "park-a", "item-a", 2025, RideOccurrenceStatus.Completed, null),
            Ride("ride-3", "visit-3", "park-b", "item-b", 2025, RideOccurrenceStatus.MissedClosed, null),
        };

        PassportGlobalStatistics result = PassportGlobalStatisticsCalculator.Calculate(visits, rides);

        Assert.Equal(2, result.ParkCount);
        Assert.Equal(3, result.Summary.VisitCount);
        Assert.Equal(3, result.Summary.RideOutcomes.RecordedOutcomeCount);
        Assert.Equal(2, result.Summary.RideOutcomes.CompletedRideCount);
        Assert.Equal(1, result.Summary.RideOutcomes.MissedClosedCount);
        Assert.Equal(2, result.ActivityByYear.Count);
        Assert.Equal(2, result.ActivityByYear.Last().VisitCount);
        Assert.Equal(2, result.ActivityByYear.Last().RecordedRideCount);
        Assert.Equal("park-a", result.TopParks.First().ParkId);
        Assert.Equal(2, result.TopParks.First().VisitCount);
        Assert.Equal(2, result.TopParks.First().RecordedRideCount);
        Assert.Equal("item-a", Assert.Single(result.TopItems).ParkItemId);
        Assert.Equal(2, Assert.Single(result.TopItems).CompletedRideCount);
        Assert.Equal(4d, result.RatingEvolution.First().ParkAverage);
        Assert.Equal(3d, result.RatingEvolution.First().RideAverage);
        Assert.Equal(5d, result.RatingEvolution.Last().ParkAverage);
        Assert.Null(result.RatingEvolution.Last().RideAverage);
    }

    [Fact]
    public void Calculate_WhenRideReferencesOutsideVisitScope_ShouldRejectEvidence()
    {
        PassportRideStatisticsObservation ride = Ride(
            "ride-1",
            "missing-visit",
            "park-a",
            "item-a",
            2025,
            RideOccurrenceStatus.Completed,
            null);

        Assert.Throws<ArgumentException>(() => PassportGlobalStatisticsCalculator.Calculate(
            Array.Empty<PassportVisitStatisticsObservation>(),
            new[] { ride }));
    }

    private static PassportVisitStatisticsObservation Visit(
        string id,
        string parkId,
        int year,
        byte? rating)
    {
        return new PassportVisitStatisticsObservation(
            id,
            parkId,
            VisitDate.ForYear(year),
            rating.HasValue ? RatingValue.FromHalfSteps(rating.Value) : null);
    }

    private static PassportRideStatisticsObservation Ride(
        string id,
        string visitId,
        string parkId,
        string parkItemId,
        int year,
        RideOccurrenceStatus status,
        byte? rating)
    {
        return new PassportRideStatisticsObservation(
            id,
            visitId,
            parkId,
            parkItemId,
            VisitDate.ForYear(year),
            status,
            rating.HasValue ? RatingValue.FromHalfSteps(rating.Value) : null,
            "Attraction",
            "Attraction");
    }
}
