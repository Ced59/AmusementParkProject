using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserRideOccurrencePendingOperationRecoveryTests
{
    [Theory]
    [InlineData("generic")]
    [InlineData("unvalidated-reorder")]
    [InlineData("reorder-compensation")]
    public async Task ConflictTransitions_ShouldClearPendingAuditEvents(string transition)
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operations =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        UpdateDefinition<UserRideOccurrenceCreationOperationDocument>? capturedUpdate = null;
        operations.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserRideOccurrenceCreationOperationDocument> _,
                UpdateDefinition<UserRideOccurrenceCreationOperationDocument> update,
                UpdateOptions _,
                CancellationToken _) => capturedUpdate = update)
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        UserRideOccurrencePendingOperationRecovery recovery = CreateRecovery(
            collection.Object,
            operations.Object);
        UserRideOccurrenceCreationOperationDocument operation =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                OperationKeyHash = "operation-hash",
                OperationKind = "reorder",
                OperationState = "pending",
                OrderGuardsValidated = false,
                ReorderCompensationStarted = true,
                PendingAuditEvents = new List<PassportAuditEventDocument>
                {
                    new PassportAuditEventDocument { EventId = "audit-1" },
                },
                UpdatedAt = new DateTime(2026, 9, 3, 20, 0, 0, DateTimeKind.Utc),
            };

        bool transitioned;
        if (string.Equals(transition, "generic", StringComparison.Ordinal))
        {
            transitioned = await recovery.SetStateAsync(
                operation,
                "conflict",
                CancellationToken.None);
        }
        else if (string.Equals(
            transition,
            "unvalidated-reorder",
            StringComparison.Ordinal))
        {
            operation.ReorderCompensationStarted = false;
            transitioned = await recovery.TrySetUnvalidatedReorderConflictAsync(
                operation,
                CancellationToken.None);
        }
        else
        {
            transitioned = await recovery.TryFinishReorderCompensationAsync(
                operation,
                CancellationToken.None);
        }

        Assert.True(transitioned);
        Assert.Equal("conflict", operation.OperationState);
        Assert.Null(operation.PendingAuditEvents);
        Assert.NotNull(capturedUpdate);
        BsonDocument rendered = Render(capturedUpdate);
        Assert.Equal("conflict", rendered["$set"]["operationState"].AsString);
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("pendingAuditEvents"));
        collection.VerifyNoOtherCalls();
        operations.VerifyAll();
    }

    [Fact]
    public async Task ReleaseUnvalidatedCreationAsync_ShouldClearUnusedAuditEvents()
    {
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>> operations =
            new Mock<IMongoCollection<UserRideOccurrenceCreationOperationDocument>>(
                MockBehavior.Strict);
        UpdateDefinition<UserRideOccurrenceCreationOperationDocument>? capturedUpdate = null;
        operations.Setup(value => value.UpdateOneAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateDefinition<UserRideOccurrenceCreationOperationDocument>>(),
                It.IsAny<UpdateOptions>(),
                CancellationToken.None))
            .Callback((
                FilterDefinition<UserRideOccurrenceCreationOperationDocument> _,
                UpdateDefinition<UserRideOccurrenceCreationOperationDocument> update,
                UpdateOptions _,
                CancellationToken _) => capturedUpdate = update)
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        UserRideOccurrencePendingOperationRecovery recovery = CreateRecovery(
            collection.Object,
            operations.Object);
        UserRideOccurrenceCreationOperationDocument operation =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                OperationKeyHash = "operation-hash",
                OperationKind = "creation",
                OperationState = "pending",
                AppendBaseValidated = false,
                CreationPreparation = new UserRideOccurrenceCreationPreparationDocument(),
                PendingAuditEvents = new List<PassportAuditEventDocument>
                {
                    new PassportAuditEventDocument { EventId = "audit-1" },
                },
                Items = new List<UserRideOccurrenceCreationAllocationDocument>
                {
                    new UserRideOccurrenceCreationAllocationDocument(),
                },
                UpdatedAt = new DateTime(2026, 9, 3, 20, 0, 0, DateTimeKind.Utc),
            };

        bool released = await recovery.ReleaseUnvalidatedCreationAsync(
            operation,
            CancellationToken.None);

        Assert.True(released);
        Assert.Equal("creation-key-reservation", operation.OperationKind);
        Assert.Equal("reserved", operation.OperationState);
        Assert.Null(operation.PendingAuditEvents);
        Assert.NotNull(capturedUpdate);
        BsonDocument rendered = Render(capturedUpdate);
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("pendingAuditEvents"));
        collection.VerifyNoOtherCalls();
        operations.VerifyAll();
    }

    private static UserRideOccurrencePendingOperationRecovery CreateRecovery(
        IMongoCollection<UserRideOccurrenceDocument> collection,
        IMongoCollection<UserRideOccurrenceCreationOperationDocument> operations)
    {
        return new UserRideOccurrencePendingOperationRecovery(
            collection,
            operations,
            new UserRideOccurrenceOrderGuardValidator(collection, operations),
            new UserRideOccurrenceCreationRecovery(collection),
            new UserRideOccurrenceDeleteOperationCoordinator(collection, operations));
    }

    private static BsonDocument Render(
        UpdateDefinition<UserRideOccurrenceCreationOperationDocument> update)
    {
        IBsonSerializer<UserRideOccurrenceCreationOperationDocument> serializer =
            BsonSerializer.SerializerRegistry
                .GetSerializer<UserRideOccurrenceCreationOperationDocument>();
        return update.Render(
            new RenderArgs<UserRideOccurrenceCreationOperationDocument>(
                serializer,
                BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }
}
