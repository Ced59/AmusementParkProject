using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripPilotMetricsRepositoryTests
{
    [Fact]
    public void DistinctTripCountPipeline_ShouldReturnOnlyAServerSideScalar()
    {
        BsonDocument filter = new("documentState", "Committed");

        BsonDocument[] pipeline =
            TripPilotMetricsRepository.BuildDistinctTripCountPipeline(filter);

        Assert.Equal(3, pipeline.Length);
        Assert.Equal(filter, pipeline[0]["$match"].AsBsonDocument);
        Assert.Equal("$tripPlanId", pipeline[1]["$group"]["_id"].AsString);
        Assert.Equal("count", pipeline[2]["$count"].AsString);
    }

    [Fact]
    public void CollaborativeFilter_ShouldRequireTwoActiveMembersInsideMongo()
    {
        BsonDocument filter = TripPilotMetricsRepository.BuildCollaborativePlanFilter().Render(
            new RenderArgs<TripPlanDocument>(
                BsonSerializer.SerializerRegistry.GetSerializer<TripPlanDocument>(),
                BsonSerializer.SerializerRegistry));
        string rendered = filter.ToJson();

        Assert.Contains("$filter", rendered, StringComparison.Ordinal);
        Assert.Contains("members", rendered, StringComparison.Ordinal);
        Assert.Contains("Active", rendered, StringComparison.Ordinal);
        Assert.Contains("$gte", rendered, StringComparison.Ordinal);
        Assert.Contains("2", rendered, StringComparison.Ordinal);
    }
}
