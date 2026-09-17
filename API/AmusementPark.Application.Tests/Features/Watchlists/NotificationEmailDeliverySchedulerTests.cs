using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.Watchlists;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Watchlists;

public sealed class NotificationEmailDeliverySchedulerTests
{
    [Fact]
    public async Task ScheduleAsync_ShouldUseStableIdentityAndWaitUntilTheDigestCloses()
    {
        DateTime periodStartUtc = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
        DateTime nowUtc = periodStartUtc.AddHours(12);
        NotificationDigest digest = NotificationDigest.CreateSnapshot(
            "user-1",
            NotificationChannel.Email,
            NotificationFrequency.DailyDigest,
            periodStartUtc,
            Array.Empty<NotificationDigestEntry>(),
            0,
            nowUtc);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<INotificationEmailPreferenceRepository> preferences =
            new Mock<INotificationEmailPreferenceRepository>(MockBehavior.Strict);
        preferences.Setup(repository => repository.GetAsync("user-1", CancellationToken.None))
            .ReturnsAsync(NotificationEmailPreference.CreateConsented(
                "user-1",
                NotificationEmailPreference.CurrentConsentTextVersion,
                "fr",
                periodStartUtc.AddDays(-1)));
        jobs.Setup(repository => repository.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.Kind == NotificationEmailDeliveryJob.Kind
                    && request.IdempotencyKey == $"watch-email:{digest.Id.Value}"
                    && request.Delay == TimeSpan.FromHours(12) + NotificationEmailDeliveryScheduler.DigestSettlementDelay
                    && request.CorrelationId == digest.Id.Value),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        NotificationEmailDeliveryScheduler scheduler = new NotificationEmailDeliveryScheduler(
            jobs.Object,
            preferences.Object,
            timeProvider.Object);

        await scheduler.ScheduleAsync(digest, CancellationToken.None);

        jobs.VerifyAll();
        preferences.VerifyAll();
        timeProvider.VerifyAll();
    }

    [Fact]
    public async Task ScheduleAsync_ShouldAvoidCreatingDeliveryWorkWithoutConsent()
    {
        DateTime periodStartUtc = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
        NotificationDigest digest = NotificationDigest.CreateSnapshot(
            "user-1",
            NotificationChannel.Email,
            NotificationFrequency.DailyDigest,
            periodStartUtc,
            Array.Empty<NotificationDigestEntry>(),
            0,
            periodStartUtc.AddHours(12));
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        Mock<INotificationEmailPreferenceRepository> preferences =
            new Mock<INotificationEmailPreferenceRepository>(MockBehavior.Strict);
        preferences.Setup(repository => repository.GetAsync("user-1", CancellationToken.None))
            .ReturnsAsync((NotificationEmailPreference?)null);
        NotificationEmailDeliveryScheduler scheduler = new NotificationEmailDeliveryScheduler(
            jobs.Object,
            preferences.Object);

        await scheduler.ScheduleAsync(digest, CancellationToken.None);

        preferences.VerifyAll();
        jobs.VerifyNoOtherCalls();
    }
}
