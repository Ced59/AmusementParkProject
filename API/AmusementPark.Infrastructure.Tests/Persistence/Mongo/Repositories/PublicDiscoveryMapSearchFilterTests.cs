using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.StandaloneAttractions.Contracts;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class PublicDiscoveryMapSearchFilterTests
{
    [Fact]
    public void BuildStandaloneMapPointProjection_ShouldIncludeOnlyTheAttractionStatus()
    {
        ProjectionDefinition<StandaloneAttractionDocument> projection = StandaloneAttractionRepository.BuildMapPointProjection();
        IBsonSerializer<StandaloneAttractionDocument> serializer = BsonSerializer.SerializerRegistry.GetSerializer<StandaloneAttractionDocument>();
        BsonDocument renderedProjection = projection.Render(new RenderArgs<StandaloneAttractionDocument>(serializer, BsonSerializer.SerializerRegistry));

        Assert.Equal(1, renderedProjection["attractionDetails.status"].AsInt32);
        Assert.DoesNotContain("attractionDetails.model", renderedProjection.Names);
        Assert.DoesNotContain("attractionDetails.accessConditions", renderedProjection.Names);
    }

    [Theory]
    [InlineData("standalone attraction")]
    [InlineData("standalone")]
    [InlineData("isolated attraction")]
    [InlineData("isolated")]
    [InlineData("attraction isolee")]
    [InlineData("attraction")]
    public void BuildStandaloneMapSearchTermFilter_WhenProjectionAliasMatches_ShouldKeepAllStandalonePoints(string searchTerm)
    {
        StandaloneAttractionSearchCriteria criteria = new StandaloneAttractionSearchCriteria(
            searchTerm,
            Array.Empty<string>(),
            Array.Empty<string>());

        FilterDefinition<StandaloneAttractionDocument>? filter = StandaloneAttractionRepository.BuildMapSearchTermFilter(criteria);

        Assert.Null(filter);
    }

    [Fact]
    public void BuildStandaloneMapSearchTermFilter_WhenHumanizedTypeMatches_ShouldSearchProjectionFields()
    {
        StandaloneAttractionSearchCriteria criteria = new StandaloneAttractionSearchCriteria(
            "roller coaster",
            Array.Empty<string>(),
            Array.Empty<string>());

        FilterDefinition<StandaloneAttractionDocument>? filter = StandaloneAttractionRepository.BuildMapSearchTermFilter(criteria);

        Assert.NotNull(filter);
        string renderedFilter = Render(filter).ToJson();

        Assert.Contains("descriptions.value", renderedFilter, StringComparison.Ordinal);
        Assert.Contains("countryCode", renderedFilter, StringComparison.Ordinal);
        Assert.Contains("rollercoaster", renderedFilter, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildParkSearchTermFilter_WhenHumanizedTypeMatches_ShouldSearchProjectionFields()
    {
        ParkSearchCriteria criteria = new ParkSearchCriteria(
            "theme park",
            Array.Empty<string>(),
            Array.Empty<string>());

        FilterDefinition<ParkDocument>? filter = ParkRepository.BuildSearchTermFilter(criteria);

        Assert.NotNull(filter);
        string renderedFilter = Render(filter).ToJson();

        Assert.Contains("descriptions.value", renderedFilter, StringComparison.Ordinal);
        Assert.Contains("themepark", renderedFilter, StringComparison.OrdinalIgnoreCase);
    }

    private static BsonDocument Render<TDocument>(FilterDefinition<TDocument> filter)
    {
        IBsonSerializer<TDocument> serializer = BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        return filter.Render(new RenderArgs<TDocument>(serializer, BsonSerializer.SerializerRegistry));
    }
}
