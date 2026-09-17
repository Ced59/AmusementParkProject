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

public sealed class NotificationDigestJobHandlerTests
{
    private static readonly DateTime PeriodStartUtc =
        new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_ShouldCollapseCorrectionsAndPersistDeterministicSnapshot()
    {
        WatchSubscription subscription = CreateSubscription();
        FactualChangeEvent original = CreatePublishedEvent("event-1", 1);
        FactualChangeEvent correction = CreatePublishedEvent("event-2", 2);
        UserNotification first = CreateNotification("notification-1", original, subscription, PeriodStartUtc.AddHours(8));
        UserNotification second = CreateNotification("notification-2", correction, subscription, PeriodStartUtc.AddHours(9));
        Mock<IUserNotificationRepository> notifications =
            new Mock<IUserNotificationRepository>(MockBehavior.Strict);
        Mock<IWatchSubscriptionRepository> subscriptions =
            new Mock<IWatchSubscriptionRepository>(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListOwnedAsync(
                "user-1",
                null,
                null,
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        notifications.Setup(repository => repository.ListOwnedForDigestAsync(
                "user-1",
                PeriodStartUtc,
                PeriodStartUtc.AddDays(7),
                It.Is<IReadOnlyCollection<NotificationDigestSubscriptionFilter>>(filters =>
                    filters.Count == 1
                    && filters.Single().SubscriptionId == subscription.Id
                    && filters.Single().EventTypes.Contains(FactualEventType.ParkNameChanged)),
                NotificationDigest.MaximumEntries + 1,
                CancellationToken.None))
            .ReturnsAsync(new[] { first, second });
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        events.Setup(repository => repository.GetManyAsync(
                It.IsAny<IReadOnlyCollection<FactualChangeEventId>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { original, correction });
        Mock<INotificationDigestRepository> digests =
            new Mock<INotificationDigestRepository>(MockBehavior.Strict);
        digests.Setup(repository => repository.ReplaceSnapshotAsync(
                It.Is<NotificationDigest>(digest =>
                    digest.ObservedNotificationCount == 2
                    && digest.Entries.Count == 1
                    && digest.Entries.Single().FactualEventId == correction.Id
                    && digest.Entries.Single().SourceRevision == 2),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow())
            .Returns(new DateTimeOffset(PeriodStartUtc.AddHours(10)));
        Mock<INotificationEmailDeliveryScheduler> emailDeliveryScheduler =
            new Mock<INotificationEmailDeliveryScheduler>(MockBehavior.Strict);
        emailDeliveryScheduler.Setup(scheduler => scheduler.ScheduleAsync(
                It.Is<NotificationDigest>(digest => digest.Entries.Count == 1),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        NotificationDigestJobHandler handler = new NotificationDigestJobHandler(
            notifications.Object,
            subscriptions.Object,
            events.Object,
            digests.Object,
            emailDeliveryScheduler.Object,
            timeProvider.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        notifications.VerifyAll();
        subscriptions.VerifyAll();
        events.VerifyAll();
        digests.VerifyAll();
        emailDeliveryScheduler.VerifyAll();
        timeProvider.VerifyAll();
    }

    private static DurableBackgroundJobExecutionContext CreateContext()
    {
        NotificationDigestJobPayload payload = new NotificationDigestJobPayload(
            "user-1",
            NotificationChannel.Email,
            NotificationFrequency.WeeklyDigest,
            PeriodStartUtc);
        return new DurableBackgroundJobExecutionContext(
            "job-1",
            NotificationDigestJob.PayloadVersion,
            JsonSerializer.SerializeToElement(payload),
            PeriodStartUtc.Ticks,
            1,
            "digest-1");
    }

    private static WatchSubscription CreateSubscription()
    {
        return WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkNameChanged },
            NotificationFrequency.WeeklyDigest,
            new[] { NotificationChannel.Email },
            PeriodStartUtc.AddDays(-1));
    }

    private static FactualChangeEvent CreatePublishedEvent(string id, long revision)
    {
        FactualChangeEvent factualEvent = FactualChangeEvent.CreateDraft(
            FactualChangeEventId.Parse(id),
            FactualEventType.ParkNameChanged,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromText("Avant"),
            FactValue.FromText("Après"),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Parc exemple",
                "Annonce",
                "https://example.com/source",
                PeriodStartUtc.AddHours(1)),
            DataConfidence.High,
            PeriodStartUtc.AddHours(2),
            "park:park-1:name",
            revision,
            PeriodStartUtc.AddHours(2));
        factualEvent.Verify(PeriodStartUtc.AddHours(3));
        factualEvent.Publish(PeriodStartUtc.AddHours(4));
        return factualEvent;
    }

    private static UserNotification CreateNotification(
        string id,
        FactualChangeEvent factualEvent,
        WatchSubscription subscription,
        DateTime deliveredAtUtc)
    {
        return UserNotification.Restore(
            UserNotificationId.Parse(id),
            "user-1",
            factualEvent.Id,
            subscription.Id,
            factualEvent.Type,
            factualEvent.Target.Type,
            factualEvent.Target.TargetId,
            "park-1",
            factualEvent.Revision,
            UserNotification.CurrentTemplateVersion,
            "FR",
            UserNotificationStatus.Delivered,
            deliveredAtUtc,
            deliveredAtUtc,
            null,
            null,
            deliveredAtUtc.AddDays(UserNotification.RetentionDays),
            1);
    }
}
