using AmusementPark.Core.Domain.Watchlists;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Watchlists;

public sealed class WatchSubscriptionIdTests
{
    [Fact]
    public void Parse_ShouldNormalizeIdentifier()
    {
        WatchSubscriptionId id = WatchSubscriptionId.Parse(" subscription-1 ");

        Assert.Equal("subscription-1", id.Value);
        Assert.Equal("subscription-1", id.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_WithMissingIdentifier_ShouldReturnFalse(string? value)
    {
        bool parsed = WatchSubscriptionId.TryParse(value, out WatchSubscriptionId id);

        Assert.False(parsed);
        Assert.Throws<InvalidOperationException>(() => id.ToString());
    }

    [Fact]
    public void New_ShouldCreateDistinctOpaqueIdentifiers()
    {
        WatchSubscriptionId first = WatchSubscriptionId.New();
        WatchSubscriptionId second = WatchSubscriptionId.New();

        Assert.NotEqual(first, second);
        Assert.Equal(32, first.Value.Length);
        Assert.Equal(32, second.Value.Length);
    }
}
