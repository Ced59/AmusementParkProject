using AmusementPark.Application.Common.Contracts;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class CommonMongoMappersTests
{
    [Fact]
    public void ToDomain_WhenDocumentsAreNull_ShouldReturnEmptyCollection()
    {
        List<LocalizedText> result = CommonMongoMappers.ToDomain((IReadOnlyCollection<LocalizedTextDocument>?)null);

        Assert.Empty(result);
    }

    [Fact]
    public void ToDomain_WhenDocumentsAreProvided_ShouldMapLanguageAndValue()
    {
        LocalizedTextDocument[] documents = new[]
        {
            new LocalizedTextDocument { LanguageCode = "fr", Value = "Bonjour" },
        };

        List<LocalizedText> result = CommonMongoMappers.ToDomain(documents);

        LocalizedText text = Assert.Single(result);
        Assert.Equal("fr", text.LanguageCode);
        Assert.Equal("Bonjour", text.Value);
    }

    [Fact]
    public void ToDocuments_WhenLocalizedTextsAreNull_ShouldReturnEmptyCollection()
    {
        List<LocalizedTextDocument> result = CommonMongoMappers.ToDocuments((IReadOnlyCollection<LocalizedText>?)null);

        Assert.Empty(result);
    }

    [Fact]
    public void ToDocuments_WhenLocalizedTextsAreProvided_ShouldMapLanguageAndValue()
    {
        LocalizedText[] values = new[] { new LocalizedText("en", "Hello") };

        List<LocalizedTextDocument> result = CommonMongoMappers.ToDocuments(values);

        LocalizedTextDocument document = Assert.Single(result);
        Assert.Equal("en", document.LanguageCode);
        Assert.Equal("Hello", document.Value);
    }

    [Fact]
    public void ToDocuments_WhenLocalizedTextValuesAreProvided_ShouldMapLanguageAndValue()
    {
        LocalizedTextValue[] values = new[] { new LocalizedTextValue("de", "Hallo") };

        List<LocalizedTextDocument> result = CommonMongoMappers.ToDocuments(values);

        LocalizedTextDocument document = Assert.Single(result);
        Assert.Equal("de", document.LanguageCode);
        Assert.Equal("Hallo", document.Value);
    }

    [Fact]
    public void ToDocument_WhenGeoPointIsNull_ShouldReturnNull()
    {
        GeoPointDocument? result = CommonMongoMappers.ToDocument(null);

        Assert.Null(result);
    }

    [Fact]
    public void ToDocument_WhenGeoPointExists_ShouldMapCoordinates()
    {
        GeoPointDocument? result = CommonMongoMappers.ToDocument(new GeoPoint(50d, 3d));

        Assert.NotNull(result);
        Assert.Equal(50d, result.Latitude);
        Assert.Equal(3d, result.Longitude);
    }

    [Theory]
    [InlineData(null, 3d)]
    [InlineData(50d, null)]
    [InlineData(null, null)]
    public void ToDomain_WhenGeoPointDocumentIsIncomplete_ShouldReturnNull(double? latitude, double? longitude)
    {
        GeoPointDocument document = new GeoPointDocument { Latitude = latitude, Longitude = longitude };

        GeoPoint? result = CommonMongoMappers.ToDomain(document);

        Assert.Null(result);
    }

    [Fact]
    public void ApplyPosition_WhenDocumentReceivesNullPoint_ShouldClearCoordinatesAndLocation()
    {
        TestMongoGeolocatedDocument document = new TestMongoGeolocatedDocument
        {
            Latitude = 50d,
            Longitude = 3d,
        };
        document.RefreshLocation();

        CommonMongoMappers.ApplyPosition(document, null);

        Assert.Null(document.Latitude);
        Assert.Null(document.Longitude);
        Assert.Null(document.Location);
    }

    private sealed class TestMongoGeolocatedDocument : MongoGeolocatedDocumentBase
    {
    }
}
