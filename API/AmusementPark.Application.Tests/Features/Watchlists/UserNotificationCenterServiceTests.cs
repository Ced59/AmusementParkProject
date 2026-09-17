using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class UserNotificationCenterServiceTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);

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

    private static UserNotificationCenterService CreateService(
        IUserNotificationRepository notifications,
        IWatchSubscriptionRepository subscriptions)
    {
        UserCollectionTargetReader targetReader = new UserCollectionTargetReader(
            new Mock<IParkRepository>(MockBehavior.Strict).Object,
            new Mock<IParkItemRepository>(MockBehavior.Strict).Object,
            new Mock<IImageRepository>(MockBehavior.Strict).Object);
        return new UserNotificationCenterService(
            notifications,
            subscriptions,
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict).Object,
            targetReader);
    }
}
