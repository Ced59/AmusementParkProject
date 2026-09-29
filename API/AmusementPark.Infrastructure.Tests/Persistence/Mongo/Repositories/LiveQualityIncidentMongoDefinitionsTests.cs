using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class LiveQualityIncidentMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldExpireMinimizedIncidentsAutomatically()
    {
        IReadOnlyCollection<CreateIndexModel<LiveQualityIncidentDocument>> indexes =
            LiveQualityIncidentMongoDefinitions.BuildIndexes();

        CreateIndexModel<LiveQualityIncidentDocument> ttl = Assert.Single(
            indexes,
            static index => index.Options.Name == "idx_live_quality_expiration_ttl");
        Assert.Equal(TimeSpan.Zero, ttl.Options.ExpireAfter);
        BsonDocument keys = ttl.Keys.Render(
            new RenderArgs<LiveQualityIncidentDocument>(
                BsonSerializer.LookupSerializer<LiveQualityIncidentDocument>(),
                BsonSerializer.SerializerRegistry));
        Assert.Equal(1, keys["expiresAtUtc"].AsInt32);
    }

    [Fact]
    public void BuildReplayCandidatesFilter_ShouldExcludeDiagnosticsAndExpiredEntries()
    {
        DateTime nowUtc = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

        BsonDocument rendered = LiveQualityIncidentMongoDefinitions
            .BuildReplayCandidatesFilter(nowUtc)
            .Render(new RenderArgs<LiveQualityIncidentDocument>(
                BsonSerializer.LookupSerializer<LiveQualityIncidentDocument>(),
                BsonSerializer.SerializerRegistry));
        string json = rendered.ToJson();

        Assert.Contains("UnmappedTarget", json, StringComparison.Ordinal);
        Assert.Contains("IneligibleMapping", json, StringComparison.Ordinal);
        Assert.Contains("InvalidFreshness", json, StringComparison.Ordinal);
        Assert.DoesNotContain("ProviderDiagnostic", json, StringComparison.Ordinal);
        Assert.Contains("expiresAtUtc", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildReplayCandidatesSort_ShouldRotatePreviouslyAttemptedIncidents()
    {
        BsonDocument rendered = LiveQualityIncidentMongoDefinitions
            .BuildReplayCandidatesSort()
            .Render(new RenderArgs<LiveQualityIncidentDocument>(
                BsonSerializer.LookupSerializer<LiveQualityIncidentDocument>(),
                BsonSerializer.SerializerRegistry));

        Assert.Equal(1, rendered["lastReplayAttemptAtUtc"].AsInt32);
        Assert.Equal(1, rendered["detectedAtUtc"].AsInt32);
    }
}
