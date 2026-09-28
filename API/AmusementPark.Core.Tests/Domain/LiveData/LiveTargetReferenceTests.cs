using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveTargetReferenceTests
{
    [Fact]
    public void Constructor_ForParkItem_ShouldPreserveParkContext()
    {
        LiveTargetReference target = new LiveTargetReference(
            LiveTargetType.ParkItem,
            "item-1",
            "park-1",
            "Black Mamba",
            "Phantasialand",
            "de");

        Assert.Equal("item-1", target.Id);
        Assert.Equal("park-1", target.ParkId);
        Assert.Equal("DE", target.CountryCode);
    }

    [Fact]
    public void Constructor_ForParkWithDifferentParkId_ShouldRejectReference()
    {
        LiveDataValidationException exception = Assert.Throws<LiveDataValidationException>(
            () => new LiveTargetReference(
                LiveTargetType.Park,
                "park-1",
                "park-2",
                "Phantasialand",
                "Phantasialand",
                "DE"));

        Assert.Equal(LiveDataErrorCodes.InvalidMapping, exception.Code);
    }

    [Fact]
    public void Equals_ShouldUseStableIdentityAndContextInsteadOfDisplayName()
    {
        LiveTargetReference first = new LiveTargetReference(
            LiveTargetType.ParkItem,
            "item-1",
            "park-1",
            "Old name",
            "Park",
            "DE");
        LiveTargetReference second = new LiveTargetReference(
            LiveTargetType.ParkItem,
            "item-1",
            "park-1",
            "New name",
            "Renamed park",
            "DE");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
