using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveLatestObservationTests
{
    private static readonly DateTime ObservedAtUtc =
        new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CompareRecencyTo_ShouldPrioritizeSourceTimestampThenReceptionTimestamp()
    {
        LiveLatestObservation current = CreateObservation(
            ObservedAtUtc,
            ObservedAtUtc.AddSeconds(10));
        LiveLatestObservation delayedOlderPayload = CreateObservation(
            ObservedAtUtc.AddMinutes(-1),
            ObservedAtUtc.AddMinutes(2));
        LiveLatestObservation sameSourceTimestampReceivedLater = CreateObservation(
            ObservedAtUtc,
            ObservedAtUtc.AddSeconds(20));

        Assert.True(delayedOlderPayload.CompareRecencyTo(current) < 0);
        Assert.True(sameSourceTimestampReceivedLater.CompareRecencyTo(current) > 0);
    }

    [Fact]
    public void Constructor_ShouldKeepZeroWaitAndDetectClosedStatusConflict()
    {
        LiveLatestObservation observation = CreateObservation(
            ObservedAtUtc,
            ObservedAtUtc.AddSeconds(10),
            LiveOperationalStatus.Closed,
            new LiveQueueObservation(LiveQueueKind.Standby, 0, false));

        LiveQueueObservation queue = Assert.Single(observation.Queues);
        Assert.Equal(0, queue.WaitTimeMinutes);
        Assert.True(observation.HasStatusQueueConflict);
    }

    private static LiveLatestObservation CreateObservation(
        DateTime observedAtUtc,
        DateTime receivedAtUtc,
        LiveOperationalStatus status = LiveOperationalStatus.Open,
        LiveQueueObservation? queue = null)
    {
        LiveObservationProvenance provenance = new LiveObservationProvenance(
            LiveDataSourceId.Parse("source"),
            "external-item",
            observedAtUtc,
            receivedAtUtc,
            receivedAtUtc,
            "correlation",
            "adapter-1",
            "mapping-1",
            LiveDataConfidence.Medium,
            "policy-1",
            "transform-1");
        return new LiveLatestObservation(
            new LiveTargetReference(
                LiveTargetType.ParkItem,
                "item-1",
                "park-1",
                "Attraction",
                "Park",
                "FR"),
            status,
            queue is null ? Array.Empty<LiveQueueObservation>() : new[] { queue },
            provenance,
            new LiveFreshnessPolicy(
                "freshness-1",
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(1)),
            new string('a', 64));
    }
}
