using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Documents.Common;

public sealed class MongoGeolocatedDocumentBaseTests
{
    [Fact]
    public void RefreshLocation_WhenLatitudeAndLongitudeExist_ShouldCreateGeoJsonPointWithLongitudeLatitudeOrder()
    {
        TestMongoGeolocatedDocument document = new TestMongoGeolocatedDocument
        {
            Latitude = 50.1d,
            Longitude = 3.2d,
        };

        document.RefreshLocation();

        Assert.NotNull(document.Location);
        Assert.Equal(3.2d, document.Location.Coordinates.Longitude);
        Assert.Equal(50.1d, document.Location.Coordinates.Latitude);
    }

    [Theory]
    [InlineData(null, 3.2d)]
    [InlineData(50.1d, null)]
    [InlineData(null, null)]
    public void RefreshLocation_WhenCoordinateIsMissing_ShouldClearGeoJsonPoint(double? latitude, double? longitude)
    {
        TestMongoGeolocatedDocument document = new TestMongoGeolocatedDocument
        {
            Latitude = latitude,
            Longitude = longitude,
        };

        document.RefreshLocation();

        Assert.Null(document.Location);
    }

    private sealed class TestMongoGeolocatedDocument : MongoGeolocatedDocumentBase
    {
    }
}
