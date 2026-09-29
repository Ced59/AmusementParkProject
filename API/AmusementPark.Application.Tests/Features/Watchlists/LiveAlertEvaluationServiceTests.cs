using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Watchlists;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class LiveAlertEvaluationServiceTests
{
    private static readonly DateTime NowUtc = new(2026, 9, 29, 12, 10, 0, DateTimeKind.Utc);

    [Fact]
    public async Task EvaluateAsync_ShouldCreateNotificationFromFreshAllowedTransition()
    {
        LiveLatestObservation seed = CreateObservation(NowUtc.AddMinutes(-2), 40);
        LiveAlertSubscription subscription = LiveAlertSubscription.Create(
            LiveAlertSubscriptionId.New(),
            "member-1",
            "item-1",
            "park-1",
            LiveAlertType.WaitBelow,
            30,
            NowUtc.AddMinutes(-2),
            NowUtc.AddHours(2),
            seed);
        LiveLatestObservation current = CreateObservation(NowUtc.AddMinutes(-1), 25);
        Mock<ILiveAlertSubscriptionRepository> subscriptions =
            new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListPendingAsync(
                NowUtc,
                500,
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        subscriptions.Setup(repository => repository.ListActiveMatchingAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })),
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        subscriptions.Setup(repository => repository.ReplaceAsync(
                subscription,
                1,
                CancellationToken.None))
            .ReturnsAsync(WatchSubscriptionWriteOutcome.Success);
        subscriptions.Setup(repository => repository.ReplaceAsync(
                subscription,
                2,
                CancellationToken.None))
            .ReturnsAsync(WatchSubscriptionWriteOutcome.Success);
        LiveAlertNotification? created = null;
        Mock<ILiveAlertNotificationRepository> notifications =
            new(MockBehavior.Strict);
        notifications.Setup(repository => repository.CreateAsync(
                It.IsAny<LiveAlertNotification>(),
                CancellationToken.None))
            .Callback<LiveAlertNotification, CancellationToken>((notification, _) => created = notification)
            .ReturnsAsync(UserNotificationWriteOutcome.Success);
        Mock<ILiveDataSourceCatalog> catalog = CreateCatalog();
        Mock<ILiveOperationalGate> gate = CreateGate();
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        LiveAlertEvaluationService service = new(
            subscriptions.Object,
            notifications.Object,
            catalog.Object,
            gate.Object,
            clock.Object);

        await service.EvaluateAsync(new[] { current }, CancellationToken.None);
        Assert.NotNull(subscription.PendingTrigger);
        notifications.VerifyNoOtherCalls();

        await service.RetryPendingAsync(CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal("member-1", created!.UserId);
        Assert.Equal(25, created.CurrentWaitMinutes);
        Assert.Equal(60, created.AgeSeconds);
        Assert.Equal(UserNotificationStatus.Delivered, created.Status);
        Assert.Null(subscription.PendingTrigger);
        subscriptions.VerifyAll();
        notifications.VerifyAll();
    }

    [Fact]
    public async Task EvaluateAsync_ShouldRetryDurablePendingTriggerAfterNotificationFailure()
    {
        LiveAlertSubscription subscription = LiveAlertSubscription.Create(
            LiveAlertSubscriptionId.New(),
            "member-1",
            "item-1",
            "park-1",
            LiveAlertType.WaitBelow,
            30,
            NowUtc.AddMinutes(-2),
            NowUtc.AddHours(2),
            CreateObservation(NowUtc.AddMinutes(-2), 40));
        LiveLatestObservation current = CreateObservation(NowUtc.AddMinutes(-1), 25);
        Mock<ILiveAlertSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListPendingAsync(
                NowUtc,
                500,
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        subscriptions.Setup(repository => repository.ListActiveMatchingAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        subscriptions.Setup(repository => repository.ReplaceAsync(
                subscription,
                1,
                CancellationToken.None))
            .ReturnsAsync(WatchSubscriptionWriteOutcome.Success);
        subscriptions.Setup(repository => repository.ReplaceAsync(
                subscription,
                2,
                CancellationToken.None))
            .ReturnsAsync(WatchSubscriptionWriteOutcome.Success);
        Mock<ILiveAlertNotificationRepository> notifications = new(MockBehavior.Strict);
        notifications.SetupSequence(repository => repository.CreateAsync(
                It.IsAny<LiveAlertNotification>(),
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("temporary write failure"))
            .ReturnsAsync(UserNotificationWriteOutcome.Success);
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        LiveAlertEvaluationService service = new(
            subscriptions.Object,
            notifications.Object,
            CreateCatalog().Object,
            CreateGate().Object,
            clock.Object);

        await service.EvaluateAsync(new[] { current }, CancellationToken.None);
        Assert.NotNull(subscription.PendingTrigger);

        await service.RetryPendingAsync(CancellationToken.None);
        Assert.NotNull(subscription.PendingTrigger);

        await service.RetryPendingAsync(CancellationToken.None);

        Assert.Null(subscription.PendingTrigger);
        notifications.Verify(repository => repository.CreateAsync(
            It.IsAny<LiveAlertNotification>(), CancellationToken.None), Times.Exactly(2));
        subscriptions.VerifyAll();
    }

    [Fact]
    public async Task EvaluateAsync_ShouldIgnoreObservationWhenPublicReadingIsDisabled()
    {
        Mock<ILiveAlertSubscriptionRepository> subscriptions =
            new(MockBehavior.Strict);
        Mock<ILiveAlertNotificationRepository> notifications =
            new(MockBehavior.Strict);
        Mock<ILiveDataSourceCatalog> catalog = new(MockBehavior.Strict);
        catalog.SetupGet(value => value.IsPublicReadEnabled).Returns(false);
        catalog.SetupGet(value => value.PublicPollingTarget).Returns((LivePollingTarget?)null);
        Mock<ILiveOperationalGate> gate = new(MockBehavior.Strict);
        LiveAlertEvaluationService service = new(
            subscriptions.Object,
            notifications.Object,
            catalog.Object,
            gate.Object);

        await service.EvaluateAsync(
            new[] { CreateObservation(NowUtc.AddMinutes(-1), 25) },
            CancellationToken.None);

        subscriptions.VerifyNoOtherCalls();
        notifications.VerifyNoOtherCalls();
        gate.VerifyNoOtherCalls();
    }

    private static Mock<ILiveDataSourceCatalog> CreateCatalog()
    {
        Mock<ILiveDataSourceCatalog> catalog = new(MockBehavior.Strict);
        catalog.SetupGet(value => value.IsPublicReadEnabled).Returns(true);
        catalog.SetupGet(value => value.PublicPollingTarget).Returns(new LivePollingTarget(
            LiveDataSourceId.Parse("source-1"),
            "external-park-1",
            new LivePollingActiveWindow(TimeZoneInfo.Utc, 0, 24),
            new LivePollingPolicy(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromHours(1),
                3,
                TimeSpan.FromMinutes(15)),
            TimeSpan.Zero));
        return catalog;
    }

    private static Mock<ILiveOperationalGate> CreateGate()
    {
        Mock<ILiveOperationalGate> gate = new(MockBehavior.Strict);
        gate.Setup(value => value.LoadAsync(
                LiveDataSourceId.Parse("source-1"),
                "external-park-1",
                CancellationToken.None))
            .ReturnsAsync(new LiveOperationalGateSnapshot(
                true,
                true,
                "external-park-1",
                Array.Empty<LiveOperationalControl>(),
                new LiveOperationalControlPolicy()));
        return gate;
    }

    private static LiveLatestObservation CreateObservation(DateTime observedAtUtc, int waitMinutes)
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
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, waitMinutes, false) },
            new LiveObservationProvenance(
                LiveDataSourceId.Parse("source-1"),
                "external-item-1",
                observedAtUtc,
                observedAtUtc,
                observedAtUtc,
                $"correlation-{observedAtUtc.Ticks}",
                "adapter-1",
                "mapping-1",
                LiveDataConfidence.High,
                "policy-1",
                "transform-1"),
            new LiveFreshnessPolicy(
                "freshness-1",
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(1)),
            new string('a', 64));
    }
}
