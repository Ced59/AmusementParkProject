using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ParkListOrderingTests
{
    [Fact]
    public void Build_WhenPublicDefaultSort_ShouldPutValidatedParksFirst()
    {
        SortDefinition<ParkDocument> sort = ParkListOrdering.Build(ParkAdminSortField.Default, false, includeHidden: false);

        BsonDocument renderedSort = Render(sort);

        Assert.Equal(-1, renderedSort["adminReviewStatus"].AsInt32);
        Assert.Equal(1, renderedSort["name"].AsInt32);
        Assert.Equal(1, renderedSort["_id"].AsInt32);
        Assert.False(renderedSort.Contains("adminReviewPriority"));
    }

    [Fact]
    public void Build_WhenAdminDefaultSort_ShouldKeepWorkToReviewFirst()
    {
        SortDefinition<ParkDocument> sort = ParkListOrdering.Build(ParkAdminSortField.Default, false, includeHidden: true);

        BsonDocument renderedSort = Render(sort);

        Assert.Equal(1, renderedSort["adminReviewPriority"].AsInt32);
        Assert.Equal(1, renderedSort["name"].AsInt32);
        Assert.Equal(1, renderedSort["_id"].AsInt32);
        Assert.False(renderedSort.Contains("adminReviewStatus"));
    }

    [Fact]
    public void Build_WhenNameSortRequested_ShouldNotUseReviewStatus()
    {
        SortDefinition<ParkDocument> sort = ParkListOrdering.Build(ParkAdminSortField.Name, true, includeHidden: false);

        BsonDocument renderedSort = Render(sort);

        Assert.Equal(-1, renderedSort["name"].AsInt32);
        Assert.Equal(1, renderedSort["_id"].AsInt32);
        Assert.False(renderedSort.Contains("adminReviewStatus"));
        Assert.False(renderedSort.Contains("adminReviewPriority"));
    }

    private static BsonDocument Render(SortDefinition<ParkDocument> sort)
    {
        IBsonSerializer<ParkDocument> serializer = BsonSerializer.SerializerRegistry.GetSerializer<ParkDocument>();
        return sort.Render(serializer, BsonSerializer.SerializerRegistry);
    }
}
