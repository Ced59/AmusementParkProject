using AmusementPark.WebAPI.RateLimiting;
using Xunit;

namespace AmusementPark.WebAPI.Tests.RateLimiting;

public sealed class AvatarUploadAttemptLimiterTests
{
    [Fact]
    public void TryAcquire_ShouldLimitEachUserAndResetAfterTheWindow()
    {
        AvatarUploadAttemptLimiter limiter = new AvatarUploadAttemptLimiter();
        DateTime nowUtc = new DateTime(2026, 7, 29, 20, 0, 0, DateTimeKind.Utc);

        AvatarUploadAttemptLease first = limiter.TryAcquire("user-1", nowUtc);
        AvatarUploadAttemptLease second = limiter.TryAcquire("user-1", nowUtc.AddSeconds(1));
        AvatarUploadAttemptLease third = limiter.TryAcquire("user-1", nowUtc.AddSeconds(2));
        AvatarUploadAttemptLease rejected = limiter.TryAcquire("user-1", nowUtc.AddSeconds(3));
        AvatarUploadAttemptLease otherUser = limiter.TryAcquire("user-2", nowUtc.AddSeconds(3));
        AvatarUploadAttemptLease reset = limiter.TryAcquire(
            "user-1",
            nowUtc.Add(AvatarUploadAttemptLimiter.Window).AddSeconds(1));

        Assert.True(first.IsAcquired);
        Assert.True(second.IsAcquired);
        Assert.True(third.IsAcquired);
        Assert.False(rejected.IsAcquired);
        Assert.True(rejected.RetryAfter > TimeSpan.Zero);
        Assert.True(otherUser.IsAcquired);
        Assert.True(reset.IsAcquired);
    }
}
