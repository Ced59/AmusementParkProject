using AmusementPark.Core.Domain.Watchlists;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Watchlists;

public sealed class WatchPilotHealthEvaluatorTests
{
    [Fact]
    public void Evaluate_ReturnsAwaitingObservations_WhenNoNotificationWasDelivered()
    {
        WatchPilotHealth health = WatchPilotHealthEvaluator.Evaluate(0, 0, 0, 0, 0, 0m, false);

        Assert.Equal(WatchPilotSignal.AwaitingObservations, health.Signal);
        Assert.False(health.CanExtendEventTypes);
    }

    [Fact]
    public void Evaluate_ReturnsNeedsAttention_WhenQualityThresholdIsExceeded()
    {
        WatchPilotHealth health = WatchPilotHealthEvaluator.Evaluate(100, 1, 0, 100, 0, 30m, true);

        Assert.Equal(WatchPilotSignal.NeedsAttention, health.Signal);
        Assert.Equal(1m, health.DuplicateRatePercent);
    }

    [Fact]
    public void Evaluate_RequiresProviderFeedbackBeforeExtendingTheCatalog()
    {
        WatchPilotHealth health = WatchPilotHealthEvaluator.Evaluate(100, 0, 1, 100, 0, 30m, false);

        Assert.Equal(WatchPilotSignal.Monitor, health.Signal);
        Assert.False(health.CanExtendEventTypes);
        Assert.False(health.ProviderFeedbackAvailable);
    }

    [Fact]
    public void Evaluate_OpensTheGate_WhenVolumeQualityAndProviderCoverageAreSufficient()
    {
        WatchPilotHealth health = WatchPilotHealthEvaluator.Evaluate(100, 0, 1, 98, 2, 300m, true);

        Assert.Equal(WatchPilotSignal.ReadyToExtend, health.Signal);
        Assert.True(health.CanExtendEventTypes);
    }
}
