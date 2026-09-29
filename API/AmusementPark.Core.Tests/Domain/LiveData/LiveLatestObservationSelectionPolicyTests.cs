using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveLatestObservationSelectionPolicyTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Select_ShouldPreferConfiguredSourcePriorityOverRecency()
    {
        LiveLatestObservationSelectionPolicy policy = new LiveLatestObservationSelectionPolicy();
        LiveLatestObservation preferred = CreateObservation("official", NowUtc.AddMinutes(-5));
        LiveLatestObservation recent = CreateObservation("aggregator", NowUtc.AddMinutes(-1));

        LiveLatestObservation? selected = policy.Select(
            new[] { recent, preferred },
            new Dictionary<LiveDataSourceId, int>
            {
                [preferred.Provenance.SourceId] = 10,
                [recent.Provenance.SourceId] = 100,
            },
            NowUtc);

        Assert.Same(preferred, selected);
    }

    [Fact]
    public void Select_ShouldUseCurrentFallbackWhenPreferredSourceExpired()
    {
        LiveLatestObservationSelectionPolicy policy = new LiveLatestObservationSelectionPolicy();
        LiveLatestObservation expired = CreateObservation("official", NowUtc.AddHours(-1));
        LiveLatestObservation current = CreateObservation("aggregator", NowUtc.AddMinutes(-1));

        LiveLatestObservation? selected = policy.Select(
            new[] { expired, current },
            new Dictionary<LiveDataSourceId, int>
            {
                [expired.Provenance.SourceId] = 10,
                [current.Provenance.SourceId] = 100,
            },
            NowUtc);

        Assert.Same(current, selected);
    }

    private static LiveLatestObservation CreateObservation(string sourceId, DateTime observedAtUtc)
    {
        return new LiveLatestObservation(
            new LiveTargetReference(
                LiveTargetType.ParkItem,
                "item-1",
                "park-1",
                "Attraction",
                "Park",
                "FR"),
            LiveOperationalStatus.Open,
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, 5, false) },
            new LiveObservationProvenance(
                LiveDataSourceId.Parse(sourceId),
                $"external-{sourceId}",
                observedAtUtc,
                observedAtUtc.AddSeconds(1),
                observedAtUtc.AddSeconds(2),
                $"correlation-{sourceId}",
                "adapter-1",
                "mapping-1",
                LiveDataConfidence.High,
                "usage-1",
                "transformation-1"),
            new LiveFreshnessPolicy(
                "freshness-1",
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(1)),
            null);
    }
}
