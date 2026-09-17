using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class FactualNotificationCorrectionJobHandlerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 17, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_WithCorrection_ShouldNotifyEveryOriginalRecipientUsingTheSuccessor()
    {
        FactualChangeEvent originalEvent = CreatePublishedEvent("event-1", 1);
        FactualChangeEvent correction = CreatePublishedEvent("event-2", 2);
        UserNotification originalNotification = CreateNotification(originalEvent, "notification-1");
        originalEvent.Correct(correction.Id, NowUtc.AddMinutes(-1));
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        Mock<IUserNotificationRepository> notifications =
            new Mock<IUserNotificationRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionReceiptRepository> receipts =
            new Mock<IFactualNotificationDistributionReceiptRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionScheduler> scheduler =
            new Mock<IFactualNotificationDistributionScheduler>(MockBehavior.Strict);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        string receiptId = FactualNotificationCorrectionJob.ReceiptId("event-1", 4);
        receipts.Setup(repository => repository.IsCompletedAsync(receiptId, CancellationToken.None))
            .ReturnsAsync(false);
        receipts.Setup(repository => repository.IsCompletedAsync("event-1", CancellationToken.None))
            .ReturnsAsync(true);
        events.Setup(repository => repository.GetAsync(originalEvent.Id, CancellationToken.None))
            .ReturnsAsync(originalEvent);
        events.Setup(repository => repository.GetAsync(correction.Id, CancellationToken.None))
            .ReturnsAsync(correction);
        notifications.Setup(repository => repository.ListByFactualEventAsync(
                originalEvent.Id,
                null,
                FactualNotificationCorrectionJob.NotificationBatchSize,
                CancellationToken.None))
            .ReturnsAsync(new[] { originalNotification });
        notifications.Setup(repository => repository.CreateManyAsync(
                It.Is<IReadOnlyCollection<UserNotification>>(items =>
                    items.Count == 1
                    && items.Single().UserId == originalNotification.UserId
                    && items.Single().FactualEventId == correction.Id
                    && items.Single().SubscriptionId == originalNotification.SubscriptionId),
                CancellationToken.None))
            .ReturnsAsync(1);
        receipts.Setup(repository => repository.CompleteAsync(
                It.Is<FactualNotificationDistributionReceipt>(receipt =>
                    receipt.EventId == receiptId && receipt.CompletedAtUtc == NowUtc),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        FactualNotificationCorrectionJobHandler handler = new FactualNotificationCorrectionJobHandler(
            events.Object,
            notifications.Object,
            receipts.Object,
            scheduler.Object,
            timeProvider.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(new FactualNotificationCorrectionJobPayload("event-1", 4, null)),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        events.VerifyAll();
        notifications.VerifyAll();
        receipts.VerifyAll();
        scheduler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WithRetraction_ShouldRedeliverDismissedOriginalWithoutDuplicates()
    {
        FactualChangeEvent originalEvent = CreatePublishedEvent("event-1", 1);
        UserNotification originalNotification = CreateNotification(originalEvent, "notification-1");
        originalEvent.Retract("source-invalidated", NowUtc.AddMinutes(-1));
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        Mock<IUserNotificationRepository> notifications =
            new Mock<IUserNotificationRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionReceiptRepository> receipts =
            new Mock<IFactualNotificationDistributionReceiptRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionScheduler> scheduler =
            new Mock<IFactualNotificationDistributionScheduler>(MockBehavior.Strict);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        string receiptId = FactualNotificationCorrectionJob.ReceiptId("event-1", 4);
        receipts.Setup(repository => repository.IsCompletedAsync(receiptId, CancellationToken.None))
            .ReturnsAsync(false);
        receipts.Setup(repository => repository.IsCompletedAsync("event-1", CancellationToken.None))
            .ReturnsAsync(true);
        events.Setup(repository => repository.GetAsync(originalEvent.Id, CancellationToken.None))
            .ReturnsAsync(originalEvent);
        notifications.Setup(repository => repository.ListByFactualEventAsync(
                originalEvent.Id,
                null,
                FactualNotificationCorrectionJob.NotificationBatchSize,
                CancellationToken.None))
            .ReturnsAsync(new[] { originalNotification });
        notifications.Setup(repository => repository.RedeliverRetractionAsync(
                originalEvent.Id,
                It.Is<IReadOnlyCollection<UserNotificationId>>(ids => ids.Single() == originalNotification.Id),
                NowUtc.AddMinutes(-1),
                NowUtc,
                CancellationToken.None))
            .ReturnsAsync(1);
        receipts.Setup(repository => repository.CompleteAsync(
                It.Is<FactualNotificationDistributionReceipt>(receipt => receipt.EventId == receiptId),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        FactualNotificationCorrectionJobHandler handler = new FactualNotificationCorrectionJobHandler(
            events.Object,
            notifications.Object,
            receipts.Object,
            scheduler.Object,
            timeProvider.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(new FactualNotificationCorrectionJobPayload("event-1", 4, null)),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        events.VerifyAll();
        notifications.VerifyAll();
        receipts.VerifyAll();
        scheduler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhileInitialDistributionIsPending_ShouldRetryWithoutCreatingFollowUps()
    {
        FactualChangeEvent originalEvent = CreatePublishedEvent("event-1", 1);
        FactualChangeEvent correction = CreatePublishedEvent("event-2", 2);
        originalEvent.Correct(correction.Id, NowUtc.AddMinutes(-1));
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        Mock<IUserNotificationRepository> notifications =
            new Mock<IUserNotificationRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionReceiptRepository> receipts =
            new Mock<IFactualNotificationDistributionReceiptRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionScheduler> scheduler =
            new Mock<IFactualNotificationDistributionScheduler>(MockBehavior.Strict);
        string receiptId = FactualNotificationCorrectionJob.ReceiptId("event-1", 4);
        receipts.Setup(repository => repository.IsCompletedAsync(receiptId, CancellationToken.None))
            .ReturnsAsync(false);
        receipts.Setup(repository => repository.IsCompletedAsync("event-1", CancellationToken.None))
            .ReturnsAsync(false);
        events.Setup(repository => repository.GetAsync(originalEvent.Id, CancellationToken.None))
            .ReturnsAsync(originalEvent);
        FactualNotificationCorrectionJobHandler handler = new FactualNotificationCorrectionJobHandler(
            events.Object,
            notifications.Object,
            receipts.Object,
            scheduler.Object,
            Mock.Of<TimeProvider>());

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(new FactualNotificationCorrectionJobPayload("event-1", 4, null)),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Retry, result.Outcome);
        Assert.Equal(FactualNotificationDistributionErrorCodes.InitialDistributionPending, result.ErrorCode);
        events.VerifyAll();
        receipts.VerifyAll();
        notifications.VerifyNoOtherCalls();
        scheduler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WithCorrectionChain_ShouldNotifyOriginalRecipientUsingLatestPublishedFact()
    {
        FactualChangeEvent originalEvent = CreatePublishedEvent("event-1", 1);
        FactualChangeEvent intermediate = CreatePublishedEvent("event-2", 2);
        FactualChangeEvent latest = CreatePublishedEvent("event-3", 3);
        UserNotification originalNotification = CreateNotification(originalEvent, "notification-1");
        originalEvent.Correct(intermediate.Id, NowUtc.AddMinutes(-4));
        intermediate.Correct(latest.Id, NowUtc.AddMinutes(-2));
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        Mock<IUserNotificationRepository> notifications =
            new Mock<IUserNotificationRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionReceiptRepository> receipts =
            new Mock<IFactualNotificationDistributionReceiptRepository>(MockBehavior.Strict);
        Mock<IFactualNotificationDistributionScheduler> scheduler =
            new Mock<IFactualNotificationDistributionScheduler>(MockBehavior.Strict);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        string receiptId = FactualNotificationCorrectionJob.ReceiptId("event-1", 4);
        receipts.Setup(repository => repository.IsCompletedAsync(receiptId, CancellationToken.None))
            .ReturnsAsync(false);
        receipts.Setup(repository => repository.IsCompletedAsync("event-1", CancellationToken.None))
            .ReturnsAsync(true);
        events.Setup(repository => repository.GetAsync(originalEvent.Id, CancellationToken.None))
            .ReturnsAsync(originalEvent);
        events.Setup(repository => repository.GetAsync(intermediate.Id, CancellationToken.None))
            .ReturnsAsync(intermediate);
        events.Setup(repository => repository.GetAsync(latest.Id, CancellationToken.None))
            .ReturnsAsync(latest);
        notifications.Setup(repository => repository.ListByFactualEventAsync(
                originalEvent.Id,
                null,
                FactualNotificationCorrectionJob.NotificationBatchSize,
                CancellationToken.None))
            .ReturnsAsync(new[] { originalNotification });
        notifications.Setup(repository => repository.CreateManyAsync(
                It.Is<IReadOnlyCollection<UserNotification>>(items =>
                    items.Count == 1 && items.Single().FactualEventId == latest.Id),
                CancellationToken.None))
            .ReturnsAsync(1);
        receipts.Setup(repository => repository.CompleteAsync(
                It.Is<FactualNotificationDistributionReceipt>(receipt => receipt.EventId == receiptId),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        FactualNotificationCorrectionJobHandler handler = new FactualNotificationCorrectionJobHandler(
            events.Object,
            notifications.Object,
            receipts.Object,
            scheduler.Object,
            timeProvider.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(new FactualNotificationCorrectionJobPayload("event-1", 4, null)),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        events.VerifyAll();
        notifications.VerifyAll();
        receipts.VerifyAll();
        scheduler.VerifyNoOtherCalls();
    }

    private static DurableBackgroundJobExecutionContext CreateContext(
        FactualNotificationCorrectionJobPayload payload)
    {
        return new DurableBackgroundJobExecutionContext(
            "job-correction-1",
            FactualNotificationCorrectionJob.PayloadVersion,
            JsonSerializer.SerializeToElement(payload),
            payload.EventVersion,
            1,
            payload.EventId);
    }

    private static UserNotification CreateNotification(
        FactualChangeEvent factualEvent,
        string notificationId)
    {
        WatchSubscription subscription = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkNameChanged },
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>(),
            NowUtc.AddHours(-3));
        return UserNotification.CreateWeb(
            UserNotificationId.Parse(notificationId),
            factualEvent,
            subscription,
            "FR",
            NowUtc.AddMinutes(-2));
    }

    private static FactualChangeEvent CreatePublishedEvent(string eventId, long revision)
    {
        FactualChangeEvent factualEvent = FactualChangeEvent.CreateDraft(
            FactualChangeEventId.Parse(eventId),
            FactualEventType.ParkNameChanged,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromText($"Name {revision}"),
            FactValue.FromText($"Name {revision + 1}"),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Example Park",
                "Official update",
                $"https://example.com/update-{revision}",
                NowUtc.AddHours(-3)),
            DataConfidence.High,
            NowUtc.AddHours(-2),
            "park:park-1:name",
            revision,
            NowUtc.AddHours(-2));
        factualEvent.Verify(NowUtc.AddHours(-1));
        factualEvent.Publish(NowUtc.AddMinutes(-30));
        return factualEvent;
    }
}
