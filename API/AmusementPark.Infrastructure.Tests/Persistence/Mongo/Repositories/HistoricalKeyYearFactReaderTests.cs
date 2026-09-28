using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class HistoricalKeyYearFactReaderTests
{
    [Fact]
    public void BuildPipeline_SelectsLatestPublishedDecisionEligibleRevisionsWithinLimit()
    {
        IReadOnlyCollection<BsonDocument> pipeline = HistoricalKeyYearFactReader.BuildPipeline(250);
        BsonDocument latestRevisionSort = pipeline.First()["$sort"].AsBsonDocument;
        string rendered = string.Join("\n", pipeline.Select(static stage => stage.ToJson()));

        Assert.Equal(-1, latestRevisionSort["factId"].AsInt32);
        Assert.Equal(-1, latestRevisionSort["revision"].AsInt32);
        Assert.Contains("$group", rendered, StringComparison.Ordinal);
        Assert.Contains("$first", rendered, StringComparison.Ordinal);
        Assert.Contains("Published", rendered, StringComparison.Ordinal);
        Assert.Contains("Verified", rendered, StringComparison.Ordinal);
        Assert.Contains("Probable", rendered, StringComparison.Ordinal);
        Assert.Contains("Disputed", rendered, StringComparison.Ordinal);
        Assert.Contains("createdAt", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("recordedAtUtc", rendered, StringComparison.Ordinal);
        Assert.Contains("250", rendered, StringComparison.Ordinal);
    }
}
