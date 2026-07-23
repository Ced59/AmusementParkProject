using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ParkItemListOrderingTests
{
    [Fact]
    public void Build_WhenDefaultSort_ShouldPrioritizeReviewThenParkAndName()
    {
        BsonDocument sort = Render(ParkItemListOrdering.Build(ParkItemAdminSortField.Default, false));
        Assert.Equal(1, sort["adminReviewPriority"].AsInt32);
        Assert.Equal(1, sort["parkId"].AsInt32);
        Assert.Equal(1, sort["name"].AsInt32);
        Assert.Equal(1, sort["_id"].AsInt32);
    }

    [Fact]
    public void Build_WhenCategoryDescending_ShouldKeepStableNameAndIdTies()
    {
        BsonDocument sort = Render(ParkItemListOrdering.Build(ParkItemAdminSortField.Category, true));
        Assert.Equal(-1, sort["category"].AsInt32);
        Assert.Equal(1, sort["name"].AsInt32);
        Assert.Equal(1, sort["_id"].AsInt32);
    }

    [Fact]
    public void Build_WhenReviewStatusRequested_ShouldUsePersistedPriority()
    {
        BsonDocument sort = Render(ParkItemListOrdering.Build(ParkItemAdminSortField.AdminReviewStatus, false));
        Assert.Equal(1, sort["adminReviewPriority"].AsInt32);
        Assert.False(sort.Contains("adminReviewStatus"));
    }

    private static BsonDocument Render(SortDefinition<ParkItemDocument> sort)
    {
        IBsonSerializer<ParkItemDocument> serializer = BsonSerializer.SerializerRegistry.GetSerializer<ParkItemDocument>();
        RenderArgs<ParkItemDocument> arguments = new RenderArgs<ParkItemDocument>(serializer, BsonSerializer.SerializerRegistry);
        return sort.Render(arguments);
    }
}
