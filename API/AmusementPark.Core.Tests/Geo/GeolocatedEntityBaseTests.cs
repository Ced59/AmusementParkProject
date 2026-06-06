using AmusementPark.Core.Geo;
using Xunit;

namespace AmusementPark.Core.Tests.Geo;

public sealed class GeolocatedEntityBaseTests
{
    [Fact]
    public void SetPosition_WhenCoordinatesAreProvided_ShouldStorePointAndTouchEntity()
    {
        TestGeolocatedEntity entity = new TestGeolocatedEntity();
        DateTime previousUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5);
        entity.UpdatedAtUtc = previousUpdatedAtUtc;

        entity.SetPosition(48.8566d, 2.3522d);

        Assert.NotNull(entity.Position);
        Assert.Equal(48.8566d, entity.Position.Latitude);
        Assert.Equal(2.3522d, entity.Position.Longitude);
        Assert.True(entity.UpdatedAtUtc > previousUpdatedAtUtc);
    }

    [Fact]
    public void SetPosition_WhenPointIsProvided_ShouldStoreSamePointAndTouchEntity()
    {
        TestGeolocatedEntity entity = new TestGeolocatedEntity();
        GeoPoint point = new GeoPoint(40d, -74d);
        DateTime previousUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5);
        entity.UpdatedAtUtc = previousUpdatedAtUtc;

        entity.SetPosition(point);

        Assert.Same(point, entity.Position);
        Assert.True(entity.UpdatedAtUtc > previousUpdatedAtUtc);
    }

    [Fact]
    public void SetPosition_WhenNullPointIsProvided_ShouldClearPointAndTouchEntity()
    {
        TestGeolocatedEntity entity = new TestGeolocatedEntity();
        entity.SetPosition(1d, 2d);
        DateTime previousUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5);
        entity.UpdatedAtUtc = previousUpdatedAtUtc;

        entity.SetPosition((GeoPoint?)null);

        Assert.Null(entity.Position);
        Assert.True(entity.UpdatedAtUtc > previousUpdatedAtUtc);
    }

    [Fact]
    public void ClearPosition_WhenPositionExists_ShouldRemovePointAndTouchEntity()
    {
        TestGeolocatedEntity entity = new TestGeolocatedEntity();
        entity.SetPosition(1d, 2d);
        DateTime previousUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5);
        entity.UpdatedAtUtc = previousUpdatedAtUtc;

        entity.ClearPosition();

        Assert.Null(entity.Position);
        Assert.True(entity.UpdatedAtUtc > previousUpdatedAtUtc);
    }

    [Fact]
    public void SetPosition_WhenInvalidCoordinatesAreProvided_ShouldLeavePreviousPositionUntouched()
    {
        TestGeolocatedEntity entity = new TestGeolocatedEntity();
        entity.SetPosition(1d, 2d);
        GeoPoint previousPosition = entity.Position!;

        Assert.Throws<ArgumentOutOfRangeException>(() => entity.SetPosition(91d, 2d));

        Assert.Same(previousPosition, entity.Position);
    }

    private sealed class TestGeolocatedEntity : GeolocatedEntityBase
    {
    }
}
