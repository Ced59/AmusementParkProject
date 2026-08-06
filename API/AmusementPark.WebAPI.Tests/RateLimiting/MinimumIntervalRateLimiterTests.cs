using System.Threading.RateLimiting;
using AmusementPark.WebAPI.RateLimiting;
using Xunit;

namespace AmusementPark.WebAPI.Tests.RateLimiting;

public sealed class MinimumIntervalRateLimiterTests
{
    [Fact]
    public void AttemptAcquire_ShouldMeasureTheIntervalFromTheLastSuccessfulPoll()
    {
        AdjustableTimeProvider timeProvider = new AdjustableTimeProvider(
            new DateTimeOffset(2026, 8, 7, 0, 0, 0, TimeSpan.Zero));
        using MinimumIntervalRateLimiter limiter = new MinimumIntervalRateLimiter(
            TimeSpan.FromSeconds(5),
            timeProvider);

        using RateLimitLease initialPoll = limiter.AttemptAcquire();
        timeProvider.Advance(TimeSpan.FromSeconds(9));
        using RateLimitLease delayedPoll = limiter.AttemptAcquire();
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        timeProvider.ShiftUtc(TimeSpan.FromHours(1));
        using RateLimitLease boundaryPoll = limiter.AttemptAcquire();

        Assert.True(initialPoll.IsAcquired);
        Assert.True(delayedPoll.IsAcquired);
        Assert.False(boundaryPoll.IsAcquired);
        Assert.True(boundaryPoll.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter));
        Assert.Equal(TimeSpan.FromSeconds(4), retryAfter);

        timeProvider.Advance(retryAfter);
        using RateLimitLease nextAllowedPoll = limiter.AttemptAcquire();
        Assert.True(nextAllowedPoll.IsAcquired);
    }

    private sealed class AdjustableTimeProvider : TimeProvider
    {
        private DateTimeOffset utcNow;
        private long timestamp;

        public AdjustableTimeProvider(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return this.utcNow;
        }

        public override long GetTimestamp()
        {
            return this.timestamp;
        }

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public void Advance(TimeSpan duration)
        {
            this.utcNow = this.utcNow.Add(duration);
            this.timestamp += duration.Ticks;
        }

        public void ShiftUtc(TimeSpan duration)
        {
            this.utcNow = this.utcNow.Add(duration);
        }
    }
}
