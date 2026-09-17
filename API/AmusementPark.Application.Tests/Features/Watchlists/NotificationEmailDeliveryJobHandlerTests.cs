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
    public async Task HandleAsync_ShouldCancelBeforeSendingWhenConsentIsMissing()
    {
        NotificationDigest digest = CreateDigest();
        Mock<INotificationDigestRepository> digests =
            new Mock<INotificationDigestRepository>(MockBehavior.Strict);
        digests.Setup(repository => repository.GetAsync(digest.Id, CancellationToken.None))
            .ReturnsAsync(digest);
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
    }

    private static NotificationEmailDeliveryJobHandler CreateHandler(
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
            digests.Object,
            preferences.Object,
            attempts.Object,
            new Mock<IUserRepository>(MockBehavior.Strict).Object,
            entryResolver,
            new Mock<INotificationEmailUnsubscribeTokenProtector>(MockBehavior.Strict).Object,
            sender.Object,
            timeProvider.Object);
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
