using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserRideOccurrenceCreationRecoveryTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 22, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RecoverAsync_WhenAnExactLateInsertUsesAnOlderFence_ShouldAdoptIt()
    {
        UserRideOccurrenceDocument lateDocument = CreateOccurrence().ToDocument();
        lateDocument.CreationOperationKeyHash = "operation-hash";
        lateDocument.CreationPayloadHash = "payload-hash";
        lateDocument.CreationOperationIndex = 0;
        lateDocument.CreationOperationCount = 1;
        lateDocument.CreationSnapshot = lateDocument.CreateCreationSnapshot();
        lateDocument.ContentMutationFenceToken = 8;
        UserRideOccurrenceCreationOperationDocument operation =
            new UserRideOccurrenceCreationOperationDocument
            {
                UserId = "user-1",
                OperationKeyHash = "operation-hash",
                PayloadHash = "payload-hash",
                OperationKind = "creation",
                VisitId = "visit-1",
                ContentMutationFenceToken = 9,
                OperationState = "pending",
                AppendBaseWasEmpty = true,
                AppendBaseValidated = true,
                Items = new List<UserRideOccurrenceCreationAllocationDocument>
                {
                    new UserRideOccurrenceCreationAllocationDocument
                    {
                        Index = 0,
                        OccurrenceId = lateDocument.Id,
                        SortPosition = lateDocument.SortPosition,
                        CreatedAtUtc = lateDocument.CreatedAt,
                        UpdatedAtUtc = lateDocument.UpdatedAt,
                        CreationSnapshot = lateDocument.CreationSnapshot,
                    },
                },
                CreatedAt = NowUtc,
                UpdatedAt = NowUtc,
            };
        Mock<IMongoCollection<UserRideOccurrenceDocument>> collection =
            new Mock<IMongoCollection<UserRideOccurrenceDocument>>(MockBehavior.Strict);
        FilterDefinition<UserRideOccurrenceDocument>? adoptionFilter = null;
        UpdateDefinition<UserRideOccurrenceDocument>? adoptionUpdate = null;
        collection.Setup(value => value.UpdateManyAsync(
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
                adoptionFilter = filter;
                adoptionUpdate = update;
                lateDocument.ContentMutationFenceToken = 9;
            })
            .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        Mock<IAsyncCursor<UserRideOccurrenceDocument>> cursor =
            CreateAsyncCursor(new[] { lateDocument });
        collection.Setup(value => value.FindAsync(
                It.IsAny<FilterDefinition<UserRideOccurrenceDocument>>(),
                It.IsAny<FindOptions<UserRideOccurrenceDocument,
                    UserRideOccurrenceDocument>>(),
                CancellationToken.None))
            .ReturnsAsync(cursor.Object);
        UserRideOccurrenceCreationRecovery recovery =
            new UserRideOccurrenceCreationRecovery(collection.Object);

        IdempotentRideOccurrenceCreationResult result = await recovery.RecoverAsync(
            operation,
            Array.Empty<UserRideOccurrenceDocument>(),
            "payload-hash",
            1,
            CancellationToken.None);

        Assert.Equal(IdempotentRideOccurrenceCreationStatus.Replayed, result.Status);
        Assert.Single(result.Occurrences);
        Assert.NotNull(adoptionFilter);
        BsonDocument renderedFilter = Render(adoptionFilter);
        string filterJson = renderedFilter.ToJson();
        Assert.Contains("visit-1", filterJson, StringComparison.Ordinal);
        Assert.Contains("occurrence-1", filterJson, StringComparison.Ordinal);
        Assert.Contains("payload-hash", filterJson, StringComparison.Ordinal);
        Assert.Contains("$lt", filterJson, StringComparison.Ordinal);
        Assert.NotNull(adoptionUpdate);
        Assert.Equal(
            9,
            Render(adoptionUpdate)["$set"]["contentMutationFenceToken"].AsInt64);
        collection.VerifyAll();
        cursor.VerifyAll();
    }

    private static RideOccurrence CreateOccurrence()
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
            NowUtc);
        return RideOccurrence.Create(
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

    private static BsonDocument Render<TDocument>(
        FilterDefinition<TDocument> filter)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        return filter.Render(new RenderArgs<TDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
    }

    private static BsonDocument Render<TDocument>(
        UpdateDefinition<TDocument> update)
    {
        IBsonSerializer<TDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<TDocument>();
        return update.Render(new RenderArgs<TDocument>(
            serializer,
            BsonSerializer.SerializerRegistry)).AsBsonDocument;
    }
}
