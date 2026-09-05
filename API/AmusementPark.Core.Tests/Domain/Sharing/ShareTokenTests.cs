using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Sharing;

public sealed class ShareTokenTests
{
    private const string CanonicalValue =
        "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA";

    [Fact]
    public void Parse_WhenValueIsCanonical_ShouldPreserveItExactly()
    {
        ShareToken token = ShareToken.Parse(CanonicalValue);

        Assert.Equal(CanonicalValue, token.Value);
        Assert.Equal(CanonicalValue, token.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA=")]
    [InlineData(" AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA")]
    [InlineData("AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHy+")]
    [InlineData("AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyB")]
    public void Parse_WhenValueIsNotCanonical_ShouldRejectIt(string? value)
    {
        Assert.Throws<ArgumentException>(() => ShareToken.Parse(value));
        Assert.False(ShareToken.TryParse(value, out ShareToken parsed));
        Assert.Equal(default, parsed);
    }

    [Fact]
    public void Value_WhenTokenIsUninitialized_ShouldRejectAccess()
    {
        ShareToken token = default;

        Assert.Throws<InvalidOperationException>(() => token.Value);
    }
}
