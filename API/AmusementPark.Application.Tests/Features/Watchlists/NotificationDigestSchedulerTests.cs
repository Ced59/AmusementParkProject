using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class NotificationDigestSchedulerTests
{
    private static readonly DateTime OccurredAtUtc =
        new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ScheduleAsync_ShouldCoalesceOneStableGroupForEligibleCandidates()
    {
        WatchSubscription subscription = CreateSubscription(NotificationFrequency.DailyDigest);
        UserNotification notification = CreateNotification(subscription);
        Mock<IUserNotificationRepository> notifications =
            new Mock<IUserNotificationRepository>(MockBehavior.Strict);
        notifications.Setup(repository => repository.ListByFactualEventAndUsersAsync(
                notification.FactualEventId,
                It.Is<IReadOnlyCollection<string>>(users => users.Single() == "user-1"),
                CancellationToken.None))
            .ReturnsAsync(new[] { notification });
        Mock<IWatchSubscriptionRepository> subscriptions =
            new Mock<IWatchSubscriptionRepository>(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListOwnedByIdsAsync(
                "user-1",
                It.Is<IReadOnlyCollection<WatchSubscriptionId>>(ids => ids.Single() == subscription.Id),
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<IWatchlistAccountDeletionFence> fence = CreateOpenFence();
        DateTime expectedStart = OccurredAtUtc.Date;
        NotificationDigestId expectedId = NotificationDigestId.ForGroup(
            "user-1",
            NotificationChannel.Email,
            NotificationFrequency.DailyDigest,
            expectedStart);
        jobs.Setup(repository => repository.CoalesceAsync(
                It.Is<CoalesceBackgroundJobRequest>(request =>
                    request.Kind == NotificationDigestJob.Kind
                    && request.NaturalKey == $"watch-digest:{expectedId.Value}"
                    && request.RequestedRevision == 0
                    && request.AdvanceRevision),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        NotificationDigestScheduler scheduler = new NotificationDigestScheduler(
            fence.Object,
            jobs.Object,
            notifications.Object,
            subscriptions.Object);

        await scheduler.ScheduleAsync(
            notification.FactualEventId,
            new[] { "user-1", "user-1" },
            CancellationToken.None);

        notifications.VerifyAll();
        subscriptions.VerifyAll();
        jobs.VerifyAll();
        fence.VerifyAll();
    }

    [Fact]
    public async Task ScheduleAsync_ShouldIgnoreWebOnlySubscription()
    {
        WatchSubscription subscription = CreateSubscription(
            NotificationFrequency.WebOnly,
            Array.Empty<NotificationChannel>());
        UserNotification notification = CreateNotification(subscription);
        Mock<IUserNotificationRepository> notifications =
            new Mock<IUserNotificationRepository>(MockBehavior.Strict);
        notifications.Setup(repository => repository.ListByFactualEventAndUsersAsync(
                notification.FactualEventId,
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { notification });
        Mock<IWatchSubscriptionRepository> subscriptions =
            new Mock<IWatchSubscriptionRepository>(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListOwnedByIdsAsync(
                "user-1",
                It.IsAny<IReadOnlyCollection<WatchSubscriptionId>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<IWatchlistAccountDeletionFence> fence = CreateOpenFence();
        NotificationDigestScheduler scheduler = new NotificationDigestScheduler(
            fence.Object,
            jobs.Object,
            notifications.Object,
            subscriptions.Object);

        await scheduler.ScheduleAsync(
            notification.FactualEventId,
            new[] { "user-1" },
            CancellationToken.None);

        notifications.VerifyAll();
        subscriptions.VerifyAll();
        jobs.VerifyNoOtherCalls();
        fence.VerifyAll();
    }

    [Fact]
    public async Task ScheduleAsync_ShouldIgnoreEventTypeRemovedFromSubscription()
    {
        WatchSubscription subscription = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.OperatorChanged },
            NotificationFrequency.DailyDigest,
            new[] { NotificationChannel.Email },
            OccurredAtUtc.AddDays(-1));
        UserNotification notification = CreateNotification(subscription);
        Mock<IUserNotificationRepository> notifications =
            new Mock<IUserNotificationRepository>(MockBehavior.Strict);
        notifications.Setup(repository => repository.ListByFactualEventAndUsersAsync(
                notification.FactualEventId,
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { notification });
        Mock<IWatchSubscriptionRepository> subscriptions =
            new Mock<IWatchSubscriptionRepository>(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListOwnedByIdsAsync(
                "user-1",
                It.IsAny<IReadOnlyCollection<WatchSubscriptionId>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<IWatchlistAccountDeletionFence> fence = CreateOpenFence();
        NotificationDigestScheduler scheduler = new NotificationDigestScheduler(
            fence.Object,
            jobs.Object,
            notifications.Object,
            subscriptions.Object);

        await scheduler.ScheduleAsync(
            notification.FactualEventId,
            new[] { "user-1" },
            CancellationToken.None);

        notifications.VerifyAll();
        subscriptions.VerifyAll();
        jobs.VerifyNoOtherCalls();
        fence.VerifyAll();
    }

    private static Mock<IWatchlistAccountDeletionFence> CreateOpenFence()
    {
        Mock<IWatchlistAccountDeletionFence> fence =
            new Mock<IWatchlistAccountDeletionFence>(MockBehavior.Strict);
        fence.Setup(candidate => candidate.IsBlockedAsync("user-1", CancellationToken.None))
            .ReturnsAsync(false);
        return fence;
    }

    private static WatchSubscription CreateSubscription(
        NotificationFrequency frequency,
        IReadOnlyCollection<NotificationChannel>? channels = null)
    {
        return WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkNameChanged },
            frequency,
            channels ?? new[] { NotificationChannel.Email },
            OccurredAtUtc.AddDays(-1));
    }

    private static UserNotification CreateNotification(WatchSubscription subscription)
    {
        return UserNotification.Restore(
            UserNotificationId.Parse("notification-1"),
            "user-1",
            FactualChangeEventId.Parse("event-1"),
            subscription.Id,
            FactualEventType.ParkNameChanged,
            FactualTargetType.Park,
            "park-1",
            "park-1",
            1,
            UserNotification.CurrentTemplateVersion,
            "FR",
            UserNotificationStatus.Delivered,
            OccurredAtUtc,
            OccurredAtUtc,
            null,
            null,
            OccurredAtUtc.AddDays(UserNotification.RetentionDays),
            1);
    }
}
