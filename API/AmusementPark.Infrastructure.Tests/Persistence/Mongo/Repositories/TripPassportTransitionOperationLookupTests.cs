using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripPassportTransitionOperationLookupTests
{
    [Fact]
    public void VisitLookup_ShouldStayOwnedAndBoundedToTheRequestedOperationHashes()
    {
        FilterDefinition<UserVisitDocument> filter =
            UserVisitMongoDefinitions.BuildOwnedCreationOperationsFilter(
                " user-1 ",
                new[] { "hash-1", "hash-2", "hash-1" });

        BsonDocument rendered = Render(filter);

        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal(
            new[] { "hash-1", "hash-2" },
            rendered["creationOperationKeyHash"]["$in"].AsBsonArray
                .Select(static value => value.AsString));
        Assert.True(rendered.Contains("deletedAtUtc"));
    }

    [Fact]
    public void ExactDateVisitLookup_ShouldExcludeSoftDeletedVisits()
    {
        FilterDefinition<UserVisitDocument> filter =
            UserVisitMongoDefinitions.BuildOwnedExactDatesFilter(
                " user-1 ",
                new[] { 20270820, 20270820 });

        BsonDocument rendered = Render(filter);

        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal(
            new[] { 20270820 },
            rendered["dateSortKey"]["$in"].AsBsonArray
                .Select(static value => value.AsInt32));
        Assert.True(rendered.Contains("deletedAtUtc"));
    }

    [Fact]
    public void DeletedCreationRelease_ShouldTargetOnlyTheOwnedTombstoneAndClearItsKey()
    {
        FilterDefinition<UserVisitDocument> filter =
            UserVisitMongoDefinitions.BuildDeletedCreationOperationFilter(
                " user-1 ",
                " operation-hash ");
        UpdateDefinition<UserVisitDocument> update =
            UserVisitMongoDefinitions.BuildReleaseCreationOperationUpdate();

        BsonDocument renderedFilter = Render(filter);
        BsonDocument renderedUpdate = Render(update);

        Assert.Equal("user-1", renderedFilter["userId"].AsString);
        Assert.Equal(
            "operation-hash",
            renderedFilter["creationOperationKeyHash"].AsString);
        Assert.True(renderedFilter.Contains("deletedAtUtc"));
        Assert.Equal(
            new[]
            {
                "creationOperationKeyHash",
                "creationPayloadHash",
                "creationSnapshot",
            },
            renderedUpdate["$unset"].AsBsonDocument.Names);
    }

    [Fact]
    public void RideLookup_ShouldReturnOnlyResumableOrCompletedCreationOperations()
    {
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions
                .BuildCreationOperationsFilter(
                    "user-1",
                    new[] { "hash-1", "hash-2" });

        BsonDocument rendered = Render(filter);

        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal(
            new[] { "creation-key-reservation", "creation" },
            rendered["operationKind"]["$in"].AsBsonArray
                .Select(static value => value.AsString));
        Assert.Equal(
            new[] { "reserved", "pending", "completed" },
            rendered["operationState"]["$in"].AsBsonArray
                .Select(static value => value.AsString));
        Assert.Equal(
            new[] { "hash-1", "hash-2" },
            rendered["operationKeyHash"]["$in"].AsBsonArray
                .Select(static value => value.AsString));
    }

    [Fact]
    public void RideRelease_ShouldTargetOnlyTheOwnedDeletedVisitCreationOperation()
    {
        VisitId visitId = VisitId.New();
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions
                .BuildBatchCreationReleaseFilter(
                    " user-1 ",
                    visitId.Value,
                    " operation-hash ");

        BsonDocument rendered = Render(filter);

        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal(visitId.Value, rendered["visitId"].AsString);
        Assert.Equal("operation-hash", rendered["operationKeyHash"].AsString);
        Assert.Equal(
            new[] { "creation-key-reservation", "creation" },
            rendered["operationKind"]["$in"].AsBsonArray
                .Select(static value => value.AsString));
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
