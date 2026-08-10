using AmusementPark.Infrastructure.Services.Seo;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Seo;

public sealed class ParkPricingSitemapRolloverRefreshServiceTests
{
    [Fact]
    public void ResolveDelayUntilNextUtcRollover_ShouldTargetTheNextDateBoundary()
    {
        DateTimeOffset nowUtc = new DateTimeOffset(2026, 8, 9, 23, 50, 0, TimeSpan.Zero);

        TimeSpan delay = ParkPricingSitemapRolloverRefreshService.ResolveDelayUntilNextUtcRollover(nowUtc);

        Assert.Equal(TimeSpan.FromMinutes(10) + TimeSpan.FromSeconds(5), delay);
    }
}
