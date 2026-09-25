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
    public void RideLookup_ShouldOnlyReturnCompletedCreationOperations()
    {
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions
                .BuildCompletedCreationOperationsFilter(
                    "user-1",
                    new[] { "hash-1", "hash-2" });

        BsonDocument rendered = Render(filter);

        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal("creation", rendered["operationKind"].AsString);
        Assert.Equal("completed", rendered["operationState"].AsString);
        Assert.Equal(
            new[] { "hash-1", "hash-2" },
            rendered["operationKeyHash"]["$in"].AsBsonArray
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
}
