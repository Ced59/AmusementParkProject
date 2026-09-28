using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class LiveLatestObservationMongoDefinitionsTests
{
    private static readonly DateTime ObservedAtUtc =
        new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildIndexes_ShouldKeepOneLatestObservationPerSourceAndTarget()
    {
        IReadOnlyCollection<CreateIndexModel<LiveLatestObservationDocument>> indexes =
            LiveLatestObservationMongoDefinitions.BuildIndexes();

        CreateIndexModel<LiveLatestObservationDocument> unique = Assert.Single(
            indexes,
            static index => index.Options.Name == "idx_live_latest_source_target_unique");
        Assert.True(unique.Options.Unique);
        BsonDocument keys = unique.Keys.Render(
            new RenderArgs<LiveLatestObservationDocument>(
                BsonSerializer.LookupSerializer<LiveLatestObservationDocument>(),
                BsonSerializer.SerializerRegistry));
        Assert.Equal(1, keys["sourceId"].AsInt32);
        Assert.Equal(1, keys["target.type"].AsInt32);
        Assert.Equal(1, keys["target.id"].AsInt32);
    }

    [Fact]
    public void BuildMonotonicUpdate_ShouldCompareObservedThenReceivedTimestamp()
    {
        LiveLatestObservationDocument document = CreateObservation().ToDocument();

        BsonValue rendered = LiveLatestObservationMongoDefinitions
            .BuildMonotonicUpdate(document)
            .Render(new RenderArgs<LiveLatestObservationDocument>(
                BsonSerializer.LookupSerializer<LiveLatestObservationDocument>(),
                BsonSerializer.SerializerRegistry));
        string json = rendered.ToJson();

        Assert.Contains("$provenance.observedAtUtc", json, StringComparison.Ordinal);
        Assert.Contains("$provenance.receivedAtUtc", json, StringComparison.Ordinal);
        Assert.Contains("$_incomingIsNewer", json, StringComparison.Ordinal);
        Assert.Contains("$unset", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ToDocument_ShouldKeepZeroWaitAndFreshnessExpiration()
    {
        LiveLatestObservationDocument document = CreateObservation().ToDocument();

        Assert.Equal(0, Assert.Single(document.Queues).WaitTimeMinutes);
        Assert.Equal(ObservedAtUtc.AddMinutes(30), document.ExpiresAtUtc);
        Assert.Equal("freshness-1", document.FreshnessPolicy.Version);
        Assert.Equal(new string('c', 64), document.PayloadSha256);
    }

    private static LiveLatestObservation CreateObservation()
    {
        DateTime receivedAtUtc = ObservedAtUtc.AddSeconds(10);
        return new LiveLatestObservation(
            new LiveTargetReference(
                LiveTargetType.ParkItem,
                "item-1",
                "park-1",
                "Attraction",
                "Park",
                "FR"),
            LiveOperationalStatus.Open,
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, 0, false) },
            new LiveObservationProvenance(
                LiveDataSourceId.Parse("source"),
                "external-1",
                ObservedAtUtc,
                receivedAtUtc,
                receivedAtUtc,
                "correlation",
                "adapter-1",
                "mapping-1",
                LiveDataConfidence.Medium,
                "usage-1",
                "transform-1"),
            new LiveFreshnessPolicy(
                "freshness-1",
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(1)),
            new string('c', 64));
    }
}
