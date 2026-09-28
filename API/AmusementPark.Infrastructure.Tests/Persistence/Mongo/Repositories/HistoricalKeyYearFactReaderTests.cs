using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class HistoricalKeyYearFactReaderTests
{
    [Fact]
    public void BuildPipeline_SelectsAllLatestPublishedDecisionEligibleRevisionsForRequestedParks()
    {
        IReadOnlyCollection<BsonDocument> pipeline = HistoricalKeyYearFactReader.BuildPipeline(
            new[] { " park-1 ", "park-2", "park-1" });
        BsonDocument latestRevisionSort = pipeline
            .First(static stage => stage.Contains("$sort"))["$sort"]
            .AsBsonDocument;
        string rendered = string.Join("\n", pipeline.Select(static stage => stage.ToJson()));

        Assert.Equal(-1, latestRevisionSort["factId"].AsInt32);
        Assert.Equal(-1, latestRevisionSort["revision"].AsInt32);
        Assert.Contains("$group", rendered, StringComparison.Ordinal);
        Assert.Contains("$first", rendered, StringComparison.Ordinal);
        Assert.Contains("Published", rendered, StringComparison.Ordinal);
        Assert.Contains("Verified", rendered, StringComparison.Ordinal);
        Assert.Contains("Probable", rendered, StringComparison.Ordinal);
        Assert.Contains("Disputed", rendered, StringComparison.Ordinal);
        Assert.Contains("subject.contextParkId", rendered, StringComparison.Ordinal);
        Assert.Contains("park-1", rendered, StringComparison.Ordinal);
        Assert.Contains("park-2", rendered, StringComparison.Ordinal);
        Assert.Contains("createdAt", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("recordedAtUtc", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("$limit", rendered, StringComparison.Ordinal);
    }
}
