using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Watchlists;

public sealed class UserNotificationTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateWeb_ShouldProjectOnePublishedMatchingFactPrivately()
    {
        FactualChangeEvent factualEvent = CreatePublishedEvent();
        WatchSubscription subscription = CreateSubscription();

        UserNotification notification = UserNotification.CreateWeb(
            UserNotificationId.Parse("notification-1"),
            factualEvent,
            subscription,
            "fr",
            NowUtc);

        Assert.Equal("user-1", notification.UserId);
        Assert.Equal("park-1", notification.ParkId);
        Assert.Equal("item-1", notification.TargetId);
        Assert.Equal(UserNotificationStatus.Delivered, notification.Status);
        Assert.True(notification.IsUnread);
        Assert.Equal(NowUtc.AddDays(UserNotification.RetentionDays), notification.ExpiresAtUtc);
        Assert.Equal(1, notification.Version);
    }

    [Fact]
    public void CreateWeb_ShouldRejectAnUnpublishedFact()
    {
        FactualChangeEvent draft = CreateDraftEvent();

        UserNotificationValidationException exception = Assert.Throws<UserNotificationValidationException>(
            () => UserNotification.CreateWeb(
                UserNotificationId.Parse("notification-1"),
                draft,
                CreateSubscription(),
                "fr",
                NowUtc));

        Assert.Equal(UserNotificationErrorCodes.EventNotDistributable, exception.Code);
    }

    [Fact]
    public void MarkReadThenDismiss_ShouldPreserveExplicitLifecycleAndVersion()
    {
        UserNotification notification = UserNotification.CreateWeb(
            UserNotificationId.Parse("notification-1"),
            CreatePublishedEvent(),
            CreateSubscription(),
            "fr",
            NowUtc);

        notification.MarkRead(NowUtc.AddMinutes(1));
        notification.MarkRead(NowUtc.AddMinutes(2));
        notification.Dismiss(NowUtc.AddMinutes(3));

        Assert.Equal(UserNotificationStatus.Dismissed, notification.Status);
        Assert.Equal(NowUtc.AddMinutes(1), notification.ReadAtUtc);
        Assert.Equal(NowUtc.AddMinutes(3), notification.DismissedAtUtc);
        Assert.Equal(3, notification.Version);
    }

    private static WatchSubscription CreateSubscription()
    {
        return WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.OpeningDateChanged },
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>(),
            NowUtc.AddHours(-1));
    }

    private static FactualChangeEvent CreatePublishedEvent()
    {
        FactualChangeEvent factualEvent = CreateDraftEvent();
        factualEvent.Verify(NowUtc.AddMinutes(-20));
        factualEvent.Publish(NowUtc.AddMinutes(-10));
        return factualEvent;
    }

    private static FactualChangeEvent CreateDraftEvent()
    {
        return FactualChangeEvent.CreateDraft(
            FactualChangeEventId.Parse("event-1"),
            FactualEventType.OpeningDateChanged,
            ChangeTarget.ForParkItem("item-1", "park-1"),
            FactValue.FromDate(new DateOnly(2027, 4, 1)),
            FactValue.FromDate(new DateOnly(2027, 4, 8)),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Example Park",
                "Opening update",
                "https://example.com/opening",
                NowUtc.AddHours(-2)),
            DataConfidence.High,
            NowUtc.AddMinutes(-40),
            "park-item:item-1:opening-date:2027",
            1,
            NowUtc.AddMinutes(-30));
    }
}
