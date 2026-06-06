using AmusementPark.Core.Geo;
using Xunit;

namespace AmusementPark.Core.Tests.Geo;

public sealed class GeoPointTests
{
    [Fact]
    public void Constructor_WhenCoordinatesAreInsideBounds_ShouldExposeCoordinates()
    {
        GeoPoint point = new GeoPoint(50.123d, 3.456d);

        Assert.Equal(50.123d, point.Latitude);
        Assert.Equal(3.456d, point.Longitude);
    }

    [Theory]
    [InlineData(-90d, -180d)]
    [InlineData(90d, 180d)]
    [InlineData(0d, 0d)]
    public void Constructor_WhenCoordinatesAreBoundaryValues_ShouldAcceptCoordinates(double latitude, double longitude)
    {
        GeoPoint point = new GeoPoint(latitude, longitude);

        Assert.Equal(latitude, point.Latitude);
        Assert.Equal(longitude, point.Longitude);
    }

    [Theory]
    [InlineData(-90.0001d)]
    [InlineData(90.0001d)]
    public void Constructor_WhenLatitudeIsOutsideBounds_ShouldThrow(double latitude)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(latitude, 0d));

        Assert.Equal("latitude", exception.ParamName);
    }

    [Theory]
    [InlineData(-180.0001d)]
    [InlineData(180.0001d)]
    public void Constructor_WhenLongitudeIsOutsideBounds_ShouldThrow(double longitude)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(0d, longitude));

        Assert.Equal("longitude", exception.ParamName);
    }
}
