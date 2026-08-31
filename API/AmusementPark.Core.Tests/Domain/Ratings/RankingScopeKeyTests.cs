using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Ratings;

public sealed class RankingScopeKeyTests
{
    [Theory]
    [InlineData("parks:global")]
    [InlineData("park-items:category:attraction")]
    [InlineData("parks:country:507f1f77bcf86cd799439011")]
    public void Parse_WhenKeyUsesTheCanonicalSyntax_ShouldPreserveIt(string value)
    {
        RankingScopeKey key = RankingScopeKey.Parse(value);

        Assert.Equal(value, key.Value);
        Assert.Equal(value, key.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("parks")]
    [InlineData(" parks:global")]
    [InlineData("parks:global ")]
    [InlineData("Parks:global")]
    [InlineData("parks::global")]
    [InlineData("parks:-global")]
    [InlineData("parks:global-")]
    [InlineData("parks:global?country=fr")]
    [InlineData("parks:global/$where")]
    public void TryParse_WhenKeyIsNotStrictlyCanonical_ShouldRejectIt(string? value)
    {
        bool parsed = RankingScopeKey.TryParse(value, out RankingScopeKey key);

        Assert.False(parsed);
        Assert.Equal(default, key);
    }

    [Fact]
    public void Parse_WhenKeyIsTooLong_ShouldRejectIt()
    {
        string value = $"parks:{new string('a', RankingScopeKey.MaximumLength)}";

        Assert.Throws<ArgumentException>(() => RankingScopeKey.Parse(value));
    }

    [Fact]
    public void Value_WhenKeyIsUninitialized_ShouldRejectTheDefaultStruct()
    {
        RankingScopeKey key = default;

        Assert.Throws<InvalidOperationException>(() => key.Value);
    }
}
