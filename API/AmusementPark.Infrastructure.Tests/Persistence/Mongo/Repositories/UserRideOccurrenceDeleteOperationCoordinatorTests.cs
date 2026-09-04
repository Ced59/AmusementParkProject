using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserRideOccurrenceDeleteOperationCoordinatorTests
{
    [Fact]
    public async Task TryCompleteAsync_WhenOperationConflicts_ShouldClearPendingAuditEvents()
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
        UserRideOccurrenceCreationOperationDocument operation =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                OperationKeyHash = "operation-hash",
                PayloadHash = "payload-hash",
                OperationKind = "delete",
                VisitId = "visit-1",
                OperationState = "pending",
                DeleteOccurrenceId = null,
                DeleteExpectedVersion = 1,
                DeleteAtUtc = new DateTime(2026, 9, 3, 20, 0, 0, DateTimeKind.Utc),
                PendingAuditEvents = new List<PassportAuditEventDocument>
                {
                    new PassportAuditEventDocument { EventId = "audit-1" },
                },
            };
        UserRideOccurrenceDeleteOperationCoordinator coordinator =
            new UserRideOccurrenceDeleteOperationCoordinator(
                collection.Object,
                operations.Object);

        bool completed = await coordinator.TryCompleteAsync(
            operation,
            CancellationToken.None);

        Assert.False(completed);
        Assert.Equal("conflict", operation.OperationState);
        Assert.Null(operation.PendingAuditEvents);
        Assert.NotNull(capturedUpdate);
        BsonDocument rendered = Render(capturedUpdate);
        Assert.Equal("conflict", rendered["$set"]["operationState"].AsString);
        Assert.True(rendered["$unset"].AsBsonDocument.Contains("pendingAuditEvents"));
        collection.VerifyNoOtherCalls();
        operations.VerifyAll();
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
