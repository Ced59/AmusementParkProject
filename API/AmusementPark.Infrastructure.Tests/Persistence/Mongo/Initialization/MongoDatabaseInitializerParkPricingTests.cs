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
        IBsonSerializer<ParkPricingDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkPricingDocument>();
        RenderArgs<ParkPricingDocument> arguments =
            new RenderArgs<ParkPricingDocument>(serializer, BsonSerializer.SerializerRegistry);
        BsonDocument keys = parkIdIndex.Keys.Render(arguments);
        Assert.Equal(1, keys["parkId"].AsInt32);
    }
}
