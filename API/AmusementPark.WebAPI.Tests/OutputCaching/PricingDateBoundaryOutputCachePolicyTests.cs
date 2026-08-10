using AmusementPark.WebAPI.OutputCaching;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class PricingDateBoundaryOutputCachePolicyTests
{
    [Fact]
    public void ResolveExpiration_DuringTheDay_ShouldKeepTheMaximumDuration()
    {
        DateTimeOffset nowUtc = new DateTimeOffset(2026, 8, 9, 12, 0, 0, TimeSpan.Zero);

        TimeSpan expiration = PricingDateBoundaryOutputCachePolicy.ResolveExpiration(nowUtc);

        Assert.Equal(TimeSpan.FromMinutes(30), expiration);
    }

    [Fact]
    public void ResolveExpiration_BeforeUtcDateRollover_ShouldStopAtMidnight()
    {
        DateTimeOffset nowUtc = new DateTimeOffset(2026, 8, 9, 23, 50, 0, TimeSpan.Zero);

        TimeSpan expiration = PricingDateBoundaryOutputCachePolicy.ResolveExpiration(nowUtc);

        Assert.Equal(TimeSpan.FromMinutes(10), expiration);
    }
}
