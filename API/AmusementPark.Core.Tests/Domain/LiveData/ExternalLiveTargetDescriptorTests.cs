using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class ExternalLiveTargetDescriptorTests
{
    [Fact]
    public void Constructor_ForParkItem_ShouldNormalizeParentEvidence()
    {
        ExternalLiveTargetDescriptor descriptor = new ExternalLiveTargetDescriptor(
            LiveTargetType.ParkItem,
            "  external-item  ",
            "  external-park  ",
            "  Black Mamba  ",
            "  Phantasialand  ",
            " de ");

        Assert.Equal("external-item", descriptor.Id);
        Assert.Equal("external-park", descriptor.ParentId);
        Assert.Equal("Black Mamba", descriptor.DisplayName);
        Assert.Equal("Phantasialand", descriptor.ParentDisplayName);
        Assert.Equal("DE", descriptor.CountryCode);
    }

    [Fact]
    public void Constructor_ForParkWithParentEvidence_ShouldRejectDescriptor()
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => new ExternalLiveTargetDescriptor(
                LiveTargetType.Park,
                "external-park",
                "unexpected-parent",
                "Phantasialand",
                "Unexpected",
                "DE"));

        Assert.Equal(LiveDataErrorCodes.InvalidMapping, exception.Code);
    }

    [Fact]
    public void Constructor_ForParkItemWithoutParentEvidence_ShouldRejectDescriptor()
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => new ExternalLiveTargetDescriptor(
                LiveTargetType.ParkItem,
                "external-item",
                null,
                "Black Mamba",
                null,
                "DE"));

        Assert.Equal(LiveDataErrorCodes.InvalidMapping, exception.Code);
    }

    [Theory]
    [InlineData("ÉÉ")]
    [InlineData("日本")]
    [InlineData("D1")]
    public void Constructor_WithNonAsciiCountryCode_ShouldRejectDescriptor(string countryCode)
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => new ExternalLiveTargetDescriptor(
                LiveTargetType.Park,
                "external-park",
                null,
                "Phantasialand",
                null,
                countryCode));

        Assert.Equal(LiveDataErrorCodes.InvalidMapping, exception.Code);
    }
}
