using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Watchlists;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Watchlists;

public sealed class CollectionTargetStatusResolverTests
{
    [Theory]
    [InlineData(ParkStatus.Operating, CollectionTargetStatus.Available)]
    [InlineData(ParkStatus.TemporarilyClosed, CollectionTargetStatus.TemporarilyClosed)]
    [InlineData(ParkStatus.ClosedDefinitively, CollectionTargetStatus.PermanentlyClosed)]
    [InlineData(ParkStatus.Cancelled, CollectionTargetStatus.PermanentlyClosed)]
    [InlineData(ParkStatus.Planned, CollectionTargetStatus.Unknown)]
    public void Resolve_MapsParkLifecycle(
        ParkStatus status,
        CollectionTargetStatus expected)
    {
        Assert.Equal(expected, CollectionTargetStatusResolver.Resolve(status));
    }

    [Fact]
    public void Resolve_PreservesAClosedParentOverAnOperatingItem()
    {
        ParkItem item = new ParkItem
        {
            AttractionDetails = new AttractionDetails { Status = "Operating" },
        };

        CollectionTargetStatus result = CollectionTargetStatusResolver.Resolve(
            item,
            ParkStatus.TemporarilyClosed);

        Assert.Equal(CollectionTargetStatus.TemporarilyClosed, result);
    }

    [Theory]
    [InlineData("TemporarilyClosed", CollectionTargetStatus.TemporarilyClosed)]
    [InlineData("Removed", CollectionTargetStatus.PermanentlyClosed)]
    [InlineData("Operating", CollectionTargetStatus.Available)]
    public void Resolve_UsesNormalizedItemStatus(
        string itemStatus,
        CollectionTargetStatus expected)
    {
        ParkItem item = new ParkItem
        {
            AttractionDetails = new AttractionDetails { Status = itemStatus },
        };

        Assert.Equal(
            expected,
            CollectionTargetStatusResolver.Resolve(item, ParkStatus.Operating));
    }
}
