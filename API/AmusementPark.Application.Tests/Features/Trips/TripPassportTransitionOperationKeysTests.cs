using AmusementPark.Application.Features.Trips.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripPassportTransitionOperationKeysTests
{
    [Fact]
    public void OperationKeys_ShouldDistinguishTwoParksScheduledOnTheSameDay()
    {
        DateOnly localDate = new(2027, 8, 20);

        string firstVisit = TripPassportTransitionOperationKeys.Visit(
            "trip-1",
            "user-1",
            "park-1",
            localDate);
        string secondVisit = TripPassportTransitionOperationKeys.Visit(
            "trip-1",
            "user-1",
            "park-2",
            localDate);
        string firstRides = TripPassportTransitionOperationKeys.Rides(
            "trip-1",
            "user-1",
            "park-1",
            localDate);
        string secondRides = TripPassportTransitionOperationKeys.Rides(
            "trip-1",
            "user-1",
            "park-2",
            localDate);

        Assert.NotEqual(firstVisit, secondVisit);
        Assert.NotEqual(firstRides, secondRides);
    }
}
