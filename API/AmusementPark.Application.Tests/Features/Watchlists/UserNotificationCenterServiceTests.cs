using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Application.Tests.Features.Watchlists.Handlers;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Watchlists;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class UserNotificationCenterServiceTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(false, false, UserNotificationNoticeKind.Update)]
    [InlineData(true, false, UserNotificationNoticeKind.Correction)]
    [InlineData(true, true, UserNotificationNoticeKind.Correction)]
    public async Task SearchAsync_WithPublishedSuccessor_ShouldOnlyLabelCorrectionForOriginalRecipients(
        bool originalWasDelivered,
        bool hasIntermediateCorrection,
        UserNotificationNoticeKind expectedKind)
    {
        FactualChangeEvent original = CreatePublishedEvent("event-1", 1);
        FactualChangeEvent intermediate = CreatePublishedEvent("event-2", 2);
        FactualChangeEvent successor = hasIntermediateCorrection
            ? CreatePublishedEvent("event-3", 3)
            : intermediate;
        original.Correct(intermediate.Id, NowUtc.AddMinutes(-10));
        if (hasIntermediateCorrection)
        {
            intermediate.Correct(successor.Id, NowUtc.AddMinutes(-8));
        }
        WatchSubscription subscription = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkNameChanged },
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>(),
            NowUtc.AddHours(-3));
        UserNotification notification = UserNotification.CreateWeb(
            UserNotificationId.Parse("notification-1"),
            successor,
            subscription,
            "FR",
            NowUtc.AddMinutes(-5));
        UserNotificationSearchCriteria criteria = new UserNotificationSearchCriteria(
            new PagedQuery(1, 20),
            false,
            null,
            null);
        Mock<IUserNotificationRepository> notifications =
            new Mock<IUserNotificationRepository>(MockBehavior.Strict);
        Mock<IWatchSubscriptionRepository> subscriptions =
            new Mock<IWatchSubscriptionRepository>(MockBehavior.Strict);
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        notifications.Setup(repository => repository.SearchOwnedAsync(
                "user-1",
                criteria,
                CancellationToken.None))
            .ReturnsAsync(new PagedResult<UserNotification>(new[] { notification }, 1, 20, 1));
        events.Setup(repository => repository.GetManyAsync(
                It.Is<IReadOnlyCollection<FactualChangeEventId>>(ids => ids.Single() == successor.Id),
                CancellationToken.None))
            .ReturnsAsync(new[] { successor });
        events.Setup(repository => repository.GetCorrectedBySuccessorIdsAsync(
                It.IsAny<IReadOnlyCollection<FactualChangeEventId>>(),
                CancellationToken.None))
            .Returns((IReadOnlyCollection<FactualChangeEventId> ids, CancellationToken _) =>
            {
                IReadOnlyCollection<FactualChangeEvent> origins = ids.Contains(successor.Id)
                    ? hasIntermediateCorrection ? new[] { intermediate } : new[] { original }
                    : hasIntermediateCorrection && ids.Contains(intermediate.Id)
                        ? new[] { original }
                        : Array.Empty<FactualChangeEvent>();
                return Task.FromResult(origins);
            });
        notifications.Setup(repository => repository.ListDeliveredFactualEventIdsOwnedAsync(
                "user-1",
                It.Is<IReadOnlyCollection<FactualChangeEventId>>(ids =>
                    ids.Contains(original.Id)
                    && ids.Count == (hasIntermediateCorrection ? 2 : 1)
                    && (!hasIntermediateCorrection || ids.Contains(intermediate.Id))),
                CancellationToken.None))
            .ReturnsAsync(originalWasDelivered
                ? new[] { original.Id }
                : Array.Empty<FactualChangeEventId>());
        subscriptions.Setup(repository => repository.ListOwnedByIdsAsync(
                "user-1",
                It.Is<IReadOnlyCollection<WatchSubscriptionId>>(ids => ids.Single() == subscription.Id),
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        parks.Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.Single() == "park-1"),
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<Park>());
        images.Setup(repository => repository.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.Park,
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0),
                ImageCategory.Park,
                true,
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string>());
        notifications.Setup(repository => repository.CountUnreadAsync("user-1", CancellationToken.None))
            .ReturnsAsync(1);
        notifications.Setup(repository => repository.ListParkIdsOwnedAsync("user-1", CancellationToken.None))
            .ReturnsAsync(Array.Empty<string>());
        UserCollectionTargetReader targetReader = new UserCollectionTargetReader(
            parks.Object,
            parkItems.Object,
            images.Object);
        UserNotificationCenterService service = new UserNotificationCenterService(
            notifications.Object,
            subscriptions.Object,
            events.Object,
            targetReader);

        ApplicationResult<UserNotificationPageResult> result = await service.SearchAsync(
            "user-1",
            criteria,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedKind, Assert.Single(result.Value!.Items).NoticeKind);
        notifications.VerifyAll();
        subscriptions.VerifyAll();
        events.VerifyAll();
        parks.VerifyAll();
        images.VerifyAll();
        parkItems.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteSourceSubscriptionAsync_WithStaleDisplayedVersion_ShouldKeepNewPreferences()
    {
        UserNotification notification = UserNotification.Restore(
            UserNotificationId.Parse("notification-1"),
            "user-1",
            FactualChangeEventId.Parse("event-1"),
            WatchSubscriptionId.Parse("subscription-1"),
            FactualEventType.ParkNameChanged,
            FactualTargetType.Park,
            "park-1",
            "park-1",
            1,
            UserNotification.CurrentTemplateVersion,
            "FR",
            UserNotificationStatus.Delivered,
            NowUtc.AddMinutes(-2),
            NowUtc.AddMinutes(-2),
            null,
            null,
            NowUtc.AddDays(UserNotification.RetentionDays),
            1);
        WatchSubscription subscription = WatchSubscription.Restore(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkNameChanged },
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>(),
            false,
            NowUtc.AddDays(-1),
            NowUtc.AddMinutes(-1),
            2);
        Mock<IUserNotificationRepository> notifications =
            new Mock<IUserNotificationRepository>(MockBehavior.Strict);
        Mock<IWatchSubscriptionRepository> subscriptions =
            new Mock<IWatchSubscriptionRepository>(MockBehavior.Strict);
        notifications.Setup(repository => repository.GetOwnedAsync(
                "user-1",
                UserNotificationId.Parse("notification-1"),
                CancellationToken.None))
            .ReturnsAsync(notification);
        subscriptions.Setup(repository => repository.GetOwnedAsync(
                "user-1",
                WatchSubscriptionId.Parse("subscription-1"),
                CancellationToken.None))
            .ReturnsAsync(subscription);
        UserNotificationCenterService service = CreateService(notifications.Object, subscriptions.Object);

        ApplicationResult result = await service.DeleteSourceSubscriptionAsync(
            "user-1",
            "notification-1",
            1,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("notification.changed-concurrently", result.Errors.Single().Code);
        notifications.VerifyAll();
        subscriptions.VerifyAll();
        subscriptions.Verify(repository => repository.DeleteAsync(
                It.IsAny<string>(),
                It.IsAny<WatchSubscriptionId>(),
                It.IsAny<long>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteSourceSubscriptionAsync_WhenDeleted_ShouldRecordTheCanonicalUnsubscriptionMetric()
    {
        UserNotification notification = UserNotification.Restore(
            UserNotificationId.Parse("notification-1"),
            "user-1",
            FactualChangeEventId.Parse("event-1"),
            WatchSubscriptionId.Parse("subscription-1"),
            FactualEventType.ParkNameChanged,
            FactualTargetType.Park,
            "park-1",
            "park-1",
            1,
            UserNotification.CurrentTemplateVersion,
            "FR",
            UserNotificationStatus.Delivered,
            NowUtc.AddMinutes(-2),
            NowUtc.AddMinutes(-2),
            null,
            null,
            NowUtc.AddDays(UserNotification.RetentionDays),
            1);
        WatchSubscription subscription = WatchSubscription.Restore(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkNameChanged },
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>(),
            false,
            NowUtc.AddDays(-1),
            NowUtc.AddMinutes(-1),
            1);
        Mock<IUserNotificationRepository> notifications = new(MockBehavior.Strict);
        Mock<IWatchSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        notifications.Setup(value => value.GetOwnedAsync(
                "user-1",
                UserNotificationId.Parse("notification-1"),
                CancellationToken.None))
            .ReturnsAsync(notification);
        subscriptions.Setup(value => value.GetOwnedAsync(
                "user-1",
                WatchSubscriptionId.Parse("subscription-1"),
                CancellationToken.None))
            .ReturnsAsync(subscription);
        subscriptions.Setup(value => value.DeleteAsync(
                "user-1",
                WatchSubscriptionId.Parse("subscription-1"),
                1,
                CancellationToken.None))
            .ReturnsAsync(WatchSubscriptionWriteOutcome.Success);
        Mock<IWatchPilotMetricsRepository> metrics = new(MockBehavior.Strict);
        metrics.Setup(value => value.IncrementInteractionAsync(
                new DateOnly(2026, 9, 17),
                WatchPilotInteractionKind.SubscriptionRemoved,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        WatchPilotMetricsRecorder recorder = new(
            metrics.Object,
            new Mock<ILogger<WatchPilotMetricsRecorder>>().Object,
            new FixedWatchPilotTimeProvider(
                new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero)));
        UserNotificationCenterService service = CreateService(
            notifications.Object,
            subscriptions.Object,
            recorder);

        ApplicationResult result = await service.DeleteSourceSubscriptionAsync(
            "user-1",
            "notification-1",
            1,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        notifications.VerifyAll();
        subscriptions.VerifyAll();
        metrics.VerifyAll();
    }

    private static UserNotificationCenterService CreateService(
        IUserNotificationRepository notifications,
        IWatchSubscriptionRepository subscriptions,
        WatchPilotMetricsRecorder? pilotMetricsRecorder = null)
    {
        UserCollectionTargetReader targetReader = new UserCollectionTargetReader(
            new Mock<IParkRepository>(MockBehavior.Strict).Object,
            new Mock<IParkItemRepository>(MockBehavior.Strict).Object,
            new Mock<IImageRepository>(MockBehavior.Strict).Object);
        return new UserNotificationCenterService(
            notifications,
            subscriptions,
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict).Object,
            targetReader,
            pilotMetricsRecorder: pilotMetricsRecorder);
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
