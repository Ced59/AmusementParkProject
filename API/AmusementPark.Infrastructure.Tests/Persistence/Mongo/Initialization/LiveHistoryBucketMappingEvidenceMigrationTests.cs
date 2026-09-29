using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class LiveHistoryBucketMappingEvidenceMigrationTests
{
    [Fact]
    public void BuildFilter_ShouldSelectBucketsContainingAmbiguousMappingEvidence()
    {
        BsonDocument filter = LiveHistoryBucketMappingEvidenceMigration.BuildFilter();
        string json = filter.ToJson();

        Assert.Contains("samples", json, StringComparison.Ordinal);
        Assert.Contains("$elemMatch", json, StringComparison.Ordinal);
        Assert.Contains("externalTargetId", json, StringComparison.Ordinal);
        Assert.Contains("mappingVersion", json, StringComparison.Ordinal);
        Assert.Contains("$exists", json, StringComparison.Ordinal);
        Assert.Contains("$in", json, StringComparison.Ordinal);
    }
}
