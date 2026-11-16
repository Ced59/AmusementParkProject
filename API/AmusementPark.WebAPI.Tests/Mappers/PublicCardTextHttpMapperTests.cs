using AmusementPark.WebAPI.Mappers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Mappers;

public sealed class PublicCardTextHttpMapperTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("<p> </p>&nbsp;", null)]
    [InlineData("<h2>Bonjour</h2><p>le&nbsp;monde</p>", "Bonjour le monde")]
    [InlineData("  Één &amp; twee\n三  ", "Één &amp; twee 三")]
    [InlineData("\u0085A\uFEFFB\u00a0C", "\u0085A B C")]
    public void ToCardDescription_PreservesTheBrowserPlainTextRules(string? source, string? expected)
    {
        Assert.Equal(expected, PublicCardTextHttpMapper.ToCardDescription(source));
    }

    [Fact]
    public void ToCardDescription_UsesTheSameNaturalBreakpointAndHardLimitAsTheCard()
    {
        string prefix = new string('a', 100);
        Assert.Equal(prefix + "...", PublicCardTextHttpMapper.ToCardDescription(prefix + ", " + new string('b', 100)));
        Assert.Equal(new string('x', 137) + "...", PublicCardTextHttpMapper.ToCardDescription(new string('x', 200)));
        Assert.Equal(new string('x', 140), PublicCardTextHttpMapper.ToCardDescription(new string('x', 140)));
    }
}
