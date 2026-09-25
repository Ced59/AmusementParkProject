using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripAdmissionRepositoryTests
{
    [Fact]
    public void BuildNoPendingDepartureCleanupFilter_ShouldFenceMemberReentry()
    {
        FilterDefinition<TripPlanDocument> filter =
            TripAdmissionRepository.BuildNoPendingDepartureCleanupFilter(" user-2 ");

        BsonDocument rendered = filter.Render(new RenderArgs<TripPlanDocument>(
            BsonSerializer.SerializerRegistry.GetSerializer<TripPlanDocument>(),
            BsonSerializer.SerializerRegistry));

        Assert.Equal(
            new BsonDocument(
                "departedPreferenceCleanupUserIds",
                new BsonDocument("$ne", "user-2")),
            rendered);
    }
}
