using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
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
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> appendBaseCursor =
            CreateAsyncCursor(Array.Empty<UserRideOccurrenceDocument>());
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
                Assert.Equal("visit-1", document.VisitId);
                Assert.Equal("pending", document.OperationState);
                Assert.True(document.AppendBaseWasEmpty);
                Assert.True(document.WasNormalized);
                Assert.Equal(3, document.Items.Count);
                Assert.All(document.Items, static item => Assert.NotNull(item.CreationSnapshot));
                Assert.All(document.Items, static item =>
                {
                    Assert.Equal(item.CreatedAtUtc, item.CreationSnapshot.CreatedAtUtc);
                    Assert.Equal(item.UpdatedAtUtc, item.CreationSnapshot.UpdatedAtUtc);
                });
            })
            .Returns(Task.CompletedTask);
        operationCollection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument, UserRideOccurrenceDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(appendBaseCursor.Object);
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
            CreateOccurrence("occurrence-1", "item-1", 1024, NowUtc.AddTicks(1234)),
            CreateOccurrence("occurrence-2", "item-1", 2048),
            CreateOccurrence("occurrence-3", "item-2", 3072),
        };

        IdempotentRideOccurrenceCreationResult result =
            await repository.CreateBatchIdempotentAsync(
                CreateRequest(occurrences),
                occurrences,
                null,
                true,
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
        Assert.True(result.WasNormalized);
        collection.VerifyAll();
        operationCollection.VerifyAll();
        appendBaseCursor.VerifyAll();
    }

    [Fact]
    public async Task CreateBatchIdempotentAsync_WithAStaleAppendBase_ShouldReleaseForRetry()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        RideOccurrence occurrence = CreateOccurrence("occurrence-1", "item-1", 2048);
        UserRideOccurrenceDocument concurrentLast = CreateOccurrence(
                "concurrent-occurrence",
                "item-2",
                3072)
            .ToDocument();
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> appendBaseCursor =
            CreateAsyncCursor(new[] { concurrentLast });
        UserRideOccurrenceCreationOperationDocument? reserved = null;
        operationCollection.Setup(value => value.InsertOneAsync(
                It.IsAny<UserRideOccurrenceCreationOperationDocument>(),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .Callback((
                UserRideOccurrenceCreationOperationDocument document,
                InsertOneOptions _,
                CancellationToken _) => reserved = document)
            .Returns(Task.CompletedTask);
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument, UserRideOccurrenceDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(appendBaseCursor.Object);
        operationCollection.Setup(value => value.DeleteOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(new DeleteResult.Acknowledged(1));
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);

        IdempotentRideOccurrenceCreationResult result =
            await repository.CreateBatchIdempotentAsync(
                CreateRequest(new[] { occurrence }),
                new[] { occurrence },
                1024,
                false,
                "request-1",
                CancellationToken.None);

        Assert.Equal(IdempotentRideOccurrenceCreationStatus.ConcurrencyConflict, result.Status);
        Assert.NotNull(reserved);
        Assert.Equal(1024, reserved.AppendBaseSortPosition);
        Assert.False(reserved.AppendBaseWasEmpty);
        Assert.False(reserved.AppendBaseValidated);
        collection.VerifyAll();
        operationCollection.VerifyAll();
        appendBaseCursor.VerifyAll();
    }

    [Fact]
    public void ResolveIdempotentBatchCreation_WithACompleteMatchingBatch_ShouldReplaySnapshots()
    {
        IReadOnlyList<RideOccurrence> occurrences = new[]
        {
            CreateOccurrence("occurrence-1", "item-1", 1024),
            CreateOccurrence("occurrence-2", "item-2", 2048),
        };
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(
            CreateRequest(occurrences));
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
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(
            CreateRequest(occurrences));
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
    public async Task ResolveExistingBatchCreationAsync_WithAPartialBatch_ShouldRecoverFromReservedSnapshots()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        IReadOnlyList<RideOccurrence> occurrences = new[]
        {
            CreateOccurrence("occurrence-1", "item-1", 1024),
            CreateOccurrence("occurrence-2", "item-2", 2048),
        };
        RideOccurrenceCreationRequest request = new RideOccurrenceCreationRequest(
            VisitId.Parse("visit-1"),
            "user-1",
            occurrences.Select(static occurrence => new RideOccurrenceCreationRequestItem(
                occurrence.ParkItemId,
                occurrence.Moment,
                occurrence.Status,
                occurrence.Source,
                occurrence.PrivateNote,
                false)).ToArray());
        string operationKeyHash =
            UserRideOccurrenceCreationFingerprint.HashOperationKey("request-1");
        string payloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(request);
        List<UserRideOccurrenceCreationAllocationDocument> allocations = occurrences
            .Select((occurrence, index) =>
            {
                UserRideOccurrenceDocument document = occurrence.ToDocument();
                return new UserRideOccurrenceCreationAllocationDocument
                {
                    Index = index,
                    OccurrenceId = occurrence.Id.Value,
                    SortPosition = occurrence.SortPosition,
                    CreatedAtUtc = occurrence.CreatedAtUtc,
                    UpdatedAtUtc = occurrence.UpdatedAtUtc,
                    CreationSnapshot = document.CreateCreationSnapshot(),
                };
            })
            .ToList();
        UserRideOccurrenceCreationOperationDocument operation =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                OperationKeyHash = operationKeyHash,
                PayloadHash = payloadHash,
                OperationKind = "creation",
                VisitId = "visit-1",
                OperationState = "pending",
                AppendBaseWasEmpty = true,
                AppendBaseValidated = true,
                Items = allocations,
            };
        UserRideOccurrenceDocument existing =
            UserRideOccurrenceRepository.CreateCreationDocument(
                occurrences[0],
                allocations[0],
                operationKeyHash,
                payloadHash,
                2);
        UserRideOccurrenceDocument? inserted = null;
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> operationCursor =
            CreateAsyncCursor(new[] { operation });
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> firstDocumentsCursor =
            CreateAsyncCursor(new[] { existing });
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> recoveredDocumentsCursor =
            CreateAsyncCursor(new[] { existing }, () => inserted is null
                ? Array.Empty<UserRideOccurrenceDocument>()
                : new[] { inserted });
        operationCollection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceCreationOperationDocument,
                    UserRideOccurrenceCreationOperationDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(operationCursor.Object);
        operationCollection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        collection.SetupSequence(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument, UserRideOccurrenceDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(firstDocumentsCursor.Object)
            .ReturnsAsync(recoveredDocumentsCursor.Object);
        collection.Setup(value => value.InsertManyAsync(
                It.IsAny<IEnumerable<UserRideOccurrenceDocument>>(),
                It.IsAny<InsertManyOptions>(),
                CancellationToken.None))
            .Callback((
                IEnumerable<UserRideOccurrenceDocument> documents,
                InsertManyOptions _,
                CancellationToken _) => inserted = Assert.Single(documents))
            .Returns(Task.CompletedTask);
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);

        IdempotentRideOccurrenceCreationResult? result =
            await repository.ResolveExistingBatchCreationAsync(
                request,
                "request-1",
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(IdempotentRideOccurrenceCreationStatus.Replayed, result.Status);
        Assert.Equal(2, result.Occurrences.Count);
        Assert.Equal("occurrence-2", inserted?.Id);
        Assert.Equal("item-2", inserted?.ParkItemId);
        collection.VerifyAll();
        operationCollection.VerifyAll();
    }

    [Fact]
    public async Task ResolveExistingBatchCreationAsync_WithChangedConfirmation_ShouldConflict()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        RideOccurrence occurrence = CreateOccurrence("occurrence-1", "item-1", 1024);
        RideOccurrenceCreationRequest confirmed = CreateRequest(
            new[] { occurrence },
            confirmHistoricalConflict: true);
        RideOccurrenceCreationRequest unconfirmed = CreateRequest(
            new[] { occurrence },
            confirmHistoricalConflict: false);
        UserRideOccurrenceCreationOperationDocument operation =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                OperationKeyHash = UserRideOccurrenceCreationFingerprint.HashOperationKey(
                    "request-1"),
                PayloadHash = UserRideOccurrenceCreationFingerprint.HashPayload(confirmed),
                OperationKind = "creation",
                VisitId = "visit-1",
                OperationState = "completed",
                AppendBaseWasEmpty = true,
            };
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> operationCursor =
            CreateAsyncCursor(new[] { operation });
        operationCollection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceCreationOperationDocument,
                    UserRideOccurrenceCreationOperationDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(operationCursor.Object);
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);

        IdempotentRideOccurrenceCreationResult? result =
            await repository.ResolveExistingBatchCreationAsync(
                unconfirmed,
                "request-1",
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(IdempotentRideOccurrenceCreationStatus.Conflict, result.Status);
        Assert.Empty(result.Occurrences);
        operationCollection.VerifyAll();
        collection.VerifyNoOtherCalls();
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
            CreateRequest(new[] { occurrence }));
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
                OperationKind = "creation",
                VisitId = "visit-1",
                OperationState = "pending",
                AppendBaseWasEmpty = true,
                Items = new List<UserRideOccurrenceCreationAllocationDocument>
                {
                    new UserRideOccurrenceCreationAllocationDocument
                    {
                        Index = 0,
                        OccurrenceId = "another-occurrence",
                        SortPosition = 1024,
                        CreatedAtUtc = NowUtc,
                        UpdatedAtUtc = NowUtc,
                        CreationSnapshot = document.CreationSnapshot!,
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
            CreateRequest(new[] { occurrence }));
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
                OperationKind = "creation",
                VisitId = "visit-1",
                OperationState = "completed",
                AppendBaseWasEmpty = true,
                AppendBaseValidated = true,
                WasNormalized = true,
                Items = new List<UserRideOccurrenceCreationAllocationDocument>
                {
                    new UserRideOccurrenceCreationAllocationDocument
                    {
                        Index = 0,
                        OccurrenceId = "occurrence-1",
                        SortPosition = 1024,
                        CreatedAtUtc = NowUtc,
                        UpdatedAtUtc = NowUtc,
                        CreationSnapshot = document.CreationSnapshot!,
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
        Assert.True(result.WasNormalized);
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
            "item-changed-after-reservation",
            4096,
            NowUtc.AddHours(2));
        UserRideOccurrenceCreationSnapshotDocument reservedSnapshot =
            CreateOccurrence("original-occurrence", "item-1", 1024)
                .ToDocument()
                .CreateCreationSnapshot();
        UserRideOccurrenceCreationAllocationDocument originalAllocation =
            new UserRideOccurrenceCreationAllocationDocument
            {
                Index = 0,
                OccurrenceId = "original-occurrence",
                SortPosition = 1024,
                CreatedAtUtc = NowUtc,
                UpdatedAtUtc = NowUtc,
                CreationSnapshot = reservedSnapshot,
            };

        UserRideOccurrenceDocument document =
            UserRideOccurrenceRepository.CreateCreationDocument(
                retryOccurrence,
                originalAllocation,
                "operation-hash",
                "payload-hash",
                1);

        Assert.Equal("original-occurrence", document.Id);
        Assert.Equal("item-1", document.ParkItemId);
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
                CreateRequest(occurrences),
                occurrences,
                null,
                false,
                "request-1",
                CancellationToken.None));
    }

    [Fact]
    public async Task GetAppendStateAsync_WithCompletedRelatedNormalization_ShouldRestoreTheSignal()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        UserRideOccurrenceDocument last = CreateOccurrence(
                "occurrence-1",
                "item-1",
                2048)
            .ToDocument();
        UserRideOccurrenceCreationOperationDocument normalization =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                VisitId = "visit-1",
                OperationKind = "reorder",
                OperationState = "completed",
                WasNormalized = true,
                RelatedCreationOperationKeyHash =
                    UserRideOccurrenceCreationFingerprint.HashOperationKey("request-1"),
            };
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> lastCursor =
            CreateAsyncCursor(new[] { last });
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> normalizationCursor =
            CreateAsyncCursor(new[] { normalization });
        FilterDefinition<UserRideOccurrenceCreationOperationDocument>? capturedFilter = null;
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument, UserRideOccurrenceDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(lastCursor.Object);
        operationCollection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceCreationOperationDocument,
                    UserRideOccurrenceCreationOperationDocument>>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter,
                FindOptions<UserRideOccurrenceCreationOperationDocument,
                    UserRideOccurrenceCreationOperationDocument> _,
                CancellationToken _) => capturedFilter = filter)
            .ReturnsAsync(normalizationCursor.Object);
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);

        RideOccurrenceAppendState state = await repository.GetAppendStateAsync(
            VisitId.Parse("visit-1"),
            " user-1 ",
            " request-1 ",
            CancellationToken.None);

        Assert.Equal(2048, state.LastSortPosition);
        Assert.True(state.WasNormalizedForOperation);
        Assert.NotNull(capturedFilter);
        BsonDocument rendered = Render(capturedFilter);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal("visit-1", rendered["visitId"].AsString);
        Assert.Equal(64, rendered["relatedCreationOperationKeyHash"].AsString.Length);
        Assert.Equal("completed", rendered["operationState"].AsString);
        collection.VerifyAll();
        operationCollection.VerifyAll();
        lastCursor.VerifyAll();
        normalizationCursor.VerifyAll();
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

    [Fact]
    public async Task TryConfirmOwnedVersionAsync_ShouldMatchWithoutIncrementingTheVersion()
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
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 0, null));
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);

        bool confirmed = await repository.TryConfirmOwnedVersionAsync(
            RideOccurrenceId.Parse("occurrence-1"),
            VisitId.Parse("visit-1"),
            "user-1",
            3,
            CancellationToken.None);

        Assert.True(confirmed);
        Assert.NotNull(capturedFilter);
        BsonDocument filter = Render(capturedFilter);
        Assert.Equal(3, filter["version"].AsInt64);
        Assert.NotNull(capturedUpdate);
        BsonDocument update = Render(capturedUpdate);
        Assert.Equal(3, update["$set"]["version"].AsInt64);
        Assert.Single(update["$set"].AsBsonDocument);
        collection.VerifyAll();
        operationCollection.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryDeleteOwnedAsync_ShouldReserveTheVisitBeforeApplyingTheTombstone()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        MockSequence sequence = new MockSequence();
        UserRideOccurrenceCreationOperationDocument? reserved = null;
        FilterDefinition<UserRideOccurrenceDocument>? capturedFilter = null;
        UpdateDefinition<UserRideOccurrenceDocument>? capturedUpdate = null;
        operationCollection.InSequence(sequence).Setup(value => value.InsertOneAsync(
                It.IsAny<UserRideOccurrenceCreationOperationDocument>(),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .Callback((
                UserRideOccurrenceCreationOperationDocument operation,
                InsertOneOptions _,
                CancellationToken _) => reserved = operation)
            .Returns(Task.CompletedTask);
        collection.InSequence(sequence).Setup(value => value.UpdateOneAsync(
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
        operationCollection.InSequence(sequence).Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);
        RideOccurrence occurrence = CreateOccurrence("occurrence-1", "item-1", 1024);
        occurrence.Delete(NowUtc.AddMinutes(1));

        bool deleted = await repository.TryDeleteOwnedAsync(
            occurrence,
            1,
            CancellationToken.None);

        Assert.True(deleted);
        Assert.NotNull(reserved);
        Assert.Equal("delete", reserved.OperationKind);
        Assert.Equal("visit-1", reserved.VisitId);
        Assert.Equal("occurrence-1", reserved.DeleteOccurrenceId);
        Assert.Equal(1, reserved.DeleteExpectedVersion);
        Assert.Equal(NowUtc.AddMinutes(1), reserved.DeleteAtUtc);
        Assert.Equal("completed", reserved.OperationState);
        Assert.NotNull(capturedFilter);
        BsonDocument filter = Render(capturedFilter);
        Assert.Equal("occurrence-1", filter["_id"].AsString);
        Assert.Equal(1, filter["version"].AsInt64);
        Assert.True(filter.Contains("deletedAtUtc"));
        Assert.NotNull(capturedUpdate);
        BsonDocument update = Render(capturedUpdate);
        Assert.Equal(2, update["$set"]["version"].AsInt64);
        Assert.Equal(
            64,
            update["$set"]["lastDeleteOperationKeyHash"].AsString.Length);
        collection.VerifyAll();
        operationCollection.VerifyAll();
    }

    [Fact]
    public async Task TryDeleteOwnedAsync_WithAbandonedDelete_ShouldRecoverItBeforeRetrying()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        string abandonedKeyHash =
            UserRideOccurrenceCreationFingerprint.HashOperationKey("abandoned-delete");
        UserRideOccurrenceCreationOperationDocument abandoned =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                OperationKeyHash = abandonedKeyHash,
                PayloadHash = abandonedKeyHash,
                OperationKind = "delete",
                VisitId = "visit-1",
                OperationState = "pending",
                DeleteOccurrenceId = "occurrence-1",
                DeleteExpectedVersion = 1,
                DeleteAtUtc = NowUtc.AddMinutes(1),
                CreatedAt = NowUtc.AddMinutes(1),
                UpdatedAt = NowUtc.AddMinutes(1),
            };
        RideOccurrence currentOccurrence =
            CreateOccurrence("occurrence-1", "item-1", 1024);
        currentOccurrence.Delete(NowUtc.AddMinutes(1));
        UserRideOccurrenceDocument current = currentOccurrence.ToDocument();
        current.LastDeleteOperationKeyHash = abandonedKeyHash;
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> pendingCursor =
            CreateAsyncCursor(new[] { abandoned });
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> releasedCursor =
            CreateAsyncCursor(Array.Empty<UserRideOccurrenceCreationOperationDocument>());
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> currentCursor =
            CreateAsyncCursor(new[] { current });
        UserRideOccurrenceCreationOperationDocument? retryOperation = null;
        int insertionAttempt = 0;
        operationCollection.Setup(value => value.InsertOneAsync(
                It.IsAny<UserRideOccurrenceCreationOperationDocument>(),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .Callback((
                UserRideOccurrenceCreationOperationDocument operation,
                InsertOneOptions _,
                CancellationToken _) =>
            {
                insertionAttempt++;
                if (insertionAttempt == 1)
                {
                    throw CreateDuplicateKeyException();
                }

                retryOperation = operation;
            })
            .Returns(Task.CompletedTask);
        operationCollection.SetupSequence(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceCreationOperationDocument,
                    UserRideOccurrenceCreationOperationDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(pendingCursor.Object)
            .ReturnsAsync(releasedCursor.Object);
        operationCollection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        collection.SetupSequence(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument, UserRideOccurrenceDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(currentCursor.Object);
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);
        RideOccurrence requested = CreateOccurrence("occurrence-1", "item-1", 1024);
        requested.Delete(NowUtc.AddMinutes(2));

        bool deleted = await repository.TryDeleteOwnedAsync(
            requested,
            1,
            CancellationToken.None);

        Assert.False(deleted);
        Assert.Equal("completed", abandoned.OperationState);
        Assert.NotNull(retryOperation);
        Assert.Equal("conflict", retryOperation.OperationState);
        collection.VerifyAll();
        operationCollection.VerifyAll();
        pendingCursor.VerifyAll();
        releasedCursor.VerifyAll();
        currentCursor.VerifyAll();
    }

    [Fact]
    public async Task ReorderIdempotentAsync_WhenNormalizationOmitsMovedOccurrence_ShouldFenceIt()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        RideOccurrence moved = CreateOccurrence("occurrence-1", "item-1", 3072);
        RideOccurrence normalizedPrevious = CreateOccurrence(
            "occurrence-2",
            "item-2",
            4000);
        RideOccurrence normalizedAnchor = CreateOccurrence(
            "occurrence-3",
            "item-3",
            4001);
        UserRideOccurrenceDocument concurrent = moved.ToDocument();
        concurrent.Status = RideOccurrenceStatus.Attempted;
        concurrent.Version = 2;
        concurrent.UpdatedAt = NowUtc.AddMinutes(1);
        UserRideOccurrenceDocument previousBeforeNormalization =
            normalizedPrevious.ToDocument();
        UserRideOccurrenceDocument anchorBeforeNormalization =
            normalizedAnchor.ToDocument();
        normalizedPrevious.MoveTo(2048, NowUtc.AddMinutes(2));
        normalizedAnchor.MoveTo(4096, NowUtc.AddMinutes(2));
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> guardCursor =
            CreateAsyncCursor(new[]
            {
                concurrent,
                previousBeforeNormalization,
                anchorBeforeNormalization,
            });
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> currentCursor =
            CreateAsyncCursor(new[] { concurrent });
        UserRideOccurrenceCreationOperationDocument? reserved = null;
        FilterDefinition<UserRideOccurrenceDocument>? versionFence = null;
        UpdateDefinition<UserRideOccurrenceDocument>? versionFenceUpdate = null;
        operationCollection.Setup(value => value.InsertOneAsync(
                It.IsAny<UserRideOccurrenceCreationOperationDocument>(),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .Callback((
                UserRideOccurrenceCreationOperationDocument operation,
                InsertOneOptions _,
                CancellationToken _) => reserved = operation)
            .Returns(Task.CompletedTask);
        operationCollection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        collection.SetupSequence(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument,
                    UserRideOccurrenceDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(guardCursor.Object)
            .ReturnsAsync(currentCursor.Object);
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserRideOccurrenceDocument> filter,
                UpdateDefinition<UserRideOccurrenceDocument> update,
                UpdateOptions _,
                CancellationToken _) =>
            {
                versionFence = filter;
                versionFenceUpdate = update;
            })
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);
        RideOccurrenceReorderRequest request = new RideOccurrenceReorderRequest(
            VisitId.Parse("visit-1"),
            "user-1",
            moved.Id,
            1,
            normalizedAnchor.Id,
            RideOccurrencePlacement.Before);

        IdempotentRideOccurrenceReorderResult result =
            await repository.ReorderIdempotentAsync(
                request,
                new[]
                {
                    new RideOccurrenceVersionedChange(
                        normalizedPrevious,
                        1,
                        4000),
                    new RideOccurrenceVersionedChange(
                        normalizedAnchor,
                        1,
                        4001),
                },
                new[]
                {
                    new RideOccurrenceOrderGuard(moved.Id, 3072),
                    new RideOccurrenceOrderGuard(normalizedPrevious.Id, 4000),
                    new RideOccurrenceOrderGuard(normalizedAnchor.Id, 4001),
                },
                moved,
                false,
                NowUtc.AddMinutes(2),
                "request-1",
                null,
                CancellationToken.None);

        Assert.Equal(IdempotentRideOccurrenceReorderStatus.Conflict, result.Status);
        Assert.NotNull(reserved);
        Assert.Equal("conflict", reserved.OperationState);
        Assert.DoesNotContain(
            reserved.ReorderItems!,
            item => string.Equals(
                item.OccurrenceId,
                moved.Id.Value,
                StringComparison.Ordinal));
        Assert.NotNull(versionFence);
        Assert.Equal(1, Render(versionFence)["version"].AsInt64);
        Assert.NotNull(versionFenceUpdate);
        Assert.Equal(
            64,
            Render(versionFenceUpdate)["$set"]["lastReorderOperationKeyHash"]
                .AsString.Length);
        collection.VerifyAll();
        operationCollection.VerifyAll();
        guardCursor.VerifyAll();
        currentCursor.VerifyAll();
    }

    [Fact]
    public async Task ReorderIdempotentAsync_ShouldReserveApplyAndCompleteTheOperation()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> guardCursor =
            new Mock<IAsyncCursor<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        UserRideOccurrenceCreationOperationDocument? reserved = null;
        UpdateDefinition<UserRideOccurrenceDocument>? occurrenceUpdate = null;
        operationCollection.Setup(value => value.InsertOneAsync(
                It.IsAny<UserRideOccurrenceCreationOperationDocument>(),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .Callback((
                UserRideOccurrenceCreationOperationDocument document,
                InsertOneOptions _,
                CancellationToken _) => reserved = document)
            .Returns(Task.CompletedTask);
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserRideOccurrenceDocument> _,
                UpdateDefinition<UserRideOccurrenceDocument> update,
                UpdateOptions _,
                CancellationToken _) => occurrenceUpdate = update)
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        operationCollection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);
        RideOccurrence occurrence = CreateOccurrence("occurrence-1", "item-1", 1024);
        UserRideOccurrenceDocument guardedOccurrence = occurrence.ToDocument();
        guardCursor.SetupSequence(cursor => cursor.MoveNextAsync(CancellationToken.None))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        guardCursor.SetupGet(cursor => cursor.Current)
            .Returns(new[] { guardedOccurrence });
        guardCursor.Setup(cursor => cursor.Dispose());
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument, UserRideOccurrenceDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(guardCursor.Object);
        occurrence.MoveTo(1536, NowUtc.AddMinutes(1));
        RideOccurrenceReorderRequest request = new RideOccurrenceReorderRequest(
            VisitId.Parse("visit-1"),
            "user-1",
            occurrence.Id,
            1,
            RideOccurrenceId.Parse("occurrence-2"),
            RideOccurrencePlacement.Before);

        IdempotentRideOccurrenceReorderResult result =
            await repository.ReorderIdempotentAsync(
                request,
                new[] { new RideOccurrenceVersionedChange(occurrence, 1, 1024) },
                new[] { new RideOccurrenceOrderGuard(occurrence.Id, 1024) },
                occurrence,
                true,
                NowUtc.AddMinutes(1),
                "request-1",
                "creation-request-1",
                CancellationToken.None);

        Assert.Equal(IdempotentRideOccurrenceReorderStatus.Applied, result.Status);
        Assert.Equal(1536, result.Occurrence?.SortPosition);
        Assert.NotNull(reserved);
        Assert.Equal("reorder", reserved.OperationKind);
        Assert.Equal("completed", reserved.OperationState);
        Assert.Equal("occurrence-1", reserved.MovedOccurrenceId);
        Assert.Single(reserved.ReorderItems!);
        Assert.Single(reserved.OrderGuards!);
        Assert.True(reserved.OrderGuardsValidated);
        Assert.Equal(1024, Assert.Single(reserved.ReorderItems!).PreviousSortPosition);
        Assert.Equal(64, reserved.OperationKeyHash.Length);
        Assert.Equal(64, reserved.RelatedCreationOperationKeyHash?.Length);
        Assert.NotNull(occurrenceUpdate);
        BsonDocument renderedUpdate = Render(occurrenceUpdate);
        Assert.Equal(1536, renderedUpdate["$set"]["sortPosition"].AsInt64);
        Assert.Equal(2, renderedUpdate["$set"]["version"].AsInt64);
        Assert.Equal(64, renderedUpdate["$set"]["lastReorderOperationKeyHash"].AsString.Length);
        collection.VerifyAll();
        operationCollection.VerifyAll();
        guardCursor.VerifyAll();
    }

    [Fact]
    public async Task ResolveExistingReorderAsync_WhenConcurrentWorkerValidatedGuards_ShouldCooperate()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        RideOccurrence moved = CreateOccurrence("occurrence-1", "item-1", 1024);
        RideOccurrenceReorderRequest request = new RideOccurrenceReorderRequest(
            VisitId.Parse("visit-1"),
            "user-1",
            moved.Id,
            1,
            null,
            RideOccurrencePlacement.First);
        moved.MoveTo(2048, NowUtc.AddMinutes(1));
        UserRideOccurrenceDocument applied = moved.ToDocument();
        string operationKeyHash =
            UserRideOccurrenceCreationFingerprint.HashOperationKey("request-1");
        applied.LastReorderOperationKeyHash = operationKeyHash;
        UserRideOccurrenceCreationOperationDocument operation =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = request.UserId,
                OperationKeyHash = operationKeyHash,
                PayloadHash = UserRideOccurrenceCreationFingerprint.HashReorderPayload(request),
                OperationKind = "reorder",
                VisitId = request.VisitId.Value,
                OperationState = "pending",
                MovedOccurrenceId = moved.Id.Value,
                ReorderExpectedVersion = 1,
                ReorderPlacement = RideOccurrencePlacement.First,
                OrderGuardsValidated = false,
                OrderGuards = new List<UserRideOccurrenceOrderGuardDocument>
                {
                    new UserRideOccurrenceOrderGuardDocument
                    {
                        OccurrenceId = moved.Id.Value,
                        SortPosition = 1024,
                    },
                },
                ReorderItems = new List<UserRideOccurrenceReorderAllocationDocument>
                {
                    new UserRideOccurrenceReorderAllocationDocument
                    {
                        Index = 0,
                        OccurrenceId = moved.Id.Value,
                        ExpectedVersion = 1,
                        PreviousSortPosition = 1024,
                        ResultSortPosition = applied.SortPosition,
                        ResultVersion = applied.Version,
                        ResultUpdatedAtUtc = applied.UpdatedAt,
                    },
                },
                ReorderResultSnapshot = applied.CreateCreationSnapshot(),
                CreatedAt = NowUtc,
                UpdatedAt = NowUtc,
            };
        UserRideOccurrenceCreationOperationDocument stillUnvalidated =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = operation.UserId,
                OperationKeyHash = operation.OperationKeyHash,
                OperationKind = operation.OperationKind,
                OperationState = "pending",
                OrderGuardsValidated = false,
            };
        UserRideOccurrenceCreationOperationDocument concurrentlyValidated =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = operation.UserId,
                OperationKeyHash = operation.OperationKeyHash,
                OperationKind = operation.OperationKind,
                OperationState = "pending",
                OrderGuardsValidated = true,
            };
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> operationCursor =
            CreateAsyncCursor(new[] { operation });
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> staleStateCursor =
            CreateAsyncCursor(new[] { stillUnvalidated });
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> validatedStateCursor =
            CreateAsyncCursor(new[] { concurrentlyValidated });
        operationCollection.SetupSequence(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceCreationOperationDocument,
                    UserRideOccurrenceCreationOperationDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(operationCursor.Object)
            .ReturnsAsync(staleStateCursor.Object)
            .ReturnsAsync(validatedStateCursor.Object);
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> firstGuardCursor =
            CreateAsyncCursor(new[] { applied });
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> secondGuardCursor =
            CreateAsyncCursor(new[] { applied });
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> appliedCursor =
            CreateAsyncCursor(new[] { applied });
        collection.SetupSequence(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument,
                    UserRideOccurrenceDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(firstGuardCursor.Object)
            .ReturnsAsync(secondGuardCursor.Object)
            .ReturnsAsync(appliedCursor.Object);
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));
        int operationUpdateCount = 0;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument>? conflictFilter = null;
        operationCollection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Returns((
                FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter,
                UpdateDefinition<UserRideOccurrenceCreationOperationDocument> _,
                UpdateOptions _,
                CancellationToken _) =>
            {
                operationUpdateCount++;
                if (operationUpdateCount == 1)
                {
                    conflictFilter = filter;
                    return Task.FromResult<UpdateResult>(
                        new UpdateResult.Acknowledged(0, 0, null));
                }

                return Task.FromResult<UpdateResult>(
                    new UpdateResult.Acknowledged(1, 1, null));
            });
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);

        IdempotentRideOccurrenceReorderResult? result =
            await repository.ResolveExistingReorderAsync(
                request,
                "request-1",
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(IdempotentRideOccurrenceReorderStatus.Replayed, result.Status);
        Assert.Equal(2, operationUpdateCount);
        Assert.NotNull(conflictFilter);
        BsonDocument rendered = Render(conflictFilter);
        Assert.Equal("pending", rendered["operationState"].AsString);
        Assert.False(rendered["orderGuardsValidated"].AsBoolean);
        operationCollection.VerifyAll();
        collection.VerifyAll();
        operationCursor.VerifyAll();
        staleStateCursor.VerifyAll();
        validatedStateCursor.VerifyAll();
        firstGuardCursor.VerifyAll();
        secondGuardCursor.VerifyAll();
        appliedCursor.VerifyAll();
    }

    [Fact]
    public async Task ReorderIdempotentAsync_WithAnotherPendingOperation_ShouldFinishItAndReleaseTheVisit()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        RideOccurrence moved = CreateOccurrence("occurrence-1", "item-1", 1024);
        RideOccurrence anchor = CreateOccurrence("occurrence-2", "item-2", 2048);
        RideOccurrenceReorderRequest abandonedRequest = new RideOccurrenceReorderRequest(
            VisitId.Parse("visit-1"),
            "user-1",
            moved.Id,
            1,
            anchor.Id,
            RideOccurrencePlacement.Before);
        moved.MoveTo(1536, NowUtc.AddMinutes(1));
        UserRideOccurrenceDocument movedSnapshot = moved.ToDocument();
        string abandonedKeyHash =
            UserRideOccurrenceCreationFingerprint.HashOperationKey("abandoned-request");
        UserRideOccurrenceCreationOperationDocument abandoned =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                OperationKeyHash = abandonedKeyHash,
                PayloadHash =
                    UserRideOccurrenceCreationFingerprint.HashReorderPayload(abandonedRequest),
                OperationKind = "reorder",
                VisitId = "visit-1",
                OperationState = "pending",
                MovedOccurrenceId = moved.Id.Value,
                ReorderExpectedVersion = 1,
                ReorderAnchorOccurrenceId = anchor.Id.Value,
                ReorderPlacement = RideOccurrencePlacement.Before,
                OrderGuardsValidated = true,
                OrderGuards = new List<UserRideOccurrenceOrderGuardDocument>
                {
                    new UserRideOccurrenceOrderGuardDocument
                    {
                        OccurrenceId = moved.Id.Value,
                        SortPosition = 1024,
                    },
                    new UserRideOccurrenceOrderGuardDocument
                    {
                        OccurrenceId = anchor.Id.Value,
                        SortPosition = 2048,
                    },
                },
                ReorderItems = new List<UserRideOccurrenceReorderAllocationDocument>
                {
                    new UserRideOccurrenceReorderAllocationDocument
                    {
                        Index = 0,
                        OccurrenceId = moved.Id.Value,
                        ExpectedVersion = 1,
                        PreviousSortPosition = 1024,
                        ResultSortPosition = movedSnapshot.SortPosition,
                        ResultVersion = movedSnapshot.Version,
                        ResultUpdatedAtUtc = movedSnapshot.UpdatedAt,
                    },
                },
                ReorderResultSnapshot = movedSnapshot.CreateCreationSnapshot(),
                CreatedAt = NowUtc,
                UpdatedAt = NowUtc,
            };
        RideOccurrenceReorderRequest retryRequest = abandonedRequest with
        {
            ExpectedVersion = 2,
        };
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> missingOwnCursor =
            CreateAsyncCursor(Array.Empty<UserRideOccurrenceCreationOperationDocument>());
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> pendingCursor =
            CreateAsyncCursor(new[] { abandoned });
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> releasedCursor =
            CreateAsyncCursor(Array.Empty<UserRideOccurrenceCreationOperationDocument>());
        operationCollection.SetupSequence(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceCreationOperationDocument,
                    UserRideOccurrenceCreationOperationDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(missingOwnCursor.Object)
            .ReturnsAsync(pendingCursor.Object)
            .ReturnsAsync(releasedCursor.Object);
        operationCollection.SetupSequence(value => value.InsertOneAsync(
                It.IsAny<UserRideOccurrenceCreationOperationDocument>(),
                It.IsAny<InsertOneOptions>(),
                CancellationToken.None))
            .ThrowsAsync(CreateDuplicateKeyException())
            .Returns(Task.CompletedTask);
        operationCollection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> guardCursor = CreateAsyncCursor(
            new[] { moved.ToDocument(), anchor.ToDocument() });
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument, UserRideOccurrenceDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(guardCursor.Object);
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);

        IdempotentRideOccurrenceReorderResult result = await repository.ReorderIdempotentAsync(
            retryRequest,
            Array.Empty<RideOccurrenceVersionedChange>(),
            new[]
            {
                new RideOccurrenceOrderGuard(moved.Id, 1536),
                new RideOccurrenceOrderGuard(anchor.Id, 2048),
            },
            moved,
            false,
            NowUtc.AddMinutes(2),
            "new-request",
            null,
            CancellationToken.None);

        Assert.Equal(IdempotentRideOccurrenceReorderStatus.Applied, result.Status);
        Assert.Equal("completed", abandoned.OperationState);
        collection.VerifyAll();
        operationCollection.VerifyAll();
        missingOwnCursor.VerifyAll();
        pendingCursor.VerifyAll();
        releasedCursor.VerifyAll();
        guardCursor.VerifyAll();
    }

    [Fact]
    public async Task ResolveExistingReorderAsync_WithAnotherPayload_ShouldReportIdempotencyConflict()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        RideOccurrenceReorderRequest request = new RideOccurrenceReorderRequest(
            VisitId.Parse("visit-1"),
            "user-1",
            RideOccurrenceId.Parse("occurrence-1"),
            1,
            RideOccurrenceId.Parse("occurrence-2"),
            RideOccurrencePlacement.Before);
        RideOccurrenceReorderRequest reservedRequest = request with
        {
            AnchorOccurrenceId = RideOccurrenceId.Parse("occurrence-3"),
        };
        UserRideOccurrenceCreationOperationDocument operation =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                OperationKeyHash =
                    UserRideOccurrenceCreationFingerprint.HashOperationKey("request-1"),
                PayloadHash =
                    UserRideOccurrenceCreationFingerprint.HashReorderPayload(reservedRequest),
                OperationKind = "reorder",
                OperationState = "completed",
            };
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> operationCursor =
            CreateAsyncCursor(new[] { operation });
        operationCollection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceCreationOperationDocument,
                    UserRideOccurrenceCreationOperationDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(operationCursor.Object);
        UserRideOccurrenceRepository repository = CreateRepository(
            collection.Object,
            operationCollection.Object);

        IdempotentRideOccurrenceReorderResult? result =
            await repository.ResolveExistingReorderAsync(
                request,
                "request-1",
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(IdempotentRideOccurrenceReorderStatus.IdempotencyConflict, result.Status);
        collection.VerifyNoOtherCalls();
        operationCollection.VerifyAll();
        operationCursor.VerifyAll();
    }

    [Fact]
    public void BuildReorderRollbackUpdate_ShouldRestorePositionFenceAndOperationMarker()
    {
        UserRideOccurrenceDocument current = CreateOccurrence(
                "occurrence-1",
                "item-1",
                1536)
            .ToDocument();
        current.Version = 4;
        current.LastReorderOperationKeyHash = "operation-hash";
        UserRideOccurrenceReorderAllocationDocument allocation =
            new UserRideOccurrenceReorderAllocationDocument
            {
                PreviousSortPosition = 1024,
            };

        UpdateDefinition<UserRideOccurrenceDocument> update =
            UserRideOccurrenceReorderRecovery.BuildRollbackUpdate(current, allocation);

        BsonDocument rendered = Render(update);
        Assert.Equal(1024, rendered["$set"]["sortPosition"].AsInt64);
        Assert.Equal(5, rendered["$set"]["version"].AsInt64);
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("lastReorderOperationKeyHash"));
    }

    [Fact]
    public void ReorderAllocationWasApplied_AfterANewerDomainUpdate_ShouldRemainRecoverable()
    {
        UserRideOccurrenceDocument current = CreateOccurrence(
                "occurrence-1",
                "item-1",
                1536)
            .ToDocument();
        current.Version = 3;
        current.UpdatedAt = NowUtc.AddMinutes(2);
        current.LastReorderOperationKeyHash = "operation-hash";
        UserRideOccurrenceReorderAllocationDocument allocation =
            new UserRideOccurrenceReorderAllocationDocument
            {
                PreviousSortPosition = 1024,
                ResultSortPosition = 1536,
                ResultVersion = 2,
                ResultUpdatedAtUtc = NowUtc.AddMinutes(1),
            };

        bool wasApplied = UserRideOccurrenceReorderRecovery.AllocationWasApplied(
            current,
            allocation,
            "operation-hash");

        Assert.True(wasApplied);
    }

    [Fact]
    public void ReorderOperation_AtMaximumSize_ShouldKeepOnlyOneFullSnapshot()
    {
        string longNonAsciiNote = new string('\u754C', RideOccurrence.MaximumPrivateNoteLength);
        UserRideOccurrenceCreationOperationDocument operation =
            new UserRideOccurrenceCreationOperationDocument
            {
                Id = "operation-1",
                UserId = "user-1",
                OperationKeyHash = new string('a', 64),
                PayloadHash = new string('b', 64),
                OperationKind = "reorder",
                VisitId = "visit-1",
                OperationState = "pending",
                MovedOccurrenceId = "occurrence-1",
                ReorderItems = Enumerable.Range(
                        0,
                        RideOccurrenceOrderPlanner.MaximumReorderSize)
                    .Select(index => new UserRideOccurrenceReorderAllocationDocument
                    {
                        Index = index,
                        OccurrenceId = $"occurrence-{index}",
                        ExpectedVersion = 1,
                        PreviousSortPosition = index + 1,
                        ResultSortPosition = index + 2,
                        ResultVersion = 2,
                        ResultUpdatedAtUtc = NowUtc,
                    })
                    .ToList(),
                OrderGuards = Enumerable.Range(
                        0,
                        RideOccurrenceOrderPlanner.MaximumReorderSize)
                    .Select(index => new UserRideOccurrenceOrderGuardDocument
                    {
                        OccurrenceId = $"occurrence-{index}",
                        SortPosition = index + 1,
                    })
                    .ToList(),
                ReorderResultSnapshot = new UserRideOccurrenceCreationSnapshotDocument
                {
                    VisitId = "visit-1",
                    ParkId = "park-1",
                    ParkItemId = "item-1",
                    Moment = new RideOccurrenceMomentDocument(),
                    Status = RideOccurrenceStatus.Completed,
                    Source = RideLogSource.Manual,
                    HistoricalConsistency = HistoricalConsistency.Verified,
                    PrivateNote = longNonAsciiNote,
                    Version = 1,
                    CreatedAtUtc = NowUtc,
                    UpdatedAtUtc = NowUtc,
                },
            };

        BsonDocument serialized = operation.ToBsonDocument();
        BsonArray allocations = serialized["reorderItems"].AsBsonArray;

        Assert.Equal(RideOccurrenceOrderPlanner.MaximumReorderSize, allocations.Count);
        Assert.DoesNotContain(
            "resultSnapshot",
            allocations[0].AsBsonDocument.Names);
        Assert.DoesNotContain("privateNote", allocations[0].AsBsonDocument.Names);
        Assert.True(serialized.ToBson().Length < 1_000_000);
    }

    [Fact]
    public void ReorderGuardsMatch_WhenANeighborMoved_ShouldRejectTheStalePlan()
    {
        UserRideOccurrenceDocument current = CreateOccurrence(
                "occurrence-2",
                "item-2",
                4096)
            .ToDocument();
        UserRideOccurrenceOrderGuardDocument guard =
            new UserRideOccurrenceOrderGuardDocument
            {
                OccurrenceId = "occurrence-2",
                SortPosition = 2048,
            };

        bool matches = UserRideOccurrenceOrderGuardValidator.GuardsMatch(
            new[] { guard },
            new[] { current });

        Assert.False(matches);
    }

    [Fact]
    public void ReorderGuardsMatch_WhenAnOccurrenceWasAdded_ShouldRejectTheStalePlan()
    {
        UserRideOccurrenceDocument original = CreateOccurrence(
                "occurrence-1",
                "item-1",
                1024)
            .ToDocument();
        UserRideOccurrenceDocument added = CreateOccurrence(
                "occurrence-2",
                "item-2",
                2048)
            .ToDocument();
        UserRideOccurrenceOrderGuardDocument guard =
            new UserRideOccurrenceOrderGuardDocument
            {
                OccurrenceId = "occurrence-1",
                SortPosition = 1024,
            };

        bool matches = UserRideOccurrenceOrderGuardValidator.GuardsMatch(
            new[] { guard },
            new[] { original, added });

        Assert.False(matches);
    }

    [Fact]
    public async Task ReorderGuardValidation_WhenAnotherWorkerValidated_ShouldIgnoreItsMutations()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        UserRideOccurrenceCreationOperationDocument operation =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                OperationKeyHash = "operation-hash",
                OperationKind = "reorder",
                OperationState = "pending",
                OrderGuardsValidated = false,
                OrderGuards = new List<UserRideOccurrenceOrderGuardDocument>
                {
                    new UserRideOccurrenceOrderGuardDocument
                    {
                        OccurrenceId = "occurrence-1",
                        SortPosition = 1024,
                    },
                },
            };
        UserRideOccurrenceCreationOperationDocument durable =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = operation.UserId,
                OperationKeyHash = operation.OperationKeyHash,
                OperationKind = operation.OperationKind,
                OperationState = "completed",
                OrderGuardsValidated = true,
            };
        UserRideOccurrenceDocument concurrentlyMoved = CreateOccurrence(
                "occurrence-1",
                "item-1",
                2048)
            .ToDocument();
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> occurrenceCursor =
            CreateAsyncCursor(new[] { concurrentlyMoved });
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> operationCursor =
            CreateAsyncCursor(new[] { durable });
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument,
                    UserRideOccurrenceDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(occurrenceCursor.Object);
        operationCollection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceCreationOperationDocument,
                    UserRideOccurrenceCreationOperationDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(operationCursor.Object);
        UserRideOccurrenceOrderGuardValidator validator =
            new UserRideOccurrenceOrderGuardValidator(
                collection.Object,
                operationCollection.Object);

        RideOccurrenceOrderGuardValidationStatus status =
            await validator.EnsureValidatedAsync(
                operation,
                new RideOccurrenceReorderRequest(
                    VisitId.Parse("visit-1"),
                    "user-1",
                    RideOccurrenceId.Parse("occurrence-1"),
                    1,
                    null,
                    RideOccurrencePlacement.First),
                CancellationToken.None);

        Assert.Equal(RideOccurrenceOrderGuardValidationStatus.Validated, status);
        Assert.True(operation.OrderGuardsValidated);
        Assert.Equal("completed", operation.OperationState);
        collection.VerifyAll();
        operationCollection.VerifyAll();
        occurrenceCursor.VerifyAll();
        operationCursor.VerifyAll();
    }

    [Fact]
    public async Task ReorderConflictTransition_ShouldRequirePendingUnvalidatedReservation()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        FilterDefinition<UserRideOccurrenceCreationOperationDocument>? capturedFilter = null;
        operationCollection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter,
                UpdateDefinition<UserRideOccurrenceCreationOperationDocument> _,
                UpdateOptions _,
                CancellationToken _) => capturedFilter = filter)
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        UserRideOccurrenceOrderGuardValidator validator =
            new UserRideOccurrenceOrderGuardValidator(
                collection.Object,
                operationCollection.Object);
        UserRideOccurrencePendingOperationRecovery recovery =
            new UserRideOccurrencePendingOperationRecovery(
                collection.Object,
                operationCollection.Object,
                validator,
                new UserRideOccurrenceCreationRecovery(collection.Object),
                new UserRideOccurrenceDeleteOperationCoordinator(
                    collection.Object,
                    operationCollection.Object));
        UserRideOccurrenceCreationOperationDocument operation =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                OperationKeyHash = "operation-hash",
                OperationKind = "reorder",
                OperationState = "pending",
                OrderGuardsValidated = false,
                UpdatedAt = NowUtc,
            };

        bool transitioned = await recovery.TrySetUnvalidatedReorderConflictAsync(
            operation,
            CancellationToken.None);

        Assert.True(transitioned);
        Assert.Equal("conflict", operation.OperationState);
        Assert.NotNull(capturedFilter);
        BsonDocument rendered = Render(capturedFilter);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal("operation-hash", rendered["operationKeyHash"].AsString);
        Assert.Equal("reorder", rendered["operationKind"].AsString);
        Assert.Equal("pending", rendered["operationState"].AsString);
        Assert.False(rendered["orderGuardsValidated"].AsBoolean);
        collection.VerifyNoOtherCalls();
        operationCollection.VerifyAll();
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

    private static Mock<IAsyncCursor<TDocument>> CreateAsyncCursor<TDocument>(
        IReadOnlyCollection<TDocument> firstBatch,
        Func<IReadOnlyCollection<TDocument>>? secondBatch = null)
    {
        Mock<IAsyncCursor<TDocument>> cursor =
            new Mock<IAsyncCursor<TDocument>>(MockBehavior.Strict);
        cursor.SetupSequence(value => value.MoveNextAsync(CancellationToken.None))
            .ReturnsAsync(true)
            .ReturnsAsync(secondBatch is not null)
            .ReturnsAsync(false);
        cursor.SetupSequence(value => value.Current)
            .Returns(firstBatch)
            .Returns(() => secondBatch?.Invoke() ?? Array.Empty<TDocument>());
        cursor.Setup(value => value.Dispose());
        return cursor;
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

    private static RideOccurrence CreateOccurrence(
        string id,
        string parkItemId,
        long sortPosition)
    {
        return CreateOccurrence(id, parkItemId, sortPosition, NowUtc);
    }

    private static RideOccurrenceCreationRequest CreateRequest(
        IReadOnlyList<RideOccurrence> occurrences,
        bool confirmHistoricalConflict = false)
    {
        RideOccurrence first = occurrences[0];
        return new RideOccurrenceCreationRequest(
            first.VisitId,
            first.UserId,
            occurrences.Select(occurrence => new RideOccurrenceCreationRequestItem(
                occurrence.ParkItemId,
                occurrence.Moment,
                occurrence.Status,
                occurrence.Source,
                occurrence.PrivateNote,
                confirmHistoricalConflict)).ToArray());
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

    private static BsonDocument Render(
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter)
    {
        IBsonSerializer<UserRideOccurrenceCreationOperationDocument> serializer =
            BsonSerializer.SerializerRegistry
                .GetSerializer<UserRideOccurrenceCreationOperationDocument>();
        RenderArgs<UserRideOccurrenceCreationOperationDocument> arguments =
            new RenderArgs<UserRideOccurrenceCreationOperationDocument>(
                serializer,
                BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }
}
