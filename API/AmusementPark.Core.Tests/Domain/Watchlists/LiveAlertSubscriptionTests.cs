using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Watchlists;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Watchlists;

public sealed class LiveAlertSubscriptionTests
{
    private static readonly DateTime StartUtc = new(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Evaluate_ShouldTriggerBelowThresholdOnlyAfterHysteresisAndCooldown()
    {
        LiveAlertSubscription subscription = CreateSubscription(
            LiveAlertType.WaitBelow,
            30,
            CreateObservation(StartUtc, LiveOperationalStatus.Open, 40));

        LiveAlertTrigger? first = subscription.Evaluate(
            CreateObservation(StartUtc.AddMinutes(1), LiveOperationalStatus.Open, 29),
            StartUtc.AddMinutes(1));
        subscription.MarkPendingTriggerDelivered();
        LiveAlertTrigger? oscillation = subscription.Evaluate(
            CreateObservation(StartUtc.AddMinutes(2), LiveOperationalStatus.Open, 31),
            StartUtc.AddMinutes(2));
        _ = subscription.Evaluate(
            CreateObservation(StartUtc.AddMinutes(3), LiveOperationalStatus.Open, 36),
            StartUtc.AddMinutes(3));
        LiveAlertTrigger? cooldown = subscription.Evaluate(
            CreateObservation(StartUtc.AddMinutes(4), LiveOperationalStatus.Open, 29),
            StartUtc.AddMinutes(4));
        LiveAlertTrigger? afterCooldown = subscription.Evaluate(
            CreateObservation(StartUtc.AddMinutes(32), LiveOperationalStatus.Open, 28),
            StartUtc.AddMinutes(32));

        Assert.NotNull(first);
        Assert.Null(oscillation);
        Assert.Null(cooldown);
        Assert.NotNull(afterCooldown);
        Assert.Equal(28, afterCooldown!.CurrentWaitMinutes);
    }

    [Fact]
    public void Evaluate_ShouldIgnoreAgingObservationWithoutAdvancingState()
    {
        LiveAlertSubscription subscription = CreateSubscription(
            LiveAlertType.WaitBelow,
            30,
            CreateObservation(StartUtc, LiveOperationalStatus.Open, 40));
        long version = subscription.Version;

        LiveAlertTrigger? trigger = subscription.Evaluate(
            CreateObservation(StartUtc.AddMinutes(1), LiveOperationalStatus.Open, 20),
            StartUtc.AddMinutes(7));

        Assert.Null(trigger);
        Assert.Equal(version, subscription.Version);
        Assert.Equal(StartUtc, subscription.LastObservedAtUtc);
    }

    [Fact]
    public void Evaluate_ShouldTriggerReopeningAfterClosedState()
    {
        LiveAlertSubscription subscription = CreateSubscription(
            LiveAlertType.Reopened,
            null,
            CreateObservation(StartUtc, LiveOperationalStatus.TemporarilyClosed, null));

        LiveAlertTrigger? trigger = subscription.Evaluate(
            CreateObservation(StartUtc.AddMinutes(1), LiveOperationalStatus.Open, 10),
            StartUtc.AddMinutes(1));

        Assert.NotNull(trigger);
        Assert.Equal(LiveOperationalStatus.TemporarilyClosed, trigger!.PreviousStatus);
        Assert.Equal(LiveOperationalStatus.Open, trigger.CurrentStatus);
    }

    [Fact]
    public void Evaluate_ShouldNotTriggerAfterSubscriptionExpiration()
    {
        LiveAlertSubscription subscription = CreateSubscription(
            LiveAlertType.Degraded,
            null,
            CreateObservation(StartUtc, LiveOperationalStatus.Open, 20),
            StartUtc.AddMinutes(30));

        LiveAlertTrigger? trigger = subscription.Evaluate(
            CreateObservation(StartUtc.AddMinutes(31), LiveOperationalStatus.Down, null),
            StartUtc.AddMinutes(31));

        Assert.Null(trigger);
        Assert.Equal(1, subscription.Version);
    }

    [Fact]
    public void Create_ShouldRequireThresholdOnlyForWaitAlerts()
    {
        LiveLatestObservation seed = CreateObservation(StartUtc, LiveOperationalStatus.Open, 20);

        Assert.Throws<ArgumentException>(() => LiveAlertSubscription.Create(
            LiveAlertSubscriptionId.New(),
            "user-1",
            "item-1",
            "park-1",
            LiveAlertType.WaitAbove,
            null,
            StartUtc,
            StartUtc.AddHours(1),
            seed));
        Assert.Throws<ArgumentException>(() => LiveAlertSubscription.Create(
            LiveAlertSubscriptionId.New(),
            "user-1",
            "item-1",
            "park-1",
            LiveAlertType.Reopened,
            30,
            StartUtc,
            StartUtc.AddHours(1),
            seed));
    }

    [Fact]
    public void Evaluate_ShouldApplyCooldownToReopenedAlertsAfterRearming()
    {
        LiveAlertSubscription subscription = CreateSubscription(
            LiveAlertType.Reopened,
            null,
            CreateObservation(StartUtc, LiveOperationalStatus.Closed, null));

        LiveAlertTrigger? first = subscription.Evaluate(
            CreateObservation(StartUtc.AddMinutes(1), LiveOperationalStatus.Open, null),
            StartUtc.AddMinutes(1));
        subscription.MarkPendingTriggerDelivered();
        _ = subscription.Evaluate(
            CreateObservation(StartUtc.AddMinutes(2), LiveOperationalStatus.Closed, null),
            StartUtc.AddMinutes(2));
        LiveAlertTrigger? duringCooldown = subscription.Evaluate(
            CreateObservation(StartUtc.AddMinutes(3), LiveOperationalStatus.Open, null),
            StartUtc.AddMinutes(3));

        Assert.NotNull(first);
        Assert.Null(duringCooldown);
    }

    [Theory]
    [InlineData(LiveOperationalStatus.Unknown)]
    [InlineData(LiveOperationalStatus.Delayed)]
    [InlineData(LiveOperationalStatus.OperatingWithLimitations)]
    [InlineData(LiveOperationalStatus.Removed)]
    public void Evaluate_ShouldNotTreatNonClosureStatusAsReopening(
        LiveOperationalStatus initialStatus)
    {
        LiveAlertSubscription subscription = CreateSubscription(
            LiveAlertType.Reopened,
            null,
            CreateObservation(StartUtc, initialStatus, null));

        LiveAlertTrigger? trigger = subscription.Evaluate(
            CreateObservation(StartUtc.AddMinutes(1), LiveOperationalStatus.Open, 10),
            StartUtc.AddMinutes(1));

        Assert.Null(trigger);
    }

    [Theory]
    [InlineData(LiveOperationalStatus.Closed)]
    [InlineData(LiveOperationalStatus.Down)]
    [InlineData(LiveOperationalStatus.WeatherClosed)]
    [InlineData(LiveOperationalStatus.Maintenance)]
    [InlineData(LiveOperationalStatus.NotOperatingToday)]
    [InlineData(LiveOperationalStatus.Removed)]
    public void Evaluate_ShouldSuppressWaitAlertWhenStatusIsNotOperational(
        LiveOperationalStatus status)
    {
        LiveAlertSubscription subscription = CreateSubscription(
            LiveAlertType.WaitBelow,
            30,
            CreateObservation(StartUtc, LiveOperationalStatus.Open, 40));

        LiveAlertTrigger? trigger = subscription.Evaluate(
            CreateObservation(StartUtc.AddMinutes(1), status, 10),
            StartUtc.AddMinutes(1));

        Assert.Null(trigger);
        Assert.Null(subscription.LastWaitMinutes);
    }

    private static LiveAlertSubscription CreateSubscription(
        LiveAlertType type,
        int? thresholdMinutes,
        LiveLatestObservation seed,
        DateTime? expiresAtUtc = null)
    {
        return LiveAlertSubscription.Create(
            LiveAlertSubscriptionId.New(),
            "user-1",
            "item-1",
            "park-1",
            type,
            thresholdMinutes,
            StartUtc,
            expiresAtUtc ?? StartUtc.AddHours(12),
            seed);
    }

    private static LiveLatestObservation CreateObservation(
        DateTime observedAtUtc,
        LiveOperationalStatus status,
        int? waitMinutes)
    {
        LiveObservationProvenance provenance = new(
            LiveDataSourceId.Parse("source-1"),
            "external-item",
            observedAtUtc,
            observedAtUtc,
            observedAtUtc,
            $"correlation-{observedAtUtc.Ticks}",
            "adapter-1",
            "mapping-1",
            LiveDataConfidence.High,
            "policy-1",
            "transform-1");
        IReadOnlyCollection<LiveQueueObservation> queues = waitMinutes.HasValue
            ? new[] { new LiveQueueObservation(LiveQueueKind.Standby, waitMinutes, false) }
            : Array.Empty<LiveQueueObservation>();
        return new LiveLatestObservation(
            new LiveTargetReference(
                LiveTargetType.ParkItem,
                "item-1",
                "park-1",
                "Attraction",
                "Park",
                "FR"),
            status,
            queues,
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
