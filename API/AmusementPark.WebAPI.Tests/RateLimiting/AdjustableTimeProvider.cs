using System.Threading.RateLimiting;
using AmusementPark.WebAPI.RateLimiting;
using Xunit;

namespace AmusementPark.WebAPI.Tests.RateLimiting;

internal sealed class AdjustableTimeProvider : TimeProvider
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
