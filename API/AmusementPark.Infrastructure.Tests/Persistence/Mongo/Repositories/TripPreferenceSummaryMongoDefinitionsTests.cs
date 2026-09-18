using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class TripPreferenceSummaryMongoDefinitionsTests
{
    [Fact]
    public void BuildSummaryStages_ShouldAggregateOnlyCommittedAnswersAndKeepTheLatestUpdate()
    {
        TripPlanId tripId = TripPlanId.New();

        BsonDocument[] stages = TripPreferenceRepository.BuildSummaryStages(
            tripId,
            new[] { "member-1", "member-2" },
            new[] { "item-1" });

        BsonDocument match = stages[0]["$match"].AsBsonDocument;
        BsonDocument group = stages[1]["$group"].AsBsonDocument;
        Assert.Equal(tripId.Value, match["tripPlanId"].AsString);
        Assert.Equal("Committed", match["documentState"].AsString);
        Assert.Equal("Unknown", match["level"].AsBsonDocument["$ne"].AsString);
        Assert.Equal(2, match["userId"].AsBsonDocument["$in"].AsBsonArray.Count);
        Assert.Equal("$updatedAt", group["latestUpdatedAtUtc"].AsBsonDocument["$max"].AsString);
    }
}
