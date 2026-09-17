using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripPlanMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldProtectOwnerCapacityAndIdempotency()
    {
        IReadOnlyCollection<CreateIndexModel<TripPlanDocument>> indexes = TripPlanMongoDefinitions.BuildIndexes();

        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_plan_owner_slot"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "uq_trip_plan_owner_operation"
            && index.Options.Unique == true);
        Assert.Contains(indexes, index => index.Options.Name == "ix_trip_plan_member_updated");
    }

    [Fact]
    public void BuildCreationOperationFilter_ShouldBindOwnerAndHashedKey()
    {
        FilterDefinition<TripPlanDocument> filter = TripPlanMongoDefinitions.BuildCreationOperationFilter(
            "user-1",
            "hash-1");
        BsonDocument rendered = filter.Render(new RenderArgs<TripPlanDocument>(
            MongoDB.Bson.Serialization.BsonSerializer.LookupSerializer<TripPlanDocument>(),
            MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry));

        Assert.Equal("user-1", rendered["ownerUserId"].AsString);
        Assert.Equal("hash-1", rendered["creationOperationKeyHash"].AsString);
    }
}
