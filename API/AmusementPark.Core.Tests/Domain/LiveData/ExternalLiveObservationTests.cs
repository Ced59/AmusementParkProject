using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class ExternalLiveObservationTests
{
    [Fact]
    public void Constructor_ShouldPreserveNormalizedProviderFacts()
    {
        DateTime updatedAtUtc = new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc);
        LiveQueueObservation queue = new LiveQueueObservation(
            LiveQueueKind.Standby,
            15,
            isEstimated: false);

        ExternalLiveObservation observation = new ExternalLiveObservation(
            "ride-1",
            "Ride One",
            LiveTargetType.ParkItem,
            LiveOperationalStatus.Open,
            updatedAtUtc,
            new[] { queue });

        Assert.Equal("ride-1", observation.ExternalTargetId);
        Assert.Equal(LiveOperationalStatus.Open, observation.Status);
        Assert.Same(queue, Assert.Single(observation.Queues));
    }

    [Fact]
    public void Constructor_WhenQueueKindIsDuplicated_ShouldRejectObservation()
    {
        DateTime updatedAtUtc = new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc);
        LiveQueueObservation first = new LiveQueueObservation(
            LiveQueueKind.Standby,
            0,
            isEstimated: false);
        LiveQueueObservation second = new LiveQueueObservation(
            LiveQueueKind.Standby,
            null,
            isEstimated: false);

        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(() =>
            new ExternalLiveObservation(
                "ride-1",
                "Ride One",
                LiveTargetType.ParkItem,
                LiveOperationalStatus.Open,
                updatedAtUtc,
                new[] { first, second }));

        Assert.Equal(LiveDataErrorCodes.InvalidQueue, exception.Code);
    }

    [Theory]
    [InlineData(LiveOperationalStatus.Closed, 0, true)]
    [InlineData(LiveOperationalStatus.Closed, 25, true)]
    [InlineData(LiveOperationalStatus.Closed, null, false)]
    [InlineData(LiveOperationalStatus.Open, 25, false)]
    public void HasStatusQueueConflict_ShouldDetectClosedTargetsWithReportedWait(
        LiveOperationalStatus status,
        int? waitTimeMinutes,
        bool expected)
    {
        ExternalLiveObservation observation = new ExternalLiveObservation(
            "ride-1",
            "Ride One",
            LiveTargetType.ParkItem,
            status,
            new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc),
            new[]
            {
                new LiveQueueObservation(
                    LiveQueueKind.Standby,
                    waitTimeMinutes,
                    isEstimated: false),
            });

        Assert.Equal(expected, observation.HasStatusQueueConflict);
    }
}
