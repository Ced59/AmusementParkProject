using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Moq;
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

    [Fact]
    public async Task TryCreateMigrationPlanAsync_WhenAnotherInstanceWins_ShouldReuseItsCohort()
    {
        Mock<IMongoCollection<ParkFitPortfolioMigrationDocument>> migrations =
            new Mock<IMongoCollection<ParkFitPortfolioMigrationDocument>>(MockBehavior.Strict);
        migrations.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<ParkFitPortfolioMigrationDocument>>(),
                It.IsAny<UpdateDefinition<ParkFitPortfolioMigrationDocument>>(),
                It.Is<UpdateOptions>(options => options.IsUpsert),
                CancellationToken.None))
            .ThrowsAsync(CreateDuplicateKeyException());

        await ParkFitPortfolioActivationMigration.TryCreateMigrationPlanAsync(
            migrations.Object,
            new[] { "park-1" },
            new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        migrations.VerifyAll();
    }

    [Fact]
    public void BuildMigrationPlanUpsert_ShouldFreezeNormalizedCandidateIds()
    {
        DateTime startedAtUtc = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
        BsonDocument update = Render(
            ParkFitPortfolioActivationMigration.BuildMigrationPlanUpsert(
                new[] { "park-1", "", "park-1", "park-2" },
                startedAtUtc));
        BsonDocument inserted = update["$setOnInsert"].AsBsonDocument;

        Assert.Equal(
            new[] { "park-1", "park-2" },
            inserted["candidateParkIds"].AsBsonArray.Select(static value => value.AsString));
        Assert.Equal(startedAtUtc, inserted["startedAtUtc"].ToUniversalTime());
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

    private static BsonDocument Render(
        UpdateDefinition<ParkFitPortfolioMigrationDocument> update)
    {
        IBsonSerializer<ParkFitPortfolioMigrationDocument> serializer =
            BsonSerializer.SerializerRegistry
                .GetSerializer<ParkFitPortfolioMigrationDocument>();
        return update.Render(new RenderArgs<ParkFitPortfolioMigrationDocument>(
            serializer,
            BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }

    private static MongoWriteException CreateDuplicateKeyException()
    {
        ClusterId clusterId = new ClusterId();
        ServerId serverId = new ServerId(
            clusterId,
            new System.Net.DnsEndPoint("localhost", 27017));
        ConnectionId connectionId = new ConnectionId(serverId);
        WriteError error = (WriteError)Activator.CreateInstance(
            typeof(WriteError),
            System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic,
            null,
            new object[]
            {
                ServerErrorCategory.DuplicateKey,
                11000,
                "duplicate key",
                new BsonDocument(),
            },
            null)!;
        return new MongoWriteException(connectionId, error, null, null);
    }
}
