using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class ParkFitPortfolioActivationMigrationTests
{
    [Fact]
    public void BuildLegacyParkFilter_ShouldPreserveOnlyThePreviouslyEligiblePortfolio()
    {
        BsonDocument filter = Render(
            ParkFitPortfolioActivationMigration.BuildLegacyParkFilter());

        Assert.True(filter["isVisible"].AsBoolean);
        Assert.Equal("Operating", filter["status"].AsString);
        Assert.True(filter.Contains("latitude"));
        Assert.True(filter.Contains("longitude"));
        Assert.True(filter.Contains("$or"));
    }

    [Fact]
    public void BuildLegacyStatusUpsert_ShouldCreateAnExplicitUnversionedActiveState()
    {
        DateTime migratedAtUtc = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
        BsonDocument update = Render(
            ParkFitPortfolioActivationMigration.BuildLegacyStatusUpsert(migratedAtUtc));
        BsonDocument inserted = update["$setOnInsert"].AsBsonDocument;

        Assert.Equal("Active", inserted["state"].AsString);
        Assert.Equal(0, inserted["revision"].AsInt64);
        Assert.Empty(inserted["decisions"].AsBsonArray);
        Assert.Equal(migratedAtUtc, inserted["createdAt"].ToUniversalTime());
        Assert.Equal(migratedAtUtc, inserted["updatedAt"].ToUniversalTime());
    }

    private static BsonDocument Render(FilterDefinition<ParkDocument> filter)
    {
        IBsonSerializer<ParkDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkDocument>();
        return filter.Render(new RenderArgs<ParkDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(
        UpdateDefinition<ParkFitOperationalStatusDocument> update)
    {
        IBsonSerializer<ParkFitOperationalStatusDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<ParkFitOperationalStatusDocument>();
        return update.Render(new RenderArgs<ParkFitOperationalStatusDocument>(
            serializer,
            BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }
}
