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
        string rendered = string.Join("\n", pipeline.Select(static stage => stage.ToJson()));

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
