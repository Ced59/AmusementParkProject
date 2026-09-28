using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class HistoricalVisitDiagnosticsReaderTests
{
    [Fact]
    public void BuildPipeline_ShouldCountDistinctAffectedVisitsWithoutUserData()
    {
        BsonDocument[] pipeline = HistoricalVisitDiagnosticsReader.BuildPipeline(" park-1 ")
            .ToArray();

        Assert.Equal(2, pipeline.Length);
        BsonDocument match = pipeline[0]["$match"].AsBsonDocument;
        Assert.Equal("park-1", match["parkId"].AsString);
        Assert.Equal(RideOccurrenceStatus.Completed.ToString(), match["status"].AsString);
        Assert.True(match["deletedAtUtc"].IsBsonNull);
        Assert.DoesNotContain("userId", pipeline[1].ToJson(), StringComparison.Ordinal);

        BsonDocument facets = pipeline[1]["$facet"].AsBsonDocument;
        Assert.Equal(3, facets.ElementCount);
        Assert.All(
            facets.Elements,
            facet => Assert.Contains(
                facet.Value.AsBsonArray,
                stage => stage.AsBsonDocument.Contains("$group")
                    && stage["$group"].AsBsonDocument.GetValue("_id", BsonNull.Value)
                        == "$visitId"));
    }

    [Fact]
    public void BuildPipeline_WithoutParkId_ShouldRejectTheQuery()
    {
        Assert.Throws<ArgumentException>(() =>
            HistoricalVisitDiagnosticsReader.BuildPipeline(" "));
    }
}
