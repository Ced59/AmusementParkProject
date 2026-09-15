using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class ParkFitPortfolioStateMigrationTests
{
    [Fact]
    public async Task MigrateAsync_WhenPlanIsIncomplete_ShouldPreserveExistingStatesAndCompleteAllBatches()
    {
        DateTime startedAtUtc = new DateTime(2026, 9, 15, 11, 0, 0, DateTimeKind.Utc);
        DateTime completedAtUtc = startedAtUtc.AddMinutes(1);
        ParkFitPortfolioMigrationDocument migrationPlan = new ParkFitPortfolioMigrationDocument
        {
            Id = ParkFitPortfolioStateMigration.MigrationId,
            StartedAtUtc = startedAtUtc,
        };
        Mock<IAsyncCursor<ParkFitPortfolioMigrationDocument>> migrationCursor =
            CreateAsyncCursor(new[] { migrationPlan });
        Mock<IAsyncCursor<ParkDocument>> parkCursor = CreateAsyncCursor(
            new[]
            {
                new ParkDocument { Id = "park-active" },
                new ParkDocument { Id = "park-suspended" },
            },
            new[] { new ParkDocument { Id = "park-new" } });
        Mock<IMongoCollection<ParkDocument>> parks =
            new Mock<IMongoCollection<ParkDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<ParkFitOperationalStatusDocument>> statuses =
            new Mock<IMongoCollection<ParkFitOperationalStatusDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<ParkFitPortfolioMigrationDocument>> migrations =
            new Mock<IMongoCollection<ParkFitPortfolioMigrationDocument>>(MockBehavior.Strict);
        SetupFind(migrations, migrationCursor);
        SetupFind(parks, parkCursor);
        List<IReadOnlyCollection<WriteModel<ParkFitOperationalStatusDocument>>> batches = new();
        statuses.Setup(collection => collection.BulkWriteAsync(
                It.IsAny<IEnumerable<WriteModel<ParkFitOperationalStatusDocument>>>(),
                It.Is<BulkWriteOptions>(options => !options.IsOrdered),
                CancellationToken.None))
            .Callback((
                IEnumerable<WriteModel<ParkFitOperationalStatusDocument>> writes,
                BulkWriteOptions _,
                CancellationToken _) => batches.Add(writes.ToArray()))
            .ReturnsAsync(() => CreateBulkWriteResult(batches[^1].Count));
        UpdateDefinition<ParkFitPortfolioMigrationDocument>? completionUpdate = null;
        migrations.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<ParkFitPortfolioMigrationDocument>>(),
                It.IsAny<UpdateDefinition<ParkFitPortfolioMigrationDocument>>(),
                null,
                CancellationToken.None))
            .Callback((
                FilterDefinition<ParkFitPortfolioMigrationDocument> _,
                UpdateDefinition<ParkFitPortfolioMigrationDocument> update,
                UpdateOptions? _,
                CancellationToken _) =>
            {
                Assert.Equal(2, batches.Count);
                completionUpdate = update;
            })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        ParkFitPortfolioStateMigration migration = new ParkFitPortfolioStateMigration(
            parks.Object,
            statuses.Object,
            migrations.Object,
            new FixedTimeProvider(completedAtUtc));

        long migratedCount = await migration.MigrateAsync(CancellationToken.None);

        Assert.Equal(3, migratedCount);
        Assert.Equal(new[] { 2, 1 }, batches.Select(static batch => batch.Count));
        Assert.All(batches.SelectMany(static batch => batch), write =>
        {
            UpdateOneModel<ParkFitOperationalStatusDocument> upsert =
                Assert.IsType<UpdateOneModel<ParkFitOperationalStatusDocument>>(write);
            Assert.True(upsert.IsUpsert);
            BsonDocument renderedUpdate = Render(upsert.Update);
            Assert.True(renderedUpdate.Contains("$setOnInsert"));
            Assert.False(renderedUpdate.Contains("$set"));
        });
        Assert.NotNull(completionUpdate);
        BsonDocument renderedCompletion = Render(completionUpdate);
        Assert.Equal(
            completedAtUtc,
            renderedCompletion["$set"]["completedAtUtc"].ToUniversalTime());
        parks.VerifyAll();
        statuses.VerifyAll();
        migrations.VerifyAll();
        parkCursor.VerifyAll();
        migrationCursor.VerifyAll();
    }

    [Fact]
    public async Task MigrateAsync_WhenAStatusBatchFails_ShouldNotMarkThePlanComplete()
    {
        DateTime startedAtUtc = new DateTime(2026, 9, 15, 11, 0, 0, DateTimeKind.Utc);
        Mock<IAsyncCursor<ParkFitPortfolioMigrationDocument>> migrationCursor =
            CreateAsyncCursor(new[]
            {
                new ParkFitPortfolioMigrationDocument
                {
                    Id = ParkFitPortfolioStateMigration.MigrationId,
                    StartedAtUtc = startedAtUtc,
                },
            });
        Mock<IAsyncCursor<ParkDocument>> parkCursor = CreateAsyncCursor(
            new[] { new ParkDocument { Id = "park-1" } });
        Mock<IMongoCollection<ParkDocument>> parks =
            new Mock<IMongoCollection<ParkDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<ParkFitOperationalStatusDocument>> statuses =
            new Mock<IMongoCollection<ParkFitOperationalStatusDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<ParkFitPortfolioMigrationDocument>> migrations =
            new Mock<IMongoCollection<ParkFitPortfolioMigrationDocument>>(MockBehavior.Strict);
        SetupFind(migrations, migrationCursor);
        SetupFind(parks, parkCursor);
        statuses.Setup(collection => collection.BulkWriteAsync(
                It.IsAny<IEnumerable<WriteModel<ParkFitOperationalStatusDocument>>>(),
                It.IsAny<BulkWriteOptions>(),
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("write failed"));
        ParkFitPortfolioStateMigration migration = new ParkFitPortfolioStateMigration(
            parks.Object,
            statuses.Object,
            migrations.Object,
            new FixedTimeProvider(startedAtUtc.AddMinutes(1)));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            migration.MigrateAsync(CancellationToken.None));

        migrations.Verify(collection => collection.UpdateOneAsync(
            It.IsAny<FilterDefinition<ParkFitPortfolioMigrationDocument>>(),
            It.IsAny<UpdateDefinition<ParkFitPortfolioMigrationDocument>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
        parks.VerifyAll();
        statuses.VerifyAll();
        parkCursor.VerifyAll();
        migrationCursor.VerifyAll();
    }

    [Fact]
    public void BuildPortfolioParkFilter_ShouldSelectPersistedParks()
    {
        BsonDocument filter = Render(
            ParkFitPortfolioStateMigration.BuildPortfolioParkFilter());

        Assert.Equal(string.Empty, filter["_id"]["$ne"].AsString);
    }

    [Fact]
    public void BuildDefaultStatusUpsert_ShouldCreateAnExplicitNotActivatedState()
    {
        DateTime migratedAtUtc = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        BsonDocument update = Render(
            ParkFitPortfolioStateMigration.BuildDefaultStatusUpsert(migratedAtUtc));
        BsonDocument inserted = update["$setOnInsert"].AsBsonDocument;

        Assert.Equal("NotActivated", inserted["state"].AsString);
        Assert.Equal(0, inserted["revision"].AsInt64);
        Assert.Empty(inserted["decisions"].AsBsonArray);
        Assert.Equal(migratedAtUtc, inserted["createdAt"].ToUniversalTime());
        Assert.Equal(migratedAtUtc, inserted["updatedAt"].ToUniversalTime());
    }

    [Fact]
    public async Task TryCreateMigrationPlanAsync_WhenAnotherInstanceWins_ShouldRemainIdempotent()
    {
        Mock<IMongoCollection<ParkFitPortfolioMigrationDocument>> migrations =
            new Mock<IMongoCollection<ParkFitPortfolioMigrationDocument>>(MockBehavior.Strict);
        migrations.Setup(collection => collection.UpdateOneAsync(
                It.IsAny<FilterDefinition<ParkFitPortfolioMigrationDocument>>(),
                It.IsAny<UpdateDefinition<ParkFitPortfolioMigrationDocument>>(),
                It.Is<UpdateOptions>(options => options.IsUpsert),
                CancellationToken.None))
            .ThrowsAsync(CreateDuplicateKeyException());

        await ParkFitPortfolioStateMigration.TryCreateMigrationPlanAsync(
            migrations.Object,
            new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        migrations.VerifyAll();
    }

    [Fact]
    public void BuildMigrationPlanUpsert_ShouldPersistTheStartTimeOnlyOnInsert()
    {
        DateTime startedAtUtc = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        BsonDocument update = Render(
            ParkFitPortfolioStateMigration.BuildMigrationPlanUpsert(startedAtUtc));
        BsonDocument inserted = update["$setOnInsert"].AsBsonDocument;

        Assert.Equal(startedAtUtc, inserted["startedAtUtc"].ToUniversalTime());
        Assert.False(update.Contains("$set"));
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

    private static void SetupFind<TDocument>(
        Mock<IMongoCollection<TDocument>> collection,
        Mock<IAsyncCursor<TDocument>> cursor)
    {
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<TDocument>>(),
                It.IsAny<FindOptions<TDocument, TDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(cursor.Object);
    }

    private static Mock<IAsyncCursor<TDocument>> CreateAsyncCursor<TDocument>(
        params IReadOnlyCollection<TDocument>[] batches)
    {
        int index = -1;
        Mock<IAsyncCursor<TDocument>> cursor =
            new Mock<IAsyncCursor<TDocument>>(MockBehavior.Strict);
        cursor.Setup(value => value.MoveNextAsync(CancellationToken.None))
            .ReturnsAsync(() =>
            {
                index++;
                return index < batches.Length;
            });
        cursor.SetupGet(value => value.Current).Returns(() => batches[index]);
        cursor.Setup(value => value.Dispose());
        return cursor;
    }

    private static BulkWriteResult<ParkFitOperationalStatusDocument> CreateBulkWriteResult(
        int upsertCount)
    {
        return new BulkWriteResult<ParkFitOperationalStatusDocument>.Acknowledged(
            upsertCount,
            0,
            0,
            0,
            0,
            Array.Empty<WriteModel<ParkFitOperationalStatusDocument>>(),
            new BulkWriteUpsert[upsertCount]);
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
