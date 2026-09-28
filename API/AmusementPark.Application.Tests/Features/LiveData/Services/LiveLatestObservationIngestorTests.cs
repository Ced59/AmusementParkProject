using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Core.Domain.LiveData;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LiveData.Services;

public sealed class LiveLatestObservationIngestorTests
{
    private static readonly DateTime ReceivedAtUtc =
        new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly LiveDataSourceId SourceId = LiveDataSourceId.Parse("source");

    [Fact]
    public async Task IngestAsync_ShouldResolveMappingsInOneBatchAndPersistCompleteProvenance()
    {
        ExternalLiveTargetMapping mapping = CreateVerifiedMapping("external-1", "item-1");
        Mock<ILiveTargetMappingRepository> mappings = CreateMappingRepository(
            new[] { mapping });
        IReadOnlyCollection<LiveLatestObservation>? saved = null;
        Mock<ILiveLatestObservationRepository> latest =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        latest
            .Setup(value => value.WriteLatestAsync(
                It.IsAny<IReadOnlyCollection<LiveLatestObservation>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<LiveLatestObservation>, CancellationToken>(
                (observations, _) => saved = observations)
            .ReturnsAsync(new LiveLatestObservationWriteResult(1, 0, 0));
        LiveLatestObservationIngestor ingestor = new LiveLatestObservationIngestor(
            mappings.Object,
            latest.Object,
            new FixedTimeProvider(ReceivedAtUtc.AddSeconds(1)));

        LiveLatestObservationIngestionResult result = await ingestor.IngestAsync(
            CreateRequest(
                CreateObservation("external-1", ReceivedAtUtc.AddMinutes(-1)),
                CreateObservation("unmapped", ReceivedAtUtc.AddMinutes(-1))),
            CancellationToken.None);

        Assert.Equal(1, result.PersistedCount);
        Assert.Equal(1, result.UnmappedCount);
        LiveLatestObservation persisted = Assert.Single(saved!);
        Assert.Equal("item-1", persisted.Target.Id);
        Assert.Equal(mapping.Version, persisted.Provenance.MappingVersion);
        Assert.Equal("adapter-1", persisted.Provenance.AdapterVersion);
        Assert.Equal("usage-1", persisted.Provenance.UsagePolicyVersion);
        Assert.Equal("transform-1", persisted.Provenance.TransformationVersion);
        Assert.Equal(0, Assert.Single(persisted.Queues).WaitTimeMinutes);
        mappings.Verify(value => value.GetLatestByExternalTargetIdsAsync(
            SourceId,
            It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_ShouldKeepNewestAliasAndRejectInactiveMapping()
    {
        ExternalLiveTargetMapping first = CreateVerifiedMapping("external-1", "item-1");
        ExternalLiveTargetMapping alias = CreateVerifiedMapping("external-2", "item-1");
        ExternalLiveTargetMapping suspended = CreateVerifiedMapping("external-3", "item-3")
            .Suspend("reviewer", "Source disabled", ReceivedAtUtc.AddMinutes(-2));
        Mock<ILiveTargetMappingRepository> mappings = CreateMappingRepository(
            new[] { first, alias, suspended });
        IReadOnlyCollection<LiveLatestObservation>? saved = null;
        Mock<ILiveLatestObservationRepository> latest =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        latest
            .Setup(value => value.WriteLatestAsync(
                It.IsAny<IReadOnlyCollection<LiveLatestObservation>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<LiveLatestObservation>, CancellationToken>(
                (observations, _) => saved = observations)
            .ReturnsAsync(new LiveLatestObservationWriteResult(0, 1, 0));
        LiveLatestObservationIngestor ingestor = new LiveLatestObservationIngestor(
            mappings.Object,
            latest.Object,
            new FixedTimeProvider(ReceivedAtUtc));

        LiveLatestObservationIngestionResult result = await ingestor.IngestAsync(
            CreateRequest(
                CreateObservation("external-1", ReceivedAtUtc.AddMinutes(-3)),
                CreateObservation("external-2", ReceivedAtUtc.AddMinutes(-1)),
                CreateObservation("external-3", ReceivedAtUtc.AddMinutes(-1))),
            CancellationToken.None);

        Assert.Equal(1, result.PersistedCount);
        Assert.Equal(1, result.IneligibleCount);
        LiveLatestObservation persisted = Assert.Single(saved!);
        Assert.Equal("external-2", persisted.Provenance.ExternalTargetId);
    }

    private static Mock<ILiveTargetMappingRepository> CreateMappingRepository(
        IReadOnlyCollection<ExternalLiveTargetMapping> returnedMappings)
    {
        Mock<ILiveTargetMappingRepository> repository =
            new Mock<ILiveTargetMappingRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetLatestByExternalTargetIdsAsync(
                SourceId,
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnedMappings);
        return repository;
    }

    private static LiveLatestObservationIngestionRequest CreateRequest(
        params ExternalLiveObservation[] observations)
    {
        return new LiveLatestObservationIngestionRequest(
            SourceId,
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
            ReceivedAtUtc,
            observations,
            new string('b', 64));
    }

    private static ExternalLiveObservation CreateObservation(
        string externalTargetId,
        DateTime observedAtUtc)
    {
        return new ExternalLiveObservation(
            externalTargetId,
            "Attraction",
            LiveTargetType.ParkItem,
            LiveOperationalStatus.Open,
            observedAtUtc,
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, 0, false) });
    }

    private static ExternalLiveTargetMapping CreateVerifiedMapping(
        string externalTargetId,
        string internalTargetId)
    {
        DateTime discoveredAtUtc = ReceivedAtUtc.AddMinutes(-5);
        ExternalLiveTargetMapping candidate = ExternalLiveTargetMapping.CreateCandidate(
            Guid.NewGuid(),
            SourceId,
            new ExternalLiveTargetDescriptor(
                LiveTargetType.ParkItem,
                externalTargetId,
                "external-park",
                "Attraction",
                "Park",
                "FR"),
            null,
            LiveMappingConfidence.Medium,
            discoveredAtUtc);
        return candidate.Verify(
            new LiveTargetReference(
                LiveTargetType.ParkItem,
                internalTargetId,
                "park-1",
                "Attraction",
                "Park",
                "FR"),
            "reviewer",
            null,
            ReceivedAtUtc.AddMinutes(-4));
    }
}
