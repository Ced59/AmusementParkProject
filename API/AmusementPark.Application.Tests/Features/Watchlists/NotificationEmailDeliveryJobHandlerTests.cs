using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Domain.Watchlists;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class NotificationEmailDeliveryJobHandlerTests
{
    private static readonly DateTime PeriodStartUtc =
        new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime NowUtc = PeriodStartUtc.AddDays(1).AddMinutes(5);

    [Fact]
    public async Task HandleAsync_ShouldStopBeforeCreatingAttemptWhenOwnerIsDeleted()
    {
        NotificationDigest digest = CreateDigest();
        Mock<INotificationDigestRepository> digests =
            new Mock<INotificationDigestRepository>(MockBehavior.Strict);
        digests.Setup(repository => repository.GetAsync(digest.Id, CancellationToken.None))
            .ReturnsAsync(digest);
        Mock<IWatchlistAccountDeletionFence> fence =
            new Mock<IWatchlistAccountDeletionFence>(MockBehavior.Strict);
        fence.Setup(candidate => candidate.IsBlockedAsync("user-1", CancellationToken.None))
            .ReturnsAsync(true);
        Mock<INotificationEmailPreferenceRepository> preferences =
            new Mock<INotificationEmailPreferenceRepository>(MockBehavior.Strict);
        Mock<INotificationDeliveryAttemptRepository> attempts =
            new Mock<INotificationDeliveryAttemptRepository>(MockBehavior.Strict);
        Mock<INotificationDigestEmailSender> sender =
            new Mock<INotificationDigestEmailSender>(MockBehavior.Strict);
        NotificationEmailDeliveryJobHandler handler = CreateHandler(
            fence,
            digests,
            preferences,
            attempts,
            sender);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(digest),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        fence.VerifyAll();
        digests.VerifyAll();
        preferences.VerifyNoOtherCalls();
        attempts.VerifyNoOtherCalls();
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldRemoveAttemptWhenDeletionStartsDuringCreation()
    {
        NotificationDigest digest = CreateDigest();
        Mock<INotificationDigestRepository> digests =
            new Mock<INotificationDigestRepository>(MockBehavior.Strict);
        digests.Setup(repository => repository.GetAsync(digest.Id, CancellationToken.None))
            .ReturnsAsync(digest);
        Mock<IWatchlistAccountDeletionFence> fence =
            new Mock<IWatchlistAccountDeletionFence>(MockBehavior.Strict);
        fence.SetupSequence(candidate => candidate.IsBlockedAsync("user-1", CancellationToken.None))
            .ReturnsAsync(false)
            .ReturnsAsync(true);
        Mock<INotificationEmailPreferenceRepository> preferences =
            new Mock<INotificationEmailPreferenceRepository>(MockBehavior.Strict);
        Mock<INotificationDeliveryAttemptRepository> attempts =
            new Mock<INotificationDeliveryAttemptRepository>(MockBehavior.Strict);
        attempts.Setup(repository => repository.GetAsync(
                $"email:{digest.Id.Value}",
                CancellationToken.None))
            .ReturnsAsync((NotificationDeliveryAttempt?)null);
        attempts.Setup(repository => repository.CreateAsync(
                It.IsAny<NotificationDeliveryAttempt>(),
                CancellationToken.None))
            .ReturnsAsync(NotificationDeliveryAttemptWriteOutcome.Success);
        attempts.Setup(repository => repository.DeleteAsync(
                $"email:{digest.Id.Value}",
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<INotificationDigestEmailSender> sender =
            new Mock<INotificationDigestEmailSender>(MockBehavior.Strict);
        NotificationEmailDeliveryJobHandler handler = CreateHandler(
            fence,
            digests,
            preferences,
            attempts,
            sender);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(digest),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        fence.VerifyAll();
        digests.VerifyAll();
        attempts.VerifyAll();
        preferences.VerifyNoOtherCalls();
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotSendWhenDeletionBlocksTheDeliveryLease()
    {
        FactualChangeEvent factualEvent = CreatePublishedEvent();
        WatchSubscription subscription = CreateSubscription();
        NotificationDigest digest = CreateDigest(factualEvent, subscription);
        Mock<INotificationDigestRepository> digests =
            new Mock<INotificationDigestRepository>(MockBehavior.Strict);
        digests.Setup(repository => repository.GetAsync(digest.Id, CancellationToken.None))
            .ReturnsAsync(digest);
        Mock<IWatchlistAccountDeletionFence> fence =
            new Mock<IWatchlistAccountDeletionFence>(MockBehavior.Strict);
        fence.Setup(candidate => candidate.IsBlockedAsync("user-1", CancellationToken.None))
            .ReturnsAsync(false);
        fence.Setup(candidate => candidate.TryAcquireActivityLeaseAsync(
                "user-1",
                TimeSpan.FromMinutes(3),
                CancellationToken.None))
            .ReturnsAsync((string?)null);
        Mock<INotificationEmailPreferenceRepository> preferences =
            new Mock<INotificationEmailPreferenceRepository>(MockBehavior.Strict);
        preferences.Setup(repository => repository.GetAsync("user-1", CancellationToken.None))
            .ReturnsAsync(NotificationEmailPreference.CreateConsented(
                "user-1",
                NotificationEmailPreference.CurrentConsentTextVersion,
                "fr",
                PeriodStartUtc.AddDays(-1)));
        Mock<INotificationDeliveryAttemptRepository> attempts =
            new Mock<INotificationDeliveryAttemptRepository>(MockBehavior.Strict);
        attempts.Setup(repository => repository.GetAsync(
                $"email:{digest.Id.Value}",
                CancellationToken.None))
            .ReturnsAsync((NotificationDeliveryAttempt?)null);
        attempts.Setup(repository => repository.CreateAsync(
                It.IsAny<NotificationDeliveryAttempt>(),
                CancellationToken.None))
            .ReturnsAsync(NotificationDeliveryAttemptWriteOutcome.Success);
        attempts.Setup(repository => repository.ReplaceAsync(
                It.Is<NotificationDeliveryAttempt>(attempt => attempt.AttemptCount == 1),
                1,
                CancellationToken.None))
            .ReturnsAsync(NotificationDeliveryAttemptWriteOutcome.Success);
        attempts.Setup(repository => repository.DeleteAsync(
                $"email:{digest.Id.Value}",
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(repository => repository.GetByIdAsync("user-1", CancellationToken.None))
            .ReturnsAsync(new User
            {
                Id = "user-1",
                Email = "member@example.com",
                IsActivated = true,
            });
        Mock<IWatchSubscriptionRepository> subscriptions =
            new Mock<IWatchSubscriptionRepository>(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.ListOwnedByIdsAsync(
                "user-1",
                It.Is<IReadOnlyCollection<WatchSubscriptionId>>(ids => ids.Single() == subscription.Id),
                CancellationToken.None))
            .ReturnsAsync(new[] { subscription });
        Mock<IFactualChangeEventRepository> events =
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict);
        events.Setup(repository => repository.GetManyAsync(
                It.Is<IReadOnlyCollection<FactualChangeEventId>>(ids => ids.Single() == factualEvent.Id),
                CancellationToken.None))
            .ReturnsAsync(new[] { factualEvent });
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<string>>(ids => ids.Single() == "park-1"),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new Park
                {
                    Id = "park-1",
                    Name = "Parc exemple",
                    IsVisible = true,
                    Status = ParkStatus.Operating,
                },
            });
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(repository => repository.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.Park,
                It.Is<IReadOnlyCollection<string>>(ids => ids.Single() == "park-1"),
                ImageCategory.Park,
                true,
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, string>());
        NotificationDigestEmailEntryResolver entryResolver = new NotificationDigestEmailEntryResolver(
            subscriptions.Object,
            events.Object,
            new UserCollectionTargetReader(
                parks.Object,
                new Mock<IParkItemRepository>(MockBehavior.Strict).Object,
                images.Object));
        Mock<INotificationDigestEmailSender> sender =
            new Mock<INotificationDigestEmailSender>(MockBehavior.Strict);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        NotificationEmailDeliveryJobHandler handler = new NotificationEmailDeliveryJobHandler(
            fence.Object,
            digests.Object,
            preferences.Object,
            attempts.Object,
            users.Object,
            entryResolver,
            new Mock<INotificationEmailUnsubscribeTokenProtector>(MockBehavior.Strict).Object,
            sender.Object,
            timeProvider.Object);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(digest),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        fence.VerifyAll();
        digests.VerifyAll();
        preferences.VerifyAll();
        attempts.VerifyAll();
        users.VerifyAll();
        subscriptions.VerifyAll();
        events.VerifyAll();
        parks.VerifyAll();
        images.VerifyAll();
        sender.VerifyNoOtherCalls();
        timeProvider.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldCancelBeforeSendingWhenConsentIsMissing()
    {
        NotificationDigest digest = CreateDigest();
        Mock<INotificationDigestRepository> digests =
            new Mock<INotificationDigestRepository>(MockBehavior.Strict);
        digests.Setup(repository => repository.GetAsync(digest.Id, CancellationToken.None))
            .ReturnsAsync(digest);
        Mock<IWatchlistAccountDeletionFence> fence = CreateOpenFence();
        Mock<INotificationEmailPreferenceRepository> preferences =
            new Mock<INotificationEmailPreferenceRepository>(MockBehavior.Strict);
        preferences.Setup(repository => repository.GetAsync("user-1", CancellationToken.None))
            .ReturnsAsync((NotificationEmailPreference?)null);
        Mock<INotificationDeliveryAttemptRepository> attempts =
            new Mock<INotificationDeliveryAttemptRepository>(MockBehavior.Strict);
        attempts.Setup(repository => repository.GetAsync(
                $"email:{digest.Id.Value}",
                CancellationToken.None))
            .ReturnsAsync((NotificationDeliveryAttempt?)null);
        attempts.Setup(repository => repository.CreateAsync(
                It.Is<NotificationDeliveryAttempt>(attempt =>
                    attempt.Status == NotificationDeliveryAttemptStatus.Pending),
                CancellationToken.None))
            .ReturnsAsync(NotificationDeliveryAttemptWriteOutcome.Success);
        attempts.Setup(repository => repository.ReplaceAsync(
                It.Is<NotificationDeliveryAttempt>(attempt =>
                    attempt.Status == NotificationDeliveryAttemptStatus.Cancelled
                    && attempt.LastErrorCode == NotificationEmailDeliveryErrorCodes.PreferenceDisabled),
                1,
                CancellationToken.None))
            .ReturnsAsync(NotificationDeliveryAttemptWriteOutcome.Success);
        Mock<INotificationDigestEmailSender> sender =
            new Mock<INotificationDigestEmailSender>(MockBehavior.Strict);
        NotificationEmailDeliveryJobHandler handler = CreateHandler(
            fence,
            digests,
            preferences,
            attempts,
            sender);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(digest),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        sender.VerifyNoOtherCalls();
        digests.VerifyAll();
        preferences.VerifyAll();
        attempts.VerifyAll();
        fence.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotReplayATerminalDelivery()
    {
        NotificationDigest digest = CreateDigest();
        NotificationDeliveryAttempt attempt = NotificationDeliveryAttempt.Create(
            $"email:{digest.Id.Value}",
            "user-1",
            digest.Id,
            NowUtc.AddMinutes(-2));
        attempt.BeginAttempt(NowUtc.AddMinutes(-1));
        attempt.MarkSucceeded(NowUtc);
        Mock<INotificationDigestRepository> digests =
            new Mock<INotificationDigestRepository>(MockBehavior.Strict);
        digests.Setup(repository => repository.GetAsync(digest.Id, CancellationToken.None))
            .ReturnsAsync(digest);
        Mock<IWatchlistAccountDeletionFence> fence = CreateOpenFence();
        Mock<INotificationEmailPreferenceRepository> preferences =
            new Mock<INotificationEmailPreferenceRepository>(MockBehavior.Strict);
        Mock<INotificationDeliveryAttemptRepository> attempts =
            new Mock<INotificationDeliveryAttemptRepository>(MockBehavior.Strict);
        attempts.Setup(repository => repository.GetAsync(
                $"email:{digest.Id.Value}",
                CancellationToken.None))
            .ReturnsAsync(attempt);
        Mock<INotificationDigestEmailSender> sender =
            new Mock<INotificationDigestEmailSender>(MockBehavior.Strict);
        NotificationEmailDeliveryJobHandler handler = CreateHandler(
            fence,
            digests,
            preferences,
            attempts,
            sender);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(digest),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        preferences.VerifyNoOtherCalls();
        sender.VerifyNoOtherCalls();
        digests.VerifyAll();
        attempts.VerifyAll();
        fence.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_ShouldCancelAnAmbiguousStartedAttemptWithoutReplayingTheEmail()
    {
        NotificationDigest digest = CreateDigest();
        NotificationDeliveryAttempt attempt = NotificationDeliveryAttempt.Create(
            $"email:{digest.Id.Value}",
            "user-1",
            digest.Id,
            NowUtc.AddMinutes(-2));
        attempt.BeginAttempt(NowUtc.AddMinutes(-1));
        Mock<INotificationDigestRepository> digests =
            new Mock<INotificationDigestRepository>(MockBehavior.Strict);
        digests.Setup(repository => repository.GetAsync(digest.Id, CancellationToken.None))
            .ReturnsAsync(digest);
        Mock<IWatchlistAccountDeletionFence> fence = CreateOpenFence();
        Mock<INotificationEmailPreferenceRepository> preferences =
            new Mock<INotificationEmailPreferenceRepository>(MockBehavior.Strict);
        Mock<INotificationDeliveryAttemptRepository> attempts =
            new Mock<INotificationDeliveryAttemptRepository>(MockBehavior.Strict);
        attempts.Setup(repository => repository.GetAsync(
                $"email:{digest.Id.Value}",
                CancellationToken.None))
            .ReturnsAsync(attempt);
        attempts.Setup(repository => repository.ReplaceAsync(
                It.Is<NotificationDeliveryAttempt>(candidate =>
                    candidate.Status == NotificationDeliveryAttemptStatus.Cancelled
                    && candidate.LastErrorCode
                        == NotificationEmailDeliveryErrorCodes.AmbiguousProviderAcceptance),
                2,
                CancellationToken.None))
            .ReturnsAsync(NotificationDeliveryAttemptWriteOutcome.Success);
        Mock<INotificationDigestEmailSender> sender =
            new Mock<INotificationDigestEmailSender>(MockBehavior.Strict);
        NotificationEmailDeliveryJobHandler handler = CreateHandler(
            fence,
            digests,
            preferences,
            attempts,
            sender);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            CreateContext(digest),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        preferences.VerifyNoOtherCalls();
        sender.VerifyNoOtherCalls();
        digests.VerifyAll();
        attempts.VerifyAll();
        fence.VerifyAll();
    }

    private static NotificationEmailDeliveryJobHandler CreateHandler(
        Mock<IWatchlistAccountDeletionFence> fence,
        Mock<INotificationDigestRepository> digests,
        Mock<INotificationEmailPreferenceRepository> preferences,
        Mock<INotificationDeliveryAttemptRepository> attempts,
        Mock<INotificationDigestEmailSender> sender)
    {
        UserCollectionTargetReader targetReader = new UserCollectionTargetReader(
            new Mock<IParkRepository>(MockBehavior.Strict).Object,
            new Mock<IParkItemRepository>(MockBehavior.Strict).Object,
            new Mock<IImageRepository>(MockBehavior.Strict).Object);
        NotificationDigestEmailEntryResolver entryResolver = new NotificationDigestEmailEntryResolver(
            new Mock<IWatchSubscriptionRepository>(MockBehavior.Strict).Object,
            new Mock<IFactualChangeEventRepository>(MockBehavior.Strict).Object,
            targetReader);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        return new NotificationEmailDeliveryJobHandler(
            fence.Object,
            digests.Object,
            preferences.Object,
            attempts.Object,
            new Mock<IUserRepository>(MockBehavior.Strict).Object,
            entryResolver,
            new Mock<INotificationEmailUnsubscribeTokenProtector>(MockBehavior.Strict).Object,
            sender.Object,
            timeProvider.Object);
    }

    private static Mock<IWatchlistAccountDeletionFence> CreateOpenFence()
    {
        Mock<IWatchlistAccountDeletionFence> fence =
            new Mock<IWatchlistAccountDeletionFence>(MockBehavior.Strict);
        fence.Setup(candidate => candidate.IsBlockedAsync("user-1", CancellationToken.None))
            .ReturnsAsync(false);
        return fence;
    }

    private static NotificationDigest CreateDigest()
    {
        return NotificationDigest.CreateSnapshot(
            "user-1",
            NotificationChannel.Email,
            NotificationFrequency.DailyDigest,
            PeriodStartUtc,
            Array.Empty<NotificationDigestEntry>(),
            0,
            PeriodStartUtc.AddHours(12));
    }

    private static NotificationDigest CreateDigest(
        FactualChangeEvent factualEvent,
        WatchSubscription subscription)
    {
        return NotificationDigest.CreateSnapshot(
            "user-1",
            NotificationChannel.Email,
            NotificationFrequency.DailyDigest,
            PeriodStartUtc,
            new[]
            {
                new NotificationDigestEntry(
                    factualEvent.Id,
                    subscription.Id,
                    factualEvent.DeduplicationKey,
                    factualEvent.Revision,
                    factualEvent.Type,
                    factualEvent.Target.Type,
                    factualEvent.Target.TargetId,
                    factualEvent.Status,
                    factualEvent.OccurredAtUtc),
            },
            1,
            PeriodStartUtc.AddHours(12));
    }

    private static WatchSubscription CreateSubscription()
    {
        return WatchSubscription.Create(
            WatchSubscriptionId.Parse("subscription-1"),
            "user-1",
            CollectionTargetType.Park,
            "park-1",
            new[] { FactualEventType.ParkNameChanged },
            NotificationFrequency.DailyDigest,
            new[] { NotificationChannel.Email },
            PeriodStartUtc.AddDays(-1));
    }

    private static FactualChangeEvent CreatePublishedEvent()
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
                "Annonce",
                "https://example.com/source",
                PeriodStartUtc.AddHours(1)),
            DataConfidence.High,
            PeriodStartUtc.AddHours(2),
            "park:park-1:name",
            1,
            PeriodStartUtc.AddHours(2));
        factualEvent.Verify(PeriodStartUtc.AddHours(3));
        factualEvent.Publish(PeriodStartUtc.AddHours(4));
        return factualEvent;
    }

    private static DurableBackgroundJobExecutionContext CreateContext(NotificationDigest digest)
    {
        NotificationEmailDeliveryJobPayload payload = new NotificationEmailDeliveryJobPayload(
            digest.Id.Value);
        return new DurableBackgroundJobExecutionContext(
            "job-1",
            NotificationEmailDeliveryJob.PayloadVersion,
            JsonSerializer.SerializeToElement(payload),
            1,
            1,
            digest.Id.Value);
    }
}
