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

public sealed class UserVisitRepositoryTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateAsync_ShouldInsertWithoutUpsertAndReturnThePersistedVisit()
    {
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        UserVisitDocument? insertedDocument = null;
        collection.Setup(value => value.InsertOneAsync(
                It.IsAny<UserVisitDocument>(),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .Callback((
                UserVisitDocument document,
                InsertOneOptions _,
                CancellationToken _) => insertedDocument = document)
            .Returns(Task.CompletedTask);
        UserVisitRepository repository = CreateRepository(collection.Object);
        Visit visit = CreateDraftVisit();

        Visit persisted = await repository.CreateAsync(visit, CancellationToken.None);

        Assert.NotNull(insertedDocument);
        Assert.Equal("visit-1", insertedDocument.Id);
        Assert.Equal("user-1", insertedDocument.UserId);
        Assert.Equal(visit.Id, persisted.Id);
        Assert.Equal(visit.Version, persisted.Version);
        collection.VerifyAll();
    }

    [Fact]
    public async Task TryUpdateOwnedAsync_ShouldUseThePreviousVersionAsAWriteFence()
    {
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        FilterDefinition<UserVisitDocument>? capturedFilter = null;
        UserVisitDocument? replacement = null;
        collection.Setup(value => value.ReplaceOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UserVisitDocument>(),
                It.IsAny<ReplaceOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserVisitDocument> filter,
                UserVisitDocument document,
                ReplaceOptions options,
                CancellationToken _) =>
            {
                capturedFilter = filter;
                replacement = document;
                Assert.False(options.IsUpsert);
            })
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(1, 1, null));
        UserVisitRepository repository = CreateRepository(collection.Object);
        Visit visit = CreateDraftVisit();
        visit.UpdateDraft(
            visit.Date,
            visit.TimeZoneId,
            visit.ServiceDayConvention,
            "Titre corrigé",
            visit.PrivateNote,
            NowUtc.AddMinutes(1));

        bool updated = await repository.TryUpdateOwnedAsync(
            visit,
            1,
            CancellationToken.None);

        Assert.True(updated);
        Assert.NotNull(capturedFilter);
        BsonDocument rendered = Render(capturedFilter);
        Assert.Equal("visit-1", rendered["_id"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal(1, rendered["version"].AsInt64);
        Assert.Equal(2, replacement?.Version);
        collection.VerifyAll();
    }

    [Fact]
    public async Task TryUpdateOwnedAsync_ShouldReportAConflictWithoutUpserting()
    {
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.ReplaceOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UserVisitDocument>(),
                It.IsAny<ReplaceOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new ReplaceOneResult.Acknowledged(0, 0, null));
        UserVisitRepository repository = CreateRepository(collection.Object);
        Visit visit = CreateDraftVisit();
        visit.UpdateDraft(
            visit.Date,
            visit.TimeZoneId,
            visit.ServiceDayConvention,
            "Titre corrigé",
            visit.PrivateNote,
            NowUtc.AddMinutes(1));

        bool updated = await repository.TryUpdateOwnedAsync(
            visit,
            1,
            CancellationToken.None);

        Assert.False(updated);
        collection.VerifyAll();
    }

    [Fact]
    public async Task TryDeleteOwnedAsync_ShouldRequireOwnerAndCurrentVersion()
    {
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        FilterDefinition<UserVisitDocument>? capturedFilter = null;
        collection.Setup(value => value.DeleteOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserVisitDocument> filter,
                CancellationToken _) => capturedFilter = filter)
            .ReturnsAsync(new DeleteResult.Acknowledged(1));
        UserVisitRepository repository = CreateRepository(collection.Object);

        bool deleted = await repository.TryDeleteOwnedAsync(
            VisitId.Parse("visit-1"),
            "user-1",
            2,
            CancellationToken.None);

        Assert.True(deleted);
        Assert.NotNull(capturedFilter);
        BsonDocument rendered = Render(capturedFilter);
        Assert.Equal("visit-1", rendered["_id"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal(2, rendered["version"].AsInt64);
        collection.VerifyAll();
    }

    [Fact]
    public async Task ListOwnedAsync_ShouldRejectAnUnboundedRequestBeforeMongo()
    {
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        UserVisitRepository repository = CreateRepository(collection.Object);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.ListOwnedAsync(
                "user-1",
                UserVisitRepository.MaximumListSize + 1,
                CancellationToken.None));
    }

    private static UserVisitRepository CreateRepository(
        IMongoCollection<UserVisitDocument> collection)
    {
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<UserVisitDocument>("user-visits", null))
            .Returns(collection);
        return new UserVisitRepository(
            database.Object,
            new MongoDbSettings { UserVisitsCollectionName = "user-visits" });
    }

    private static Visit CreateDraftVisit()
    {
        return Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            "Titre",
            "Note privée",
            NowUtc);
    }

    private static BsonDocument Render(
        FilterDefinition<UserVisitDocument> filter)
    {
        IBsonSerializer<UserVisitDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserVisitDocument>();
        RenderArgs<UserVisitDocument> arguments =
            new RenderArgs<UserVisitDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }
}
