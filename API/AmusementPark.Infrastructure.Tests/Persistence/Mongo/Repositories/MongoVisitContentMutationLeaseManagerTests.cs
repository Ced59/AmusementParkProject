using AmusementPark.Application.Features.Passport.Ports;
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
        FilterDefinition<UserVisitDocument>? acquireFilter = null;
        UpdateDefinition<UserVisitDocument>? acquireUpdate = null;
        int acquisitionCount = 0;
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<UserVisitDocument, UserVisitDocument>>(),
                It.IsAny<CancellationToken>()))
            .Callback((
                FilterDefinition<UserVisitDocument> filter,
                UpdateDefinition<UserVisitDocument> update,
                FindOneAndUpdateOptions<UserVisitDocument, UserVisitDocument> _,
                CancellationToken _) =>
            {
                acquireFilter = filter;
                acquireUpdate = update;
            })
            .Returns(() => Task.FromResult<UserVisitDocument>(
                Interlocked.Increment(ref acquisitionCount) == 1
                    ? null!
                    : new UserVisitDocument
                    {
                        Id = "visit-1",
                        UserId = "user-1",
                        ContentMutationFenceToken = 7,
                        ContentMutationFenceStableToken = 6,
                    }));
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
        SetupContentCollections(database);
        MongoVisitContentMutationLeaseManager manager =
            new MongoVisitContentMutationLeaseManager(
                database.Object,
                new MongoDbSettings());

        IVisitContentMutationLease? lease = await manager.TryAcquireAsync(
            visit,
            NowUtc,
            CancellationToken.None);
        Assert.NotNull(lease);
        lease.MarkMutationCompleted();
        await lease.DisposeAsync();

        Assert.Equal(2, filters.Count);
        Assert.NotNull(acquireFilter);
        Assert.NotNull(acquireUpdate);
        BsonDocument renderedAcquireFilter = Render(acquireFilter);
        string renderedAcquireFilterJson = renderedAcquireFilter.ToJson();
        Assert.Contains("\"_id\" : \"visit-1\"", renderedAcquireFilterJson);
        Assert.Contains("\"userId\" : \"user-1\"", renderedAcquireFilterJson);
        Assert.Contains("\"version\"", renderedAcquireFilterJson);
        Assert.Contains("\"status\" : \"Draft\"", renderedAcquireFilterJson);
        BsonDocument renderedAcquireUpdate = Render(acquireUpdate);
        string token = renderedAcquireUpdate["$set"]["contentMutationLeaseToken"].AsString;
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(
            NowUtc.Add(MongoVisitContentMutationLeaseManager.LeaseDuration),
            renderedAcquireUpdate["$set"]["contentMutationLeaseExpiresAtUtc"].ToUniversalTime());
        Assert.Equal(1, renderedAcquireUpdate["$inc"]["contentMutationFenceToken"].AsInt64);
        Assert.False(renderedAcquireUpdate["$set"]["contentMutationFenceReady"].AsBoolean);

        BsonDocument promotionFilter = Render(filters[0]);
        Assert.Equal(7, promotionFilter["contentMutationFenceToken"].AsInt64);
        BsonDocument promotionUpdate = Render(updates[0]);
        Assert.True(promotionUpdate["$set"]["contentMutationFenceReady"].AsBoolean);
        Assert.Equal(
            7,
            promotionUpdate["$set"]["contentMutationFenceStableToken"].AsInt64);
        BsonDocument releaseFilter = Render(filters[1]);
        Assert.Equal(token, releaseFilter["contentMutationLeaseToken"].AsString);
        BsonDocument releaseUpdate = Render(updates[1]);
        Assert.True(releaseUpdate["$unset"].AsBsonDocument.Contains(
            "contentMutationLeaseToken"));
        Assert.True(releaseUpdate["$unset"].AsBsonDocument.Contains(
            "contentMutationLeaseExpiresAtUtc"));
        Assert.False(releaseUpdate.Contains("$set"));
        database.VerifyAll();
        collection.Verify(
            value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<UserVisitDocument, UserVisitDocument>>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        collection.Verify(
            value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task TryAcquireAsync_ShouldPromoteOnlyTheStableFenceBeforeReturning()
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
        List<UpdateDefinition<UserVisitDocument>> acquisitionUpdates =
            new List<UpdateDefinition<UserVisitDocument>>();
        int acquisitionCount = 0;
        Mock<IMongoCollection<UserVisitDocument>> visits =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        visits.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<UserVisitDocument, UserVisitDocument>>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserVisitDocument> _,
                UpdateDefinition<UserVisitDocument> update,
                FindOneAndUpdateOptions<UserVisitDocument, UserVisitDocument> _,
                CancellationToken _) => acquisitionUpdates.Add(update))
            .Returns(() => Task.FromResult<UserVisitDocument>(
                Interlocked.Increment(ref acquisitionCount) == 1
                    ? null!
                    : new UserVisitDocument
                    {
                        Id = "visit-1",
                        UserId = "user-1",
                        ContentMutationFenceToken = 8,
                        ContentMutationFenceStableToken = 7,
                    }));
        visits.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        List<FilterDefinition<UserRideOccurrenceDocument>> occurrenceFilters =
            new List<FilterDefinition<UserRideOccurrenceDocument>>();
        Mock<IMongoCollection<UserRideOccurrenceDocument>> occurrences =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        occurrences.Setup(value => value.UpdateManyAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserRideOccurrenceDocument> filter,
                UpdateDefinition<UserRideOccurrenceDocument> _,
                UpdateOptions _,
                CancellationToken _) => occurrenceFilters.Add(filter))
            .ReturnsAsync(new UpdateResult.Acknowledged(2, 2, null));
        List<FilterDefinition<UserRideOccurrenceCreationOperationDocument>> operationFilters =
            new List<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>();
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operations =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        operations.Setup(value => value.UpdateManyAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter,
                UpdateDefinition<UserRideOccurrenceCreationOperationDocument> _,
                UpdateOptions _,
                CancellationToken _) => operationFilters.Add(filter))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<UserVisitDocument>("user-visits", null))
            .Returns(visits.Object);
        database.Setup(value => value.GetCollection<UserRideOccurrenceDocument>(
                "user-ride-occurrences",
                null))
            .Returns(occurrences.Object);
        database.Setup(value =>
                value.GetCollection<UserRideOccurrenceCreationOperationDocument>(
                    "user-ride-occurrence-operations",
                    null))
            .Returns(operations.Object);
        MongoVisitContentMutationLeaseManager manager =
            new MongoVisitContentMutationLeaseManager(database.Object, new MongoDbSettings());

        IVisitContentMutationLease? lease = await manager.TryAcquireAsync(
            visit,
            NowUtc,
            CancellationToken.None);

        Assert.NotNull(lease);
        Assert.Equal(8, lease.ContentFenceToken);
        Assert.Equal(2, acquisitionUpdates.Count);
        Assert.False(Render(acquisitionUpdates[0]).Contains("$inc"));
        Assert.Equal(
            1,
            Render(acquisitionUpdates[1])["$inc"]["contentMutationFenceToken"].AsInt64);
        Assert.Single(occurrenceFilters);
        Assert.Single(operationFilters);
        Assert.Equal(
            7,
            Render(occurrenceFilters[0])["contentMutationFenceToken"]["$gte"].AsInt64);
        Assert.Equal(
            8,
            Render(occurrenceFilters[0])["contentMutationFenceToken"]["$lt"].AsInt64);
        Assert.Equal(
            7,
            Render(operationFilters[0])["contentMutationFenceToken"]["$gte"].AsInt64);
        Assert.Equal(
            8,
            Render(operationFilters[0])["contentMutationFenceToken"]["$lt"].AsInt64);
        lease.MarkMutationCompleted();
        await lease.DisposeAsync();
        visits.VerifyAll();
        occurrences.VerifyAll();
        operations.VerifyAll();
        database.VerifyAll();
    }

    [Fact]
    public async Task TryAcquireAsync_WhenFenceIsStable_ShouldReuseItWithoutRewritingHistory()
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
        UpdateDefinition<UserVisitDocument>? acquisitionUpdate = null;
        Mock<IMongoCollection<UserVisitDocument>> visits =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        visits.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<UserVisitDocument, UserVisitDocument>>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserVisitDocument> _,
                UpdateDefinition<UserVisitDocument> update,
                FindOneAndUpdateOptions<UserVisitDocument, UserVisitDocument> _,
                CancellationToken _) => acquisitionUpdate = update)
            .ReturnsAsync(new UserVisitDocument
            {
                Id = "visit-1",
                UserId = "user-1",
                ContentMutationFenceToken = 7,
                ContentMutationFenceStableToken = 7,
                ContentMutationFenceReady = true,
            });
        visits.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        Mock<IMongoCollection<UserRideOccurrenceDocument>> occurrences =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operations =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<UserVisitDocument>("user-visits", null))
            .Returns(visits.Object);
        database.Setup(value => value.GetCollection<UserRideOccurrenceDocument>(
                "user-ride-occurrences",
                null))
            .Returns(occurrences.Object);
        database.Setup(value =>
                value.GetCollection<UserRideOccurrenceCreationOperationDocument>(
                    "user-ride-occurrence-operations",
                    null))
            .Returns(operations.Object);
        MongoVisitContentMutationLeaseManager manager =
            new MongoVisitContentMutationLeaseManager(database.Object, new MongoDbSettings());

        IVisitContentMutationLease? lease = await manager.TryAcquireAsync(
            visit,
            NowUtc,
            CancellationToken.None);

        Assert.NotNull(lease);
        Assert.Equal(7, lease.ContentFenceToken);
        Assert.NotNull(acquisitionUpdate);
        Assert.False(Render(acquisitionUpdate).Contains("$inc"));
        lease.MarkMutationCompleted();
        await lease.DisposeAsync();
        visits.VerifyAll();
        occurrences.VerifyNoOtherCalls();
        operations.VerifyNoOtherCalls();
        database.VerifyAll();
    }

    [Fact]
    public async Task DisposeAsync_WhenMutationWasNotCompleted_ShouldDirtyTheFence()
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
        UpdateDefinition<UserVisitDocument>? releaseUpdate = null;
        Mock<IMongoCollection<UserVisitDocument>> visits =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        visits.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<UserVisitDocument, UserVisitDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(new UserVisitDocument
            {
                Id = "visit-1",
                UserId = "user-1",
                ContentMutationFenceToken = 7,
                ContentMutationFenceStableToken = 7,
                ContentMutationFenceReady = true,
            });
        visits.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserVisitDocument> _,
                UpdateDefinition<UserVisitDocument> update,
                UpdateOptions _,
                CancellationToken _) => releaseUpdate = update)
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<UserVisitDocument>("user-visits", null))
            .Returns(visits.Object);
        SetupContentCollections(database);
        MongoVisitContentMutationLeaseManager manager =
            new MongoVisitContentMutationLeaseManager(database.Object, new MongoDbSettings());

        IVisitContentMutationLease? lease = await manager.TryAcquireAsync(
            visit,
            NowUtc,
            CancellationToken.None);

        Assert.NotNull(lease);
        await lease.DisposeAsync();
        Assert.NotNull(releaseUpdate);
        BsonDocument renderedRelease = Render(releaseUpdate);
        Assert.False(renderedRelease["$set"]["contentMutationFenceReady"].AsBoolean);
        Assert.True(renderedRelease["$unset"].AsBsonDocument.Contains(
            "contentMutationLeaseToken"));
        visits.VerifyAll();
        database.VerifyAll();
    }

    [Fact]
    public async Task AcquiredLease_ShouldRenewItsExactUnexpiredTokenUntilDisposed()
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
        UpdateDefinition<UserVisitDocument>? acquireUpdate = null;
        TaskCompletionSource renewalObserved = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<UserVisitDocument, UserVisitDocument>>(),
                It.IsAny<CancellationToken>()))
            .Callback((
                FilterDefinition<UserVisitDocument> _,
                UpdateDefinition<UserVisitDocument> update,
                FindOneAndUpdateOptions<UserVisitDocument, UserVisitDocument> _,
                CancellationToken _) => acquireUpdate = update)
            .ReturnsAsync(new UserVisitDocument
            {
                Id = "visit-1",
                UserId = "user-1",
                ContentMutationFenceToken = 7,
                ContentMutationFenceStableToken = 7,
                ContentMutationFenceReady = true,
            });
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
                if (filters.Count == 1)
                {
                    renewalObserved.TrySetResult();
                }
            })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<UserVisitDocument>(
                "user-visits",
                null))
            .Returns(collection.Object);
        SetupContentCollections(database);
        MongoVisitContentMutationLeaseManager manager =
            new MongoVisitContentMutationLeaseManager(
                database.Object,
                new MongoDbSettings(),
                TimeProvider.System,
                TimeSpan.FromMilliseconds(20));

        IVisitContentMutationLease? lease = await manager.TryAcquireAsync(
            visit,
            NowUtc,
            CancellationToken.None);
        Assert.NotNull(lease);
        await renewalObserved.Task.WaitAsync(TimeSpan.FromSeconds(2));
        lease.MarkMutationCompleted();
        await lease.DisposeAsync();

        Assert.True(filters.Count >= 2);
        Assert.NotNull(acquireUpdate);
        BsonDocument renderedAcquireUpdate = Render(acquireUpdate);
        string token = renderedAcquireUpdate["$set"]["contentMutationLeaseToken"].AsString;
        Assert.False(renderedAcquireUpdate.Contains("$inc"));
        BsonDocument renewalFilter = Render(filters[0]);
        Assert.Equal(token, renewalFilter["contentMutationLeaseToken"].AsString);
        Assert.True(renewalFilter["contentMutationLeaseExpiresAtUtc"].AsBsonDocument.Contains("$gt"));
        BsonDocument renewalUpdate = Render(updates[0]);
        Assert.True(renewalUpdate["$set"].AsBsonDocument.Contains(
            "contentMutationLeaseExpiresAtUtc"));
        BsonDocument releaseFilter = Render(filters[^1]);
        Assert.Equal(token, releaseFilter["contentMutationLeaseToken"].AsString);
    }

    [Fact]
    public async Task AcquiredLease_WhenRenewalLosesOwnership_ShouldCancelProtectedWork()
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
        int updateCount = 0;
        Mock<IMongoCollection<UserVisitDocument>> collection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        collection.Setup(value => value.FindOneAndUpdateAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<FindOneAndUpdateOptions<UserVisitDocument, UserVisitDocument>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserVisitDocument
            {
                Id = "visit-1",
                UserId = "user-1",
                ContentMutationFenceToken = 7,
                ContentMutationFenceStableToken = 7,
                ContentMutationFenceReady = true,
            });
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateDefinition<UserVisitDocument>>(),
                It.IsAny<UpdateOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Interlocked.Increment(ref updateCount) == 1
                ? new UpdateResult.Acknowledged(0, 0, null)
                : new UpdateResult.Acknowledged(1, 1, null));
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<UserVisitDocument>(
                "user-visits",
                null))
            .Returns(collection.Object);
        SetupContentCollections(database);
        MongoVisitContentMutationLeaseManager manager =
            new MongoVisitContentMutationLeaseManager(
                database.Object,
                new MongoDbSettings(),
                TimeProvider.System,
                TimeSpan.FromMilliseconds(20));

        IVisitContentMutationLease? lease = await manager.TryAcquireAsync(
            visit,
            NowUtc,
            CancellationToken.None);
        Assert.NotNull(lease);
        CancellationToken leaseLostToken = lease.LeaseLostToken;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Task.Delay(Timeout.InfiniteTimeSpan, leaseLostToken)
                .WaitAsync(TimeSpan.FromSeconds(2)));
        await lease.DisposeAsync();

        Assert.True(leaseLostToken.IsCancellationRequested);
    }

    private static BsonDocument Render<TDocument>(FilterDefinition<TDocument> filter)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        return filter.Render(new RenderArgs<TDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }

    private static void SetupContentCollections(Mock<IMongoDatabase> database)
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> occurrences =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>();
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operations =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>();
        database.Setup(value => value.GetCollection<UserRideOccurrenceDocument>(
                "user-ride-occurrences",
                null))
            .Returns(occurrences.Object);
        database.Setup(value =>
                value.GetCollection<UserRideOccurrenceCreationOperationDocument>(
                    "user-ride-occurrence-operations",
                    null))
            .Returns(operations.Object);
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
