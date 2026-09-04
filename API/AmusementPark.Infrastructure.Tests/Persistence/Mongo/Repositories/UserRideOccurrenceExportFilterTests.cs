using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class UserRideOccurrenceExportFilterTests
{
    [Fact]
    public void BuildExportFilter_ShouldSelectOnlyActiveOccurrencesFromActiveVisits()
    {
        BsonDocument filter = Render(
            UserRideOccurrenceRepository.BuildExportFilter(
                "owner-1",
                new[] { VisitId.Parse("visit-1"), VisitId.Parse("visit-2") }));

        Assert.Equal("owner-1", filter["userId"].AsString);
        Assert.Equal(
            new[] { "visit-1", "visit-2" },
            filter["visitId"]["$in"].AsBsonArray.Select(static value => value.AsString));
        Assert.True(filter["deletedAtUtc"].IsBsonNull);
        Assert.True(filter["creationPendingCompletion"]["$ne"].AsBoolean);
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
}
