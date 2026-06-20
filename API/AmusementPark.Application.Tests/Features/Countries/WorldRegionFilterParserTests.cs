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
    [InlineData("asia", WorldRegionFilter.Asia)]
    [InlineData("middle-east", WorldRegionFilter.MiddleEast)]
    [InlineData("middleeast", WorldRegionFilter.MiddleEast)]
    [InlineData("oceania", WorldRegionFilter.Oceania)]
    [InlineData("australia", WorldRegionFilter.Oceania)]
    [InlineData("pacific", WorldRegionFilter.Oceania)]
    [InlineData("orient", WorldRegionFilter.Orient)]
    [InlineData("asia-pacific", WorldRegionFilter.Orient)]
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
