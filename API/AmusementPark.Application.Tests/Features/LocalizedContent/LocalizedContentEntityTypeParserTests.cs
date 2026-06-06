using AmusementPark.Application.Features.LocalizedContent;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LocalizedContent;

public sealed class LocalizedContentEntityTypeParserTests
{
    [Theory]
    [InlineData("park", LocalizedContentEntityType.Park)]
    [InlineData("parks", LocalizedContentEntityType.Park)]
    [InlineData("park-zone", LocalizedContentEntityType.ParkZone)]
    [InlineData("zone", LocalizedContentEntityType.ParkZone)]
    [InlineData("park_item", LocalizedContentEntityType.ParkItem)]
    [InlineData("attraction", LocalizedContentEntityType.ParkItem)]
    [InlineData("operator", LocalizedContentEntityType.ParkOperator)]
    [InlineData("exploitant", LocalizedContentEntityType.ParkOperator)]
    [InlineData("fondateur", LocalizedContentEntityType.ParkFounder)]
    [InlineData("manufacturer", LocalizedContentEntityType.AttractionManufacturer)]
    [InlineData("asset", LocalizedContentEntityType.Image)]
    [InlineData("tag", LocalizedContentEntityType.ImageTag)]
    [InlineData("condition-type", LocalizedContentEntityType.AccessConditionType)]
    public void TryParse_WhenAliasIsKnown_ShouldReturnTrueAndExpectedType(string value, LocalizedContentEntityType expected)
    {
        bool success = LocalizedContentEntityTypeParser.TryParse(value, out LocalizedContentEntityType entityType);

        Assert.True(success);
        Assert.Equal(expected, entityType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown")]
    public void TryParse_WhenValueIsUnknown_ShouldReturnFalseAndDefault(string? value)
    {
        bool success = LocalizedContentEntityTypeParser.TryParse(value, out LocalizedContentEntityType entityType);

        Assert.False(success);
        Assert.Equal(default, entityType);
    }

    [Theory]
    [InlineData(LocalizedContentEntityType.Park, LocalizedContentEntityTypes.Park)]
    [InlineData(LocalizedContentEntityType.ParkZone, LocalizedContentEntityTypes.ParkZone)]
    [InlineData(LocalizedContentEntityType.ParkItem, LocalizedContentEntityTypes.ParkItem)]
    [InlineData(LocalizedContentEntityType.ParkOperator, LocalizedContentEntityTypes.ParkOperator)]
    [InlineData(LocalizedContentEntityType.ParkFounder, LocalizedContentEntityTypes.ParkFounder)]
    [InlineData(LocalizedContentEntityType.AttractionManufacturer, LocalizedContentEntityTypes.AttractionManufacturer)]
    [InlineData(LocalizedContentEntityType.Image, LocalizedContentEntityTypes.Image)]
    [InlineData(LocalizedContentEntityType.ImageTag, LocalizedContentEntityTypes.ImageTag)]
    [InlineData(LocalizedContentEntityType.AccessConditionType, LocalizedContentEntityTypes.AccessConditionType)]
    public void ToApiValue_WhenKnownTypeProvided_ShouldReturnStableApiValue(LocalizedContentEntityType entityType, string expected)
    {
        string result = LocalizedContentEntityTypeParser.ToApiValue(entityType);

        Assert.Equal(expected, result);
    }
}
