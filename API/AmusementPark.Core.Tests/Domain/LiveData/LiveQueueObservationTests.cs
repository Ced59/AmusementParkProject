using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveQueueObservationTests
{
    [Fact]
    public void Constructor_ShouldKeepExplicitZeroDistinctFromUnknownWait()
    {
        LiveQueueObservation zeroWait = new LiveQueueObservation(
            LiveQueueKind.Standby,
            0,
            isEstimated: false);
        LiveQueueObservation unknownWait = new LiveQueueObservation(
            LiveQueueKind.SingleRider,
            null,
            isEstimated: false);

        Assert.Equal(0, zeroWait.WaitTimeMinutes);
        Assert.Null(unknownWait.WaitTimeMinutes);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(LiveQueueObservation.MaximumWaitTimeMinutes + 1)]
    public void Constructor_WhenWaitIsOutsideTechnicalBounds_ShouldRejectValue(int waitTimeMinutes)
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(() =>
            new LiveQueueObservation(LiveQueueKind.Standby, waitTimeMinutes, isEstimated: false));

        Assert.Equal(LiveDataErrorCodes.InvalidWaitTime, exception.Code);
    }

    [Fact]
    public void Constructor_WhenReturnWindowIsReversed_ShouldRejectQueue()
    {
        DateTime startUtc = new DateTime(2026, 9, 28, 11, 0, 0, DateTimeKind.Utc);

        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(() =>
            new LiveQueueObservation(
                LiveQueueKind.ReturnTime,
                null,
                isEstimated: false,
                returnStartUtc: startUtc,
                returnEndUtc: startUtc.AddMinutes(-30)));

        Assert.Equal(LiveDataErrorCodes.InvalidQueue, exception.Code);
    }
}
