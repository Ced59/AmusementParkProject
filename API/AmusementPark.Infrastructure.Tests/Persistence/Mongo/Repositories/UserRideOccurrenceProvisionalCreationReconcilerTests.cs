using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Moq.Language;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserRideOccurrenceProvisionalCreationReconcilerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReconcileBatchAsync_WhenExactOperationCompletedCurrentFence_ShouldCommitMarker()
    {
        UserRideOccurrenceDocument document = CreateProvisionalDocument(9);
        UserRideOccurrenceCreationOperationDocument operation = CreateOperation(
            document,
            "completed",
            9);
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        SetupCandidates(collection, document);
        SetupOperation(operationCollection, operation);
        FilterDefinition<UserRideOccurrenceDocument>? updateFilter = null;
        UpdateDefinition<UserRideOccurrenceDocument>? update = null;
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserRideOccurrenceDocument> filter,
                UpdateDefinition<UserRideOccurrenceDocument> definition,
                UpdateOptions _,
                CancellationToken _) =>
            {
                updateFilter = filter;
                update = definition;
            })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        UserRideOccurrenceProvisionalCreationReconciler reconciler =
            new UserRideOccurrenceProvisionalCreationReconciler(
                collection.Object,
                operationCollection.Object);

        int count = await reconciler.ReconcileBatchAsync(50, CancellationToken.None);

        Assert.Equal(1, count);
        Assert.NotNull(updateFilter);
        AssertExactDocumentFilter(updateFilter, 9);
        Assert.NotNull(update);
        Assert.True(Render(update)["$unset"].AsBsonDocument.Contains(
            "creationPendingCompletion"));
        collection.VerifyAll();
        operationCollection.VerifyAll();
    }

    [Fact]
    public async Task ReconcileBatchAsync_WhenOperationRejectedAfterFencePromotion_ShouldDeleteMarker()
    {
        UserRideOccurrenceDocument document = CreateProvisionalDocument(8);
        UserRideOccurrenceCreationOperationDocument operation = CreateOperation(
            document,
            "conflict",
            9);
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        SetupCandidates(collection, document);
        SetupOperation(operationCollection, operation);
        FilterDefinition<UserRideOccurrenceDocument>? deletionFilter = null;
        collection.Setup(value => value.DeleteOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserRideOccurrenceDocument> filter,
                CancellationToken _) => deletionFilter = filter)
            .ReturnsAsync(new DeleteResult.Acknowledged(1));
        UserRideOccurrenceProvisionalCreationReconciler reconciler =
            new UserRideOccurrenceProvisionalCreationReconciler(
                collection.Object,
                operationCollection.Object);

        int count = await reconciler.ReconcileBatchAsync(50, CancellationToken.None);

        Assert.Equal(1, count);
        Assert.NotNull(deletionFilter);
        AssertExactDocumentFilter(deletionFilter, 8);
        collection.VerifyAll();
        operationCollection.VerifyAll();
    }

    [Fact]
    public async Task ReconcileBatchAsync_WhenPromotedOperationIsPending_ShouldLeaveLateMarkerRecoverable()
    {
        UserRideOccurrenceDocument document = CreateProvisionalDocument(8);
        UserRideOccurrenceCreationOperationDocument operation = CreateOperation(
            document,
            "pending",
            9);
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        SetupCandidates(collection, document);
        SetupOperation(operationCollection, operation);
        UserRideOccurrenceProvisionalCreationReconciler reconciler =
            new UserRideOccurrenceProvisionalCreationReconciler(
                collection.Object,
                operationCollection.Object);

        int count = await reconciler.ReconcileBatchAsync(50, CancellationToken.None);

        Assert.Equal(0, count);
        collection.VerifyAll();
        collection.Verify(value => value.UpdateOneAsync(
            It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
            It.IsAny<UpdateDefinition<UserRideOccurrenceDocument>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
        collection.Verify(value => value.DeleteOneAsync(
            It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        operationCollection.VerifyAll();
    }

    [Fact]
    public async Task ReconcileBatchAsync_WhenOccurrenceWasPromotedBeforeCompletedOperation_ShouldWait()
    {
        UserRideOccurrenceDocument document = CreateProvisionalDocument(9);
        UserRideOccurrenceCreationOperationDocument operation = CreateOperation(
            document,
            "completed",
            8);
        UserVisitDocument visit = CreateVisitDocument(
            isFenceReady: false,
            stableFenceToken: 8,
            currentFenceToken: 9);
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        Mock<IMongoCollection<UserVisitDocument>> visitCollection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        SetupCandidates(collection, document);
        SetupOperation(operationCollection, operation);
        SetupVisit(visitCollection, visit);
        UserRideOccurrenceProvisionalCreationReconciler reconciler =
            new UserRideOccurrenceProvisionalCreationReconciler(
                collection.Object,
                operationCollection.Object,
                visitCollection.Object);

        int count = await reconciler.ReconcileBatchAsync(50, CancellationToken.None);

        Assert.Equal(0, count);
        collection.VerifyAll();
        collection.Verify(value => value.UpdateOneAsync(
            It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
            It.IsAny<UpdateDefinition<UserRideOccurrenceDocument>>(),
            It.IsAny<UpdateOptions>(),
            It.IsAny<CancellationToken>()), Times.Never);
        collection.Verify(value => value.DeleteOneAsync(
            It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        operationCollection.VerifyAll();
        visitCollection.VerifyAll();
    }

    [Fact]
    public async Task ReconcileBatchAsync_WhenPromotionCompletedAfterOperationRead_ShouldReloadAndCommit()
    {
        UserRideOccurrenceDocument document = CreateProvisionalDocument(9);
        UserRideOccurrenceCreationOperationDocument staleOperation = CreateOperation(
            document,
            "completed",
            8);
        UserRideOccurrenceCreationOperationDocument promotedOperation = CreateOperation(
            document,
            "completed",
            9);
        UserVisitDocument visit = CreateVisitDocument(
            isFenceReady: true,
            stableFenceToken: 9,
            currentFenceToken: 9);
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operationCollection =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        Mock<IMongoCollection<UserVisitDocument>> visitCollection =
            new Mock<IMongoCollection<UserVisitDocument>>(MockBehavior.Strict);
        SetupCandidates(collection, document);
        SetupOperationSequence(operationCollection, staleOperation, promotedOperation);
        SetupVisit(visitCollection, visit);
        collection.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        UserRideOccurrenceProvisionalCreationReconciler reconciler =
            new UserRideOccurrenceProvisionalCreationReconciler(
                collection.Object,
                operationCollection.Object,
                visitCollection.Object);

        int count = await reconciler.ReconcileBatchAsync(50, CancellationToken.None);

        Assert.Equal(1, count);
        collection.VerifyAll();
        collection.Verify(value => value.DeleteOneAsync(
            It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        operationCollection.VerifyAll();
        visitCollection.VerifyAll();
    }

    private static UserRideOccurrenceDocument CreateProvisionalDocument(long fenceToken)
    {
        Visit visit = Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 4),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            NowUtc);
        RideOccurrence occurrence = RideOccurrence.Create(
            RideOccurrenceId.Parse("occurrence-1"),
            visit,
            "item-1",
            1024,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            null,
            NowUtc);
        UserRideOccurrenceDocument document = occurrence.ToDocument();
        document.CreationOperationKeyHash = "operation-hash";
        document.CreationPayloadHash = "payload-hash";
        document.CreationOperationIndex = 0;
        document.CreationOperationCount = 1;
        document.CreationSnapshot = document.CreateCreationSnapshot();
        document.CreationPendingCompletion = true;
        document.ContentMutationFenceToken = fenceToken;
        return document;
    }

    private static UserRideOccurrenceCreationOperationDocument CreateOperation(
        UserRideOccurrenceDocument document,
        string state,
        long fenceToken)
    {
        return new UserRideOccurrenceCreationOperationDocument
        {
            UserId = document.UserId,
            OperationKeyHash = document.CreationOperationKeyHash!,
            PayloadHash = document.CreationPayloadHash!,
            OperationKind = "creation",
            VisitId = document.VisitId,
            ContentMutationFenceToken = fenceToken,
            OperationState = state,
            AppendBaseWasEmpty = true,
            AppendBaseValidated = true,
            Items = new List<UserRideOccurrenceCreationAllocationDocument>
            {
                new UserRideOccurrenceCreationAllocationDocument
                {
                    Index = 0,
                    OccurrenceId = document.Id,
                    SortPosition = document.SortPosition,
                    CreatedAtUtc = document.CreatedAt,
                    UpdatedAtUtc = document.UpdatedAt,
                    CreationSnapshot = document.CreationSnapshot!,
                },
            },
            CreatedAt = NowUtc,
            UpdatedAt = NowUtc,
        };
    }

    private static UserVisitDocument CreateVisitDocument(
        bool isFenceReady,
        long? stableFenceToken,
        long currentFenceToken)
    {
        return new UserVisitDocument
        {
            Id = "visit-1",
            UserId = "user-1",
            ContentMutationFenceReady = isFenceReady,
            ContentMutationFenceStableToken = stableFenceToken,
            ContentMutationFenceToken = currentFenceToken,
        };
    }

    private static void SetupCandidates(
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection,
        UserRideOccurrenceDocument document)
    {
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> cursor = CreateAsyncCursor(
            new[] { document });
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument,
                    UserRideOccurrenceDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(cursor.Object);
    }

    private static void SetupOperation(
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> collection,
        UserRideOccurrenceCreationOperationDocument operation)
    {
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> cursor =
            CreateAsyncCursor(new[] { operation });
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceCreationOperationDocument,
                    UserRideOccurrenceCreationOperationDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(cursor.Object);
    }

    private static void SetupOperationSequence(
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> collection,
        params UserRideOccurrenceCreationOperationDocument[] operations)
    {
        Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>>[] cursors = operations
            .Select(operation => CreateAsyncCursor(new[] { operation }))
            .ToArray();
        ISetupSequentialResult<Task<IAsyncCursor<
            UserRideOccurrenceCreationOperationDocument>>> sequence = collection
            .SetupSequence(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceCreationOperationDocument,
                    UserRideOccurrenceCreationOperationDocument>>(),
                CancellationToken.None));
        foreach (Mock<IAsyncCursor<UserRideOccurrenceCreationOperationDocument>> cursor in cursors)
        {
            sequence.ReturnsAsync(cursor.Object);
        }
    }

    private static void SetupVisit(
        Mock<IMongoCollection<UserVisitDocument>> collection,
        UserVisitDocument visit)
    {
        Mock<IAsyncCursor<UserVisitDocument>> cursor = CreateAsyncCursor(
            new[] { visit });
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserVisitDocument>>(),
                It.IsAny<FindOptions<UserVisitDocument, UserVisitDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(cursor.Object);
    }

    private static Mock<IAsyncCursor<TDocument>> CreateAsyncCursor<TDocument>(
        IReadOnlyCollection<TDocument> values)
    {
        Mock<IAsyncCursor<TDocument>> cursor =
            new Mock<IAsyncCursor<TDocument>>(MockBehavior.Strict);
        cursor.SetupSequence(value => value.MoveNextAsync(CancellationToken.None))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        cursor.Setup(value => value.Current).Returns(values);
        cursor.Setup(value => value.Dispose());
        return cursor;
    }

    private static void AssertExactDocumentFilter(
        FilterDefinition<UserRideOccurrenceDocument> filter,
        long fenceToken)
    {
        BsonDocument rendered = Render(filter);
        Assert.Equal("occurrence-1", rendered["_id"].AsString);
        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal("visit-1", rendered["visitId"].AsString);
        Assert.Equal("operation-hash", rendered["creationOperationKeyHash"].AsString);
        Assert.Equal("payload-hash", rendered["creationPayloadHash"].AsString);
        Assert.Equal(0, rendered["creationOperationIndex"].AsInt32);
        Assert.Equal(1, rendered["creationOperationCount"].AsInt32);
        Assert.Equal(fenceToken, rendered["contentMutationFenceToken"].AsInt64);
        Assert.True(rendered["creationPendingCompletion"].AsBoolean);
    }

    private static BsonDocument Render<TDocument>(FilterDefinition<TDocument> filter)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        return filter.Render(new RenderArgs<TDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render<TDocument>(UpdateDefinition<TDocument> update)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        return update.Render(new RenderArgs<TDocument>(
            serializer,
            BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }
}
