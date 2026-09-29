using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class LiveQualityIncidentMongoMapperTests
{
    [Fact]
    public void RoundTrip_ShouldPreserveReplayPayloadAndSubMillisecondTimestamp()
    {
        DateTime receivedAtUtc =
            new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc).AddTicks(17);
        LiveQualityIncident incident = CreateIncident(receivedAtUtc, "external:a");

        LiveQualityIncidentDocument document = incident.ToDocument();
        LiveQualityIncidentDocument persisted = BsonSerializer.Deserialize<LiveQualityIncidentDocument>(
            document.ToBson());
        LiveQualityIncident restored = persisted.ToDomain();

        Assert.Equal(incident.Id, restored.Id);
        Assert.Equal(incident.ReceivedAtUtc, restored.ReceivedAtUtc);
        Assert.Equal(incident.DetectedAtUtc, restored.DetectedAtUtc);
        Assert.Equal(incident.Observation!.SourceUpdatedAtUtc, restored.Observation!.SourceUpdatedAtUtc);
        Assert.Equal(17, document.Observation!.SourceUpdatedAtUtcTicks % TimeSpan.TicksPerSecond);
        Assert.Equal(incident.FreshnessPolicy.Version, restored.FreshnessPolicy.Version);
        Assert.Equal(incident.PayloadSha256, restored.PayloadSha256);
    }

    [Fact]
    public void ToDocument_ShouldUseUnambiguousDedupeKey()
    {
        DateTime receivedAtUtc = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
        LiveQualityIncident first = CreateIncident(receivedAtUtc, "a|1:b");
        LiveQualityIncident second = CreateIncident(receivedAtUtc, "a|1:b|");

        Assert.NotEqual(first.ToDocument().Id, second.ToDocument().Id);
    }

    [Fact]
    public void ToDocument_ShouldKeepDiagnosticRetryOnPayloadIdentity()
    {
        DateTime firstReceivedAtUtc = new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
        DateTime secondReceivedAtUtc = firstReceivedAtUtc.AddMinutes(5);
        string payloadSha256 = new string('b', 64);

        LiveQualityIncident first = CreateDiagnostic(firstReceivedAtUtc, payloadSha256);
        LiveQualityIncident retry = CreateDiagnostic(secondReceivedAtUtc, payloadSha256);
        LiveQualityIncident hashlessRetry = CreateDiagnostic(secondReceivedAtUtc, null);

        Assert.Equal(first.ToDocument().Id, retry.ToDocument().Id);
        Assert.NotEqual(retry.ToDocument().Id, hashlessRetry.ToDocument().Id);
    }

    private static LiveQualityIncident CreateIncident(
        DateTime receivedAtUtc,
        string externalTargetId)
    {
        ExternalLiveObservation observation = new ExternalLiveObservation(
            externalTargetId,
            "Attraction",
            LiveTargetType.ParkItem,
            LiveOperationalStatus.Open,
            receivedAtUtc,
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, 0, false) });
        return new LiveQualityIncident(
            Guid.NewGuid(),
            LiveDataSourceId.Parse("source"),
            observation,
            LiveQualityIncidentReason.UnmappedTarget,
            null,
            null,
            null,
            receivedAtUtc,
            receivedAtUtc,
            receivedAtUtc.AddDays(7),
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
    }

    private static LiveQualityIncident CreateDiagnostic(
        DateTime receivedAtUtc,
        string? payloadSha256)
    {
        return new LiveQualityIncident(
            Guid.NewGuid(),
            LiveDataSourceId.Parse("source"),
            null,
            LiveQualityIncidentReason.ProviderDiagnostic,
            "unknown-status",
            "external-1",
            "status",
            receivedAtUtc,
            receivedAtUtc,
            receivedAtUtc.AddDays(7),
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
            payloadSha256);
    }
}
