using AmusementPark.Application.Features.Countries;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Countries;

public sealed class WorldRegionFilterParserTests
{
    [Theory]
    [InlineData("europe", WorldRegionFilter.Europe)]
    [InlineData(" North_America ", WorldRegionFilter.NorthAmerica)]
    [InlineData("northamerica", WorldRegionFilter.NorthAmerica)]
    [InlineData("america-north", WorldRegionFilter.NorthAmerica)]
    [InlineData("south-america", WorldRegionFilter.SouthAmerica)]
    [InlineData("southamerica", WorldRegionFilter.SouthAmerica)]
    [InlineData("america-south", WorldRegionFilter.SouthAmerica)]
    [InlineData("orient", WorldRegionFilter.Orient)]
    [InlineData("asia", WorldRegionFilter.Orient)]
    [InlineData("asia-pacific", WorldRegionFilter.Orient)]
    [InlineData("middle-east", WorldRegionFilter.Orient)]
    [InlineData("africa", WorldRegionFilter.Africa)]
    public void Parse_WhenAliasIsKnown_ShouldReturnExpectedRegion(string value, WorldRegionFilter expected)
    {
        WorldRegionFilter? result = WorldRegionFilterParser.Parse(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown")]
    public void Parse_WhenAliasIsUnknownOrBlank_ShouldReturnNull(string? value)
    {
        WorldRegionFilter? result = WorldRegionFilterParser.Parse(value);

        Assert.Null(result);
    }
}
