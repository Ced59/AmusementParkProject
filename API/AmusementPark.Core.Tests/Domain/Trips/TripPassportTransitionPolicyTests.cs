using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripPassportTransitionPolicyTests
{
    [Theory]
    [InlineData(-1, true, true)]
    [InlineData(0, true, false)]
    [InlineData(1, true, false)]
    [InlineData(-1, false, false)]
    public void CanConfirmDay_ShouldRequireAPastDayWithAnAvailablePark(
        int dayOffset,
        bool isParkAvailable,
        bool expected)
    {
        DateOnly destinationToday = new(2027, 8, 22);

        bool result = TripPassportTransitionPolicy.CanConfirmDay(
            destinationToday.AddDays(dayOffset),
            destinationToday,
            isParkAvailable);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void HasElapsed_ShouldDependOnlyOnTheDestinationCalendar(
        int dayOffset,
        bool expected)
    {
        DateOnly destinationToday = new(2027, 8, 22);

        bool result = TripPassportTransitionPolicy.HasElapsed(
            destinationToday.AddDays(dayOffset),
            destinationToday);

        Assert.Equal(expected, result);
    }
}
