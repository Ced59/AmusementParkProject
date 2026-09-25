using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserRideOccurrenceCreationOperationMongoDefinitionsTests
{
    [Fact]
    public void BuildBatchCreationReservationReleaseFilter_ShouldFenceTheObservedReservation()
    {
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions
                .BuildBatchCreationReservationReleaseFilter(
                    "user-1",
                    "visit-1",
                    "operation-hash",
                    "reservation-1");

        BsonDocument rendered = Render(filter);

        Assert.Equal("user-1", rendered["userId"].AsString);
        Assert.Equal("operation-hash", rendered["operationKeyHash"].AsString);
        Assert.Equal("reservation-1", rendered["_id"].AsString);
        Assert.Equal("visit-1", rendered["visitId"].AsString);
        Assert.Equal("creation-key-reservation", rendered["operationKind"].AsString);
        Assert.Equal("reserved", rendered["operationState"].AsString);
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
