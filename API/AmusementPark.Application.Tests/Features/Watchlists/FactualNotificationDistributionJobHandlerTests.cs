using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Domain.Watchlists;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class FactualNotificationDistributionJobHandlerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 15, 13, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_WithPublishedMatchingFact_ShouldCreatePrivateNotificationAndReceipt()
    {
        FactualChangeEvent factualEvent = CreatePublishedEvent();
        WatchSubscription subscription = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkNameChanged },
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>(),
            NowUtc.AddDays(-1));
        User user = new User { Id = "user-1", PreferredLanguage = "FR" };
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        Mock<IWatchSubscriptionRepository> subscriptions =
            new Mock<IWatchSubscriptionRepository>(MockBehavior.Strict);
        Mock<IUserNotificationRepository> notifications =
            new Mock<IUserNotificationRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionReceiptRepository> receipts =
            new Mock<IFactualNotificationDistributionReceiptRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionScheduler> scheduler =
            new Mock<IFactualNotificationDistributionScheduler>(MockBehavior.Strict);
        Mock<INotificationDigestScheduler> digestScheduler =
            new Mock<INotificationDigestScheduler>(MockBehavior.Strict);
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        receipts.Setup(repository => repository.IsCompletedAsync("event-1", CancellationToken.None))
            .ReturnsAsync(false);
        events.Setup(repository => repository.GetAsync(
                FactualChangeEventId.Parse("event-1"),
                CancellationToken.None))
            .ReturnsAsync(factualEvent);
        subscriptions.Setup(repository => repository.ListMatchingAsync(
                factualEvent,
                null,
                FactualNotificationDistributionJob.SubscriptionBatchSize,
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        users.Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "user-1" })),
                CancellationToken.None))
            .ReturnsAsync(new[] { user });
        timeProvider.Setup(provider => provider.GetUtcNow())
            .Returns(new DateTimeOffset(NowUtc));
        notifications.Setup(repository => repository.CreateManyAsync(
                It.Is<IReadOnlyCollection<UserNotification>>(created =>
                    created.Count == 1
                    && created.Single().UserId == "user-1"
                    && created.Single().FactualEventId == factualEvent.Id
                    && created.Single().Language == "FR"
                    && created.Single().Status == UserNotificationStatus.Delivered),
                CancellationToken.None))
            .ReturnsAsync(1);
        digestScheduler.Setup(value => value.ScheduleAsync(
                factualEvent.Id,
                It.Is<IReadOnlyCollection<string>>(userIds =>
                    userIds.Count == 1
                    && userIds.Single() == "user-1"),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        receipts.Setup(repository => repository.CompleteAsync(
                It.Is<FactualNotificationDistributionReceipt>(receipt =>
                    receipt.EventId == "event-1"
                    && receipt.CompletedAtUtc == NowUtc),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        FactualNotificationDistributionJobHandler handler = new FactualNotificationDistributionJobHandler(
            events.Object,
            subscriptions.Object,
            notifications.Object,
            receipts.Object,
            scheduler.Object,
            digestScheduler.Object,
            users.Object,
            timeProvider.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(new FactualNotificationDistributionJobPayload("event-1", null)),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        events.VerifyAll();
        subscriptions.VerifyAll();
        notifications.VerifyAll();
        receipts.VerifyAll();
        users.VerifyAll();
        digestScheduler.VerifyAll();
        scheduler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WithCompletedReceipt_ShouldRemainIdempotent()
    {
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        Mock<IWatchSubscriptionRepository> subscriptions =
            new Mock<IWatchSubscriptionRepository>(MockBehavior.Strict);
        Mock<IUserNotificationRepository> notifications =
            new Mock<IUserNotificationRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionReceiptRepository> receipts =
            new Mock<IFactualNotificationDistributionReceiptRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionScheduler> scheduler =
            new Mock<IFactualNotificationDistributionScheduler>(MockBehavior.Strict);
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        receipts.Setup(repository => repository.IsCompletedAsync("event-1", CancellationToken.None))
            .ReturnsAsync(true);
        FactualNotificationDistributionJobHandler handler = new FactualNotificationDistributionJobHandler(
            events.Object,
            subscriptions.Object,
            notifications.Object,
            receipts.Object,
            scheduler.Object,
            Mock.Of<INotificationDigestScheduler>(),
            users.Object,
            timeProvider.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(new FactualNotificationDistributionJobPayload("event-1", null)),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        receipts.VerifyAll();
        events.VerifyNoOtherCalls();
        subscriptions.VerifyNoOtherCalls();
        notifications.VerifyNoOtherCalls();
        scheduler.VerifyNoOtherCalls();
        users.VerifyNoOtherCalls();
        timeProvider.VerifyNoOtherCalls();
    }

    private static DurableBackgroundJobExecutionContext CreateContext(
        FactualNotificationDistributionJobPayload payload)
    {
        return new DurableBackgroundJobExecutionContext(
            "job-1",
            FactualNotificationDistributionJob.PayloadVersion,
            JsonSerializer.SerializeToElement(payload),
            0,
            1,
            "event-1");
    }

    private static FactualChangeEvent CreatePublishedEvent()
    {
        FactualChangeEvent factualEvent = FactualChangeEvent.CreateDraft(
            FactualChangeEventId.Parse("event-1"),
            FactualEventType.ParkNameChanged,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromText("Ancien parc"),
            FactValue.FromText("Nouveau parc"),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Parc exemple",
                "Annonce officielle",
                "https://example.com/announcement",
                NowUtc.AddHours(-4)),
            DataConfidence.High,
            NowUtc.AddHours(-3),
            "park:park-1:name",
            1,
            NowUtc.AddHours(-3));
        factualEvent.Verify(NowUtc.AddHours(-2));
        factualEvent.Publish(NowUtc.AddHours(-1));
        return factualEvent;
    }
}
