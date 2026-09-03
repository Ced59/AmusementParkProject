using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class MongoVisitContentMutationLeaseManagerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task TryAcquireAsync_ShouldFenceDraftAndReleaseOnlyItsOwnToken()
    {
        Visit visit = Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            NowUtc.AddHours(-1));
        List<FilterDefinition<UserVisitDocument>> filters =
            new List<FilterDefinition<UserVisitDocument>>();
        List<UpdateDefinition<UserVisitDocument>> updates =
            new List<UpdateDefinition<UserVisitDocument>>();
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback((
                FilterDefinition<UserVisitDocument> filter,
                UpdateDefinition<UserVisitDocument> update,
                UpdateOptions _,
                CancellationToken _) =>
            {
                filters.Add(filter);
                updates.Add(update);
            })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<UserVisitDocument>(
                "user-visits",
                null))
            .Returns(collection.Object);
        MongoVisitContentMutationLeaseManager manager =
            new MongoVisitContentMutationLeaseManager(
                database.Object,
                new MongoDbSettings());

        IAsyncDisposable? lease = await manager.TryAcquireAsync(
            visit,
            NowUtc,
            CancellationToken.None);
        Assert.NotNull(lease);
        await lease.DisposeAsync();

        Assert.Equal(2, filters.Count);
        BsonDocument acquireFilter = Render(filters[0]);
        Assert.Equal("visit-1", acquireFilter["_id"].AsString);
        Assert.Equal("user-1", acquireFilter["userId"].AsString);
        Assert.Equal(1, acquireFilter["version"].AsInt64);
        Assert.Equal("Draft", acquireFilter["status"].AsString);
        BsonDocument acquireUpdate = Render(updates[0]);
        string token = acquireUpdate["$set"]["contentMutationLeaseToken"].AsString;
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(
            NowUtc.Add(MongoVisitContentMutationLeaseManager.LeaseDuration),
            acquireUpdate["$set"]["contentMutationLeaseExpiresAtUtc"].ToUniversalTime());

        BsonDocument releaseFilter = Render(filters[1]);
        Assert.Equal(token, releaseFilter["contentMutationLeaseToken"].AsString);
        BsonDocument releaseUpdate = Render(updates[1]);
        Assert.True(releaseUpdate["$unset"].AsBsonDocument.Contains(
            "contentMutationLeaseToken"));
        Assert.True(releaseUpdate["$unset"].AsBsonDocument.Contains(
            "contentMutationLeaseExpiresAtUtc"));
        database.VerifyAll();
        collection.Verify(
            value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    private static BsonDocument Render(FilterDefinition<UserVisitDocument> filter)
    {
        IBsonSerializer<UserVisitDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserVisitDocument>();
        return filter.Render(new RenderArgs<UserVisitDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render(UpdateDefinition<UserVisitDocument> update)
    {
        IBsonSerializer<UserVisitDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserVisitDocument>();
        return update.Render(new RenderArgs<UserVisitDocument>(
            serializer,
            BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }
}
