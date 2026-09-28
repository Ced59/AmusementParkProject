using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalParkItemClassificationPolicyTests
{
    [Theory]
    [InlineData("Attraction", true)]
    [InlineData("DarkRide", true)]
    [InlineData("Other", false)]
    [InlineData("Restaurant", false)]
    [InlineData(null, false)]
    public void IsAttractionClassification_ShouldDisambiguateCategoryAndTypeNames(
        string? value,
        bool expected)
    {
        bool result = HistoricalParkItemClassificationPolicy.IsAttractionClassification(value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryResolveDetailedAttractionType_ShouldRejectAmbiguousOther()
    {
        bool otherResolved =
            HistoricalParkItemClassificationPolicy.TryResolveDetailedAttractionType(
                "Other",
                out ParkItemType _);
        bool darkRideResolved =
            HistoricalParkItemClassificationPolicy.TryResolveDetailedAttractionType(
                "DarkRide",
                out ParkItemType darkRide);

        Assert.False(otherResolved);
        Assert.True(darkRideResolved);
        Assert.Equal(ParkItemType.DarkRide, darkRide);
    }
}
