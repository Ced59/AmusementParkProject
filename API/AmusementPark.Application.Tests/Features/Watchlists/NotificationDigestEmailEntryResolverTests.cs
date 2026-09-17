using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class NotificationDigestEmailEntryResolverTests
{
    private static readonly DateTime PeriodStartUtc =
        new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ResolveEligibleAsync_ShouldSuppressAnObsoleteFactualRevision()
    {
        WatchSubscription subscription = WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkNameChanged },
            NotificationFrequency.DailyDigest,
            new[] { NotificationChannel.Email },
            PeriodStartUtc.AddDays(-1));
        FactualChangeEvent currentEvent = CreatePublishedEvent(revision: 2);
        NotificationDigestEntry obsoleteEntry = new NotificationDigestEntry(
            currentEvent.Id,
            subscription.Id,
            currentEvent.DeduplicationKey,
            1,
            currentEvent.Type,
            currentEvent.Target.Type,
            currentEvent.Target.TargetId,
            currentEvent.Status,
            currentEvent.OccurredAtUtc);
        NotificationDigest digest = NotificationDigest.CreateSnapshot(
            "user-1",
            NotificationChannel.Email,
            NotificationFrequency.DailyDigest,
            PeriodStartUtc,
            new[] { obsoleteEntry },
            1,
            PeriodStartUtc.AddHours(12));
        Mock<IWatchSubscriptionRepository> subscriptions =
            new Mock<IWatchSubscriptionRepository>(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListOwnedByIdsAsync(
                "user-1",
                It.IsAny<IReadOnlyCollection<WatchSubscriptionId>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        events.Setup(repository => repository.GetManyAsync(
                It.IsAny<IReadOnlyCollection<FactualChangeEventId>>(),
                CancellationToken.None))
            .ReturnsAsync(new[] { currentEvent });
        NotificationDigestEmailEntryResolver resolver = new NotificationDigestEmailEntryResolver(
            subscriptions.Object,
            events.Object,
            new UserCollectionTargetReader(
                new Mock<IParkRepository>(MockBehavior.Strict).Object,
                new Mock<IParkItemRepository>(MockBehavior.Strict).Object,
                new Mock<IImageRepository>(MockBehavior.Strict).Object));

        NotificationDigestEmailEntry[] entries = await resolver.ResolveEligibleAsync(
            digest,
            CancellationToken.None);

        Assert.Empty(entries);
        subscriptions.VerifyAll();
        events.VerifyAll();
    }

    private static FactualChangeEvent CreatePublishedEvent(long revision)
    {
        FactualChangeEvent factualEvent = FactualChangeEvent.CreateDraft(
            FactualChangeEventId.Parse("event-1"),
            FactualEventType.ParkNameChanged,
            ChangeTarget.ForPark("park-1"),
            FactValue.FromText("Avant"),
            FactValue.FromText("Après"),
            new SourceReference(
                SourceReferenceType.OfficialWebsite,
                "Parc exemple",
                "Annonce officielle",
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
}
