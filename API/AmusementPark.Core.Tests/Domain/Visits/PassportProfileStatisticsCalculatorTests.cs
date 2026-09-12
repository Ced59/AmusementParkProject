using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class PassportProfileStatisticsCalculatorTests
{
    [Fact]
    public void Calculate_ShouldBuildCompletedActivityAndRatingCoverageByYearAndPark()
    {
        PassportVisitStatisticsObservation[] visits =
        {
            new PassportVisitStatisticsObservation(
                "visit-1",
                "park-1",
                VisitDate.ForDay(2025, 6, 1),
                RatingValue.FromDouble(4)),
            new PassportVisitStatisticsObservation(
                "visit-2",
                "park-1",
                VisitDate.ForDay(2026, 7, 1),
                null),
            new PassportVisitStatisticsObservation(
                "visit-3",
                "park-2",
                VisitDate.ForDay(2026, 8, 1),
                RatingValue.FromDouble(5)),
        };
        PassportRideStatisticsObservation[] rides =
        {
            CreateRide("ride-1", "visit-1", "park-1", 2025, RideOccurrenceStatus.Completed),
            CreateRide("ride-2", "visit-2", "park-1", 2026, RideOccurrenceStatus.MissedClosed),
            CreateRide("ride-3", "visit-3", "park-2", 2026, RideOccurrenceStatus.Completed),
        };

        PassportProfileStatistics result = PassportProfileStatisticsCalculator.Calculate(
            visits,
            rides);

        Assert.Equal(2, result.ParkCount);
        Assert.Equal(3, result.Summary.VisitCount);
        Assert.Equal(2, result.Summary.RideOutcomes.CompletedRideCount);
        PassportProfileYearStatistics year2026 = Assert.Single(
            result.Years,
            value => value.Year == 2026);
        Assert.Equal(2, year2026.VisitCount);
        Assert.Equal(2, year2026.ParkCount);
        Assert.Equal(1, year2026.CompletedRideCount);
        PassportProfileParkStatistics park1 = Assert.Single(
            result.Parks,
            value => value.ParkId == "park-1");
        Assert.Equal(2, park1.VisitCount);
        Assert.Equal(1, park1.CompletedRideCount);
        Assert.Equal(1, park1.RatedVisitCount);
        Assert.Equal(4d, park1.AverageVisitRating);
    }

    [Fact]
    public void CalculateMissedItems_ShouldMergeStatusesPublishedAsMissedOther()
    {
        PassportProfileMissedItemObservation[] observations =
        {
            new PassportProfileMissedItemObservation(
                "Attraction test",
                RideOccurrenceStatus.Attempted),
            new PassportProfileMissedItemObservation(
                "Attraction test",
                RideOccurrenceStatus.MissedUnavailable),
            new PassportProfileMissedItemObservation(
                "Attraction test",
                RideOccurrenceStatus.SkippedByChoice),
            new PassportProfileMissedItemObservation(
                "Attraction test",
                RideOccurrenceStatus.MissedClosed),
        };

        IReadOnlyCollection<PassportProfileMissedItemStatistics> result =
            PassportProfileStatisticsCalculator.CalculateMissedItems(observations);

        Assert.Collection(
            result,
            item =>
            {
                Assert.Equal(PassportProfileMissedItemStatus.MissedOther, item.Status);
                Assert.Equal(3, item.OccurrenceCount);
            },
            item =>
            {
                Assert.Equal(PassportProfileMissedItemStatus.MissedClosure, item.Status);
                Assert.Equal(1, item.OccurrenceCount);
            });
    }

    private static PassportRideStatisticsObservation CreateRide(
        string rideId,
        string visitId,
        string parkId,
        int year,
        RideOccurrenceStatus status)
    {
        return new PassportRideStatisticsObservation(
            rideId,
            visitId,
            parkId,
            $"item-{rideId}",
            VisitDate.ForDay(year, 7, 1),
            status,
            null,
            null,
            null);
    }
}
