using AmusementPark.Core.Domain.Watchlists;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Watchlists;

public sealed class NotificationDeliveryAttemptTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void DeliveryLifecycle_ShouldCountAttemptsAndBecomeTerminal()
    {
        NotificationDeliveryAttempt attempt = NotificationDeliveryAttempt.Create(
            "attempt-1",
            "user-1",
            NotificationDigestId.Parse("digest-1"),
            NowUtc);

        attempt.BeginAttempt(NowUtc.AddMinutes(1));
        attempt.RecordFailure("provider-timeout", NowUtc.AddMinutes(2));
        attempt.BeginAttempt(NowUtc.AddMinutes(3));
        attempt.MarkSucceeded(NowUtc.AddMinutes(4));

        Assert.Equal(NotificationDeliveryAttemptStatus.Succeeded, attempt.Status);
        Assert.Equal(2, attempt.AttemptCount);
        Assert.Null(attempt.LastErrorCode);
        Assert.Equal(NowUtc.AddMinutes(4), attempt.CompletedAtUtc);
        Assert.Equal(NowUtc.AddMinutes(4).AddDays(NotificationDeliveryAttempt.RetentionDays), attempt.ExpiresAtUtc);
        Assert.Throws<InvalidOperationException>(() => attempt.BeginAttempt(NowUtc.AddMinutes(5)));
    }

    [Fact]
    public void Cancel_ShouldKeepAStableReasonForOperationalMetrics()
    {
        NotificationDeliveryAttempt attempt = NotificationDeliveryAttempt.Create(
            "attempt-1",
            "user-1",
            NotificationDigestId.Parse("digest-1"),
            NowUtc);

        attempt.Cancel("preference-disabled", NowUtc.AddMinutes(1));

        Assert.Equal(NotificationDeliveryAttemptStatus.Cancelled, attempt.Status);
        Assert.Equal("preference-disabled", attempt.LastErrorCode);
        Assert.Equal(NowUtc.AddMinutes(1), attempt.CompletedAtUtc);
    }

    [Fact]
    public void MarkSucceeded_ShouldRejectAProviderSuccessWithoutAStartedAttempt()
    {
        NotificationDeliveryAttempt attempt = NotificationDeliveryAttempt.Create(
            "attempt-1",
            "user-1",
            NotificationDigestId.Parse("digest-1"),
            NowUtc);

        Assert.Throws<InvalidOperationException>(() => attempt.MarkSucceeded(NowUtc.AddMinutes(1)));
    }
}
