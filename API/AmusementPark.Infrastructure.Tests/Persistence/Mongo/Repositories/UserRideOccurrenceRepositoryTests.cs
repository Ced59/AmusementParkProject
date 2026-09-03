using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserRideOccurrenceRepositoryTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateBatchIdempotentAsync_ShouldInsertOneDocumentPerOccurrence()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        IReadOnlyCollection<UserRideOccurrenceDocument>? inserted = null;
        operationCollection.Setup(value => value.InsertOneAsync(
                It.IsAny<UserRideOccurrenceCreationOperationDocument>(),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .Callback((
                UserRideOccurrenceCreationOperationDocument document,
                InsertOneOptions _,
                CancellationToken _) =>
            {
                Assert.Equal("user-1", document.UserId);
                Assert.Equal(64, document.OperationKeyHash.Length);
                Assert.Equal(64, document.PayloadHash.Length);
                Assert.Equal(3, document.Items.Count);
            })
            .Returns(Task.CompletedTask);
        collection.Setup(value => value.InsertManyAsync(
                It.IsAny<IEnumerable<UserRideOccurrenceDocument>>(),
                It.IsAny<InsertManyOptions>(),
                CancellationToken.None))
            .Callback((
                IEnumerable<UserRideOccurrenceDocument> documents,
                InsertManyOptions options,
                CancellationToken _) =>
            {
                inserted = documents.ToArray();
                Assert.False(options.IsOrdered);
            })
            .Returns(Task.CompletedTask);
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);
        IReadOnlyList<RideOccurrence> occurrences = new[]
        {
            CreateOccurrence("occurrence-1", "item-1", 1024),
            CreateOccurrence("occurrence-2", "item-1", 2048),
            CreateOccurrence("occurrence-3", "item-2", 3072),
        };

        IdempotentRideOccurrenceCreationResult result =
            await repository.CreateBatchIdempotentAsync(
                occurrences,
                " request-1 ",
                CancellationToken.None);

        Assert.NotNull(inserted);
        Assert.Equal(3, inserted.Count);
        Assert.All(inserted, static document => Assert.Equal(64, document.CreationOperationKeyHash?.Length));
        Assert.All(inserted, static document => Assert.Equal(64, document.CreationPayloadHash?.Length));
        Assert.Equal(new int?[] { 0, 1, 2 }, inserted.Select(static document => document.CreationOperationIndex));
        Assert.All(inserted, static document => Assert.Equal(3, document.CreationOperationCount));
        Assert.All(inserted, static document => Assert.NotNull(document.CreationSnapshot));
        Assert.Equal(IdempotentRideOccurrenceCreationStatus.Created, result.Status);
        Assert.Equal(3, result.Occurrences.Count);
        collection.VerifyAll();
        operationCollection.VerifyAll();
    }

    [Fact]
    public void ResolveIdempotentBatchCreation_WithACompleteMatchingBatch_ShouldReplaySnapshots()
    {
        IReadOnlyList<RideOccurrence> occurrences = new[]
        {
            CreateOccurrence("occurrence-1", "item-1", 1024),
            CreateOccurrence("occurrence-2", "item-2", 2048),
        };
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(occurrences);
        List<UserRideOccurrenceDocument> documents = occurrences
            .Select((occurrence, index) => CreateCreationDocument(
                occurrence,
                payloadHash,
                index,
                occurrences.Count))
            .ToList();
        documents[0].Status = RideOccurrenceStatus.MissedClosed;
        documents[0].Version = 2;

        IdempotentRideOccurrenceCreationResult? result =
            UserRideOccurrenceRepository.ResolveIdempotentBatchCreation(
                documents,
                payloadHash,
                2);

        Assert.NotNull(result);
        Assert.Equal(IdempotentRideOccurrenceCreationStatus.Replayed, result.Status);
        Assert.Equal(2, result.Occurrences.Count);
        Assert.All(result.Occurrences, static occurrence =>
            Assert.Equal(RideOccurrenceStatus.Completed, occurrence.Status));
        Assert.All(result.Occurrences, static occurrence => Assert.Equal(1, occurrence.Version));
    }

    [Fact]
    public void ResolveIdempotentBatchCreation_WithAPartialMatchingBatch_ShouldAllowRecovery()
    {
        IReadOnlyList<RideOccurrence> occurrences = new[]
        {
            CreateOccurrence("occurrence-1", "item-1", 1024),
            CreateOccurrence("occurrence-2", "item-2", 2048),
        };
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(occurrences);
        UserRideOccurrenceDocument first = CreateCreationDocument(
            occurrences[0],
            payloadHash,
            0,
            2);

        IdempotentRideOccurrenceCreationResult? result =
            UserRideOccurrenceRepository.ResolveIdempotentBatchCreation(
                new[] { first },
                payloadHash,
                2);

        Assert.Null(result);
    }

    [Fact]
    public void ResolveIdempotentBatchCreation_WithAnotherPayload_ShouldConflict()
    {
        RideOccurrence occurrence = CreateOccurrence("occurrence-1", "item-1", 1024);
        UserRideOccurrenceDocument document = CreateCreationDocument(
            occurrence,
            "first-payload",
            0,
            1);

        IdempotentRideOccurrenceCreationResult? result =
            UserRideOccurrenceRepository.ResolveIdempotentBatchCreation(
                new[] { document },
                "second-payload",
                1);

        Assert.NotNull(result);
        Assert.Equal(IdempotentRideOccurrenceCreationStatus.Conflict, result.Status);
        Assert.Empty(result.Occurrences);
    }

    [Fact]
    public void ResolveAgainstOperation_WithAnAllocationMismatch_ShouldConflict()
    {
        RideOccurrence occurrence = CreateOccurrence("occurrence-1", "item-1", 1024);
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(
            new[] { occurrence });
        UserRideOccurrenceDocument document = CreateCreationDocument(
            occurrence,
            payloadHash,
            0,
            1);
        UserRideOccurrenceCreationOperationDocument operation =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                OperationKeyHash = "operation-hash",
                PayloadHash = payloadHash,
                Items = new List<UserRideOccurrenceCreationAllocationDocument>
                {
                    new UserRideOccurrenceCreationAllocationDocument
                    {
                        Index = 0,
                        OccurrenceId = "another-occurrence",
                        SortPosition = 1024,
                        CreatedAtUtc = NowUtc,
                        UpdatedAtUtc = NowUtc,
                    },
                },
            };

        IdempotentRideOccurrenceCreationResult? result =
            UserRideOccurrenceRepository.ResolveAgainstOperation(
                operation,
                new[] { document },
                payloadHash,
                1);

        Assert.NotNull(result);
        Assert.Equal(IdempotentRideOccurrenceCreationStatus.Conflict, result.Status);
    }

    [Fact]
    public void ResolveAgainstOperation_AfterALiveOccurrenceMutation_ShouldReplayTheSnapshot()
    {
        RideOccurrence occurrence = CreateOccurrence("occurrence-1", "item-1", 1024);
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(
            new[] { occurrence });
        UserRideOccurrenceDocument document = CreateCreationDocument(
            occurrence,
            payloadHash,
            0,
            1);
        document.SortPosition = 4096;
        document.Status = RideOccurrenceStatus.MissedClosed;
        document.Version = 3;
        document.UpdatedAt = NowUtc.AddHours(2);
        document.DeletedAtUtc = NowUtc.AddHours(2);
        UserRideOccurrenceCreationOperationDocument operation =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                OperationKeyHash = "operation-hash",
                PayloadHash = payloadHash,
                Items = new List<UserRideOccurrenceCreationAllocationDocument>
                {
                    new UserRideOccurrenceCreationAllocationDocument
                    {
                        Index = 0,
                        OccurrenceId = "occurrence-1",
                        SortPosition = 1024,
                        CreatedAtUtc = NowUtc,
                        UpdatedAtUtc = NowUtc,
                    },
                },
            };

        IdempotentRideOccurrenceCreationResult? result =
            UserRideOccurrenceRepository.ResolveAgainstOperation(
                operation,
                new[] { document },
                payloadHash,
                1);

        Assert.NotNull(result);
        Assert.Equal(IdempotentRideOccurrenceCreationStatus.Replayed, result.Status);
        RideOccurrence replayed = Assert.Single(result.Occurrences);
        Assert.Equal(1024, replayed.SortPosition);
        Assert.Equal(RideOccurrenceStatus.Completed, replayed.Status);
        Assert.Equal(1, replayed.Version);
        Assert.False(replayed.IsDeleted);
        Assert.Equal(NowUtc, replayed.UpdatedAtUtc);
    }

    [Fact]
    public void CreateCreationDocument_ShouldReuseTheReservedOriginalTimestamps()
    {
        RideOccurrence retryOccurrence = CreateOccurrence(
            "retry-occurrence",
            "item-1",
            4096,
            NowUtc.AddHours(2));
        UserRideOccurrenceCreationAllocationDocument originalAllocation =
            new UserRideOccurrenceCreationAllocationDocument
            {
                Index = 0,
                OccurrenceId = "original-occurrence",
                SortPosition = 1024,
                CreatedAtUtc = NowUtc,
                UpdatedAtUtc = NowUtc,
            };

        UserRideOccurrenceDocument document =
            UserRideOccurrenceRepository.CreateCreationDocument(
                retryOccurrence,
                originalAllocation,
                "operation-hash",
                "payload-hash",
                1);

        Assert.Equal("original-occurrence", document.Id);
        Assert.Equal(1024, document.SortPosition);
        Assert.Equal(NowUtc, document.CreatedAt);
        Assert.Equal(NowUtc, document.UpdatedAt);
        Assert.Equal(NowUtc, document.CreationSnapshot?.CreatedAtUtc);
        Assert.Equal(NowUtc, document.CreationSnapshot?.UpdatedAtUtc);
    }

    [Fact]
    public async Task CreateBatchIdempotentAsync_WithDuplicatePositions_ShouldFailBeforeMongo()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);
        IReadOnlyList<RideOccurrence> occurrences = new[]
        {
            CreateOccurrence("occurrence-1", "item-1", 1024),
            CreateOccurrence("occurrence-2", "item-2", 1024),
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.CreateBatchIdempotentAsync(
                occurrences,
                "request-1",
                CancellationToken.None));
    }

    [Fact]
    public async Task TryUpdateOwnedAsync_ShouldFenceTheWriteWithoutReplacingIdempotencyData()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        FilterDefinition<UserRideOccurrenceDocument>? capturedFilter = null;
        UpdateDefinition<UserRideOccurrenceDocument>? capturedUpdate = null;
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserRideOccurrenceDocument> filter,
                UpdateDefinition<UserRideOccurrenceDocument> update,
                UpdateOptions options,
                CancellationToken _) =>
            {
                capturedFilter = filter;
                capturedUpdate = update;
                Assert.False(options.IsUpsert);
            })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);
        RideOccurrence occurrence = CreateOccurrence("occurrence-1", "item-1", 1024);
        occurrence.MoveTo(2048, NowUtc.AddMinutes(1));

        bool updated = await repository.TryUpdateOwnedAsync(
            occurrence,
            1,
            CancellationToken.None);

        Assert.True(updated);
        Assert.NotNull(capturedFilter);
        BsonDocument filter = Render(capturedFilter);
        Assert.Equal("occurrence-1", filter["_id"].AsString);
        Assert.Equal("visit-1", filter["visitId"].AsString);
        Assert.Equal("user-1", filter["userId"].AsString);
        Assert.Equal(1, filter["version"].AsInt64);
        Assert.NotNull(capturedUpdate);
        BsonDocument update = Render(capturedUpdate);
        Assert.Equal(2048, update["$set"]["sortPosition"].AsInt64);
        Assert.False(update.ToString().Contains("creationOperationKeyHash", StringComparison.Ordinal));
        Assert.False(update.ToString().Contains("creationSnapshot", StringComparison.Ordinal));
        collection.VerifyAll();
    }

    private static UserRideOccurrenceDocument CreateCreationDocument(
        RideOccurrence occurrence,
        string payloadHash,
        int operationIndex,
        int operationCount)
    {
        UserRideOccurrenceDocument document = occurrence.ToDocument();
        document.CreationPayloadHash = payloadHash;
        document.CreationOperationIndex = operationIndex;
        document.CreationOperationCount = operationCount;
        document.CreationSnapshot = document.CreateCreationSnapshot();
        return document;
    }

    private static UserRideOccurrenceRepository CreateRepository(
        IMongoCollection<UserRideOccurrenceDocument> collection,
        IMongoCollection<UserRideOccurrenceCreationOperationDocument> operationCollection)
    {
        Mock<IMongoDatabase> database = new Mock<IMongoDatabase>(MockBehavior.Strict);
        database.Setup(value => value.GetCollection<UserRideOccurrenceDocument>(
                "user-ride-occurrences",
                null))
            .Returns(collection);
        database.Setup(value => value.GetCollection<UserRideOccurrenceCreationOperationDocument>(
                "user-ride-occurrence-operations",
                null))
            .Returns(operationCollection);
        return new UserRideOccurrenceRepository(
            database.Object,
            new MongoDbSettings
            {
                UserRideOccurrencesCollectionName = "user-ride-occurrences",
                UserRideOccurrenceOperationsCollectionName =
                    "user-ride-occurrence-operations",
            });
    }

    private static RideOccurrence CreateOccurrence(
        string id,
        string parkItemId,
        long sortPosition)
    {
        return CreateOccurrence(id, parkItemId, sortPosition, NowUtc);
    }

    private static RideOccurrence CreateOccurrence(
        string id,
        string parkItemId,
        long sortPosition,
        DateTime nowUtc)
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
            nowUtc);
        return RideOccurrence.Create(
            RideOccurrenceId.Parse(id),
            visit,
            parkItemId,
            sortPosition,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            null,
            nowUtc);
    }

    private static BsonDocument Render(
        FilterDefinition<UserRideOccurrenceDocument> filter)
    {
        IBsonSerializer<UserRideOccurrenceDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserRideOccurrenceDocument>();
        RenderArgs<UserRideOccurrenceDocument> arguments =
            new RenderArgs<UserRideOccurrenceDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }

    private static BsonDocument Render(
        UpdateDefinition<UserRideOccurrenceDocument> update)
    {
        IBsonSerializer<UserRideOccurrenceDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<UserRideOccurrenceDocument>();
        RenderArgs<UserRideOccurrenceDocument> arguments =
            new RenderArgs<UserRideOccurrenceDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return update.Render(arguments).AsBsonDocument;
    }
}
