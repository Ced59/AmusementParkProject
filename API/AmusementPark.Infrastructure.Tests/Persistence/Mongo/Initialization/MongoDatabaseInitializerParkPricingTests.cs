using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkPricing;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class MongoDatabaseInitializerParkPricingTests
{
    [Fact]
    public void BuildParkPricingIndexes_ShouldMakeParkIdUnique()
    {
        IReadOnlyCollection<CreateIndexModel<ParkPricingDocument>> indexes =
            MongoDatabaseInitializer.BuildParkPricingIndexes();
        CreateIndexModel<ParkPricingDocument> parkIdIndex = Assert.Single(
            indexes,
            static index => string.Equals(index.Options.Name, "idx_park_pricing_park_id_unique", StringComparison.Ordinal));

        Assert.True(parkIdIndex.Options.Unique);
        BsonDocument keys = parkIdIndex.Keys.Render(
            BsonSerializer.SerializerRegistry.GetSerializer<ParkPricingDocument>(),
            BsonSerializer.SerializerRegistry);
        Assert.Equal(1, keys["parkId"].AsInt32);
    }
}
