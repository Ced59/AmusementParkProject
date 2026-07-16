using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class StandaloneAttractionListOrderingTests
{
    [Fact]
    public void Build_WhenDefaultSort_ShouldKeepWorkToReviewFirst()
    {
        SortDefinition<StandaloneAttractionDocument> sort = StandaloneAttractionListOrdering.Build(StandaloneAttractionAdminSortField.Default, false);

        BsonDocument renderedSort = Render(sort);

        Assert.Equal(1, renderedSort["adminReviewPriority"].AsInt32);
        Assert.Equal(1, renderedSort["name"].AsInt32);
        Assert.Equal(1, renderedSort["_id"].AsInt32);
    }

    [Fact]
    public void Build_WhenUpdatedSortRequested_ShouldUseUpdatedAt()
    {
        SortDefinition<StandaloneAttractionDocument> sort = StandaloneAttractionListOrdering.Build(StandaloneAttractionAdminSortField.UpdatedAt, true);

        BsonDocument renderedSort = Render(sort);

        Assert.Equal(-1, renderedSort["updatedAt"].AsInt32);
        Assert.Equal(1, renderedSort["name"].AsInt32);
        Assert.Equal(1, renderedSort["_id"].AsInt32);
    }

    [Fact]
    public void Build_WhenCreatedSortRequested_ShouldUseCreatedAt()
    {
        SortDefinition<StandaloneAttractionDocument> sort = StandaloneAttractionListOrdering.Build(StandaloneAttractionAdminSortField.CreatedAt, false);

        BsonDocument renderedSort = Render(sort);

        Assert.Equal(1, renderedSort["createdAt"].AsInt32);
        Assert.Equal(1, renderedSort["name"].AsInt32);
        Assert.Equal(1, renderedSort["_id"].AsInt32);
    }

    private static BsonDocument Render(SortDefinition<StandaloneAttractionDocument> sort)
    {
        IBsonSerializer<StandaloneAttractionDocument> serializer = BsonSerializer.SerializerRegistry.GetSerializer<StandaloneAttractionDocument>();
        return sort.Render(serializer, BsonSerializer.SerializerRegistry);
    }
}
