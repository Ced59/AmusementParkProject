using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class LiveLatestObservationNormalizedTicksMigrationTests
{
    [Fact]
    public void BuildFilter_ShouldOnlyTargetDocumentsWithoutNormalizedTicks()
    {
        BsonDocument filter = LiveLatestObservationNormalizedTicksMigration.BuildFilter();

        Assert.False(filter["provenance.normalizedAtUtcTicks"]["$exists"].AsBoolean);
    }

    [Fact]
    public void BuildPipeline_ShouldNeverBackdateNormalizationBeforeExactReception()
    {
        string pipeline = new BsonArray(
            LiveLatestObservationNormalizedTicksMigration.BuildPipeline()).ToJson();

        Assert.Contains("$provenance.receivedAtUtcTicks", pipeline, StringComparison.Ordinal);
        Assert.Contains("$provenance.normalizedAtUtc", pipeline, StringComparison.Ordinal);
        Assert.Contains("$max", pipeline, StringComparison.Ordinal);
        Assert.Contains("normalizedAtUtcTicks", pipeline, StringComparison.Ordinal);
    }
}
