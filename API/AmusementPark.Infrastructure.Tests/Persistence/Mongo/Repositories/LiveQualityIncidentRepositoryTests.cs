using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class LiveQualityIncidentRepositoryTests
{
    [Fact]
    public void BuildScopedReplayFilter_ShouldRestrictSourceAndConfiguredTargets()
    {
        FilterDefinition<LiveQualityIncidentDocument> filter =
            LiveQualityIncidentRepository.BuildScopedReplayFilter(
                LiveDataSourceId.Parse("source-current"),
                new[] { "target-a", "target-b", "target-a" },
                new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc));

        BsonDocument rendered = filter.Render(
            new RenderArgs<LiveQualityIncidentDocument>(
                BsonSerializer.LookupSerializer<LiveQualityIncidentDocument>(),
                BsonSerializer.SerializerRegistry));
        string json = rendered.ToJson();

        Assert.Contains("source-current", json, StringComparison.Ordinal);
        Assert.Contains("observation.externalTargetId", json, StringComparison.Ordinal);
        Assert.Contains("target-a", json, StringComparison.Ordinal);
        Assert.Contains("target-b", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildInsert_ShouldPreserveExactAuditTicksThroughBson()
    {
        DateTime receivedAtUtc =
            new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc).AddTicks(17);
        DateTime detectedAtUtc = receivedAtUtc.AddTicks(23);
        ExternalLiveObservation observation = new ExternalLiveObservation(
            "external-1",
            "Attraction",
            LiveTargetType.ParkItem,
            LiveOperationalStatus.Open,
            receivedAtUtc,
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, 15, false) });
        LiveQualityIncident incident = new LiveQualityIncident(
            Guid.NewGuid(),
            LiveDataSourceId.Parse("source"),
            observation,
            LiveQualityIncidentReason.UnmappedTarget,
            null,
            null,
            null,
            receivedAtUtc,
            detectedAtUtc,
            detectedAtUtc.AddDays(7),
            "correlation",
            "adapter-1",
            "usage-1",
            "transform-1",
            LiveDataConfidence.Medium,
            new LiveFreshnessPolicy(
                "freshness-1",
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(1)),
            new string('a', 64));
        LiveQualityIncidentDocument source = incident.ToDocument();

        BsonDocument update = LiveQualityIncidentRepository.BuildInsert(source)
            .Render(new RenderArgs<LiveQualityIncidentDocument>(
                BsonSerializer.LookupSerializer<LiveQualityIncidentDocument>(),
                BsonSerializer.SerializerRegistry))
            .AsBsonDocument;
        BsonDocument inserted = update["$setOnInsert"].AsBsonDocument;
        LiveQualityIncidentDocument persisted =
            BsonSerializer.Deserialize<LiveQualityIncidentDocument>(inserted);
        LiveQualityIncident restored = persisted.ToDomain();

        Assert.Equal(receivedAtUtc.Ticks, inserted["receivedAtUtcTicks"].AsInt64);
        Assert.Equal(detectedAtUtc.Ticks, inserted["detectedAtUtcTicks"].AsInt64);
        Assert.Equal(receivedAtUtc, restored.ReceivedAtUtc);
        Assert.Equal(detectedAtUtc, restored.DetectedAtUtc);
    }
}
