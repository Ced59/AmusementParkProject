using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
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
    public async Task IngestAsync_WhenTargetCollectionIsStopped_ShouldNotPersistObservation()
    {
        Mock<ILiveTargetMappingRepository> mappings = CreateMappingRepository(
            new[] { CreateVerifiedMapping("external-1", "item-1") });
        Mock<ILiveLatestObservationRepository> latest =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        latest.Setup(value => value.WriteLatestAsync(
                It.Is<IReadOnlyCollection<LiveLatestObservation>>(observations => observations.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveLatestObservationWriteResult(0, 0, 0));
        LiveLatestObservationIngestor ingestor = new LiveLatestObservationIngestor(
            mappings.Object,
            latest.Object,
            CreateIncidentRepository().Object,
            CreateOperationalGate(collectionEnabled: false).Object,
            new FixedTimeProvider(ReceivedAtUtc));

        LiveLatestObservationIngestionResult result = await ingestor.IngestAsync(
            CreateRequest(CreateObservation("external-1", ReceivedAtUtc.AddMinutes(-1))),
            CancellationToken.None);

        Assert.Equal(1, result.SuppressedByOperationalControlCount);
        Assert.Equal(0, result.PersistedCount);
    }

    [Fact]
    public async Task IngestAsync_WhenCollectionStopsBeforeWriteBoundary_ShouldSuppressObservation()
    {
        Mock<ILiveTargetMappingRepository> mappings = CreateMappingRepository(
            new[] { CreateVerifiedMapping("external-1", "item-1") });
        Mock<ILiveLatestObservationRepository> latest =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        latest.Setup(value => value.WriteLatestAsync(
                It.Is<IReadOnlyCollection<LiveLatestObservation>>(observations => observations.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveLatestObservationWriteResult(0, 0, 0));
        Mock<ILiveOperationalGate> gate = new Mock<ILiveOperationalGate>(MockBehavior.Strict);
        gate.SetupSequence(value => value.LoadAsync(
                SourceId,
                "external-park",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateGateSnapshot(collectionEnabled: true))
            .ReturnsAsync(CreateGateSnapshot(collectionEnabled: false));
        LiveLatestObservationIngestor ingestor = new LiveLatestObservationIngestor(
            mappings.Object,
            latest.Object,
            CreateIncidentRepository().Object,
            gate.Object,
            new FixedTimeProvider(ReceivedAtUtc));

        LiveLatestObservationIngestionResult result = await ingestor.IngestAsync(
            CreateRequest(CreateObservation("external-1", ReceivedAtUtc.AddMinutes(-1))),
            CancellationToken.None);

        Assert.Equal(1, result.SuppressedByOperationalControlCount);
        Assert.Equal(0, result.PersistedCount);
        gate.Verify(value => value.LoadAsync(
            SourceId,
            "external-park",
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

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
            CreateIncidentRepository().Object,
            CreateOperationalGate().Object,
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
            CreateIncidentRepository().Object,
            CreateOperationalGate().Object,
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

    [Fact]
    public async Task IngestAsync_ShouldRejectTimestampBeyondAcceptedFutureSkew()
    {
        ExternalLiveTargetMapping mapping = CreateVerifiedMapping("external-1", "item-1");
        Mock<ILiveTargetMappingRepository> mappings = CreateMappingRepository(new[] { mapping });
        Mock<ILiveLatestObservationRepository> latest =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        latest
            .Setup(value => value.WriteLatestAsync(
                It.Is<IReadOnlyCollection<LiveLatestObservation>>(observations =>
                    observations.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveLatestObservationWriteResult(0, 0, 0));
        LiveLatestObservationIngestor ingestor = new LiveLatestObservationIngestor(
            mappings.Object,
            latest.Object,
            CreateIncidentRepository().Object,
            CreateOperationalGate().Object,
            new FixedTimeProvider(ReceivedAtUtc));

        LiveLatestObservationIngestionResult result = await ingestor.IngestAsync(
            CreateRequest(CreateObservation("external-1", ReceivedAtUtc.AddYears(1))),
            CancellationToken.None);

        Assert.Equal(0, result.PersistedCount);
        Assert.Equal(1, result.InvalidFreshnessCount);
    }

    [Fact]
    public async Task IngestAsync_ShouldQuarantineConflictAndProviderDiagnostic()
    {
        ExternalLiveTargetMapping mapping = CreateVerifiedMapping("external-1", "item-1");
        Mock<ILiveTargetMappingRepository> mappings = CreateMappingRepository(new[] { mapping });
        Mock<ILiveLatestObservationRepository> latest =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        latest
            .Setup(value => value.WriteLatestAsync(
                It.Is<IReadOnlyCollection<LiveLatestObservation>>(observations =>
                    observations.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveLatestObservationWriteResult(0, 0, 0));
        IReadOnlyCollection<LiveQualityIncident>? saved = null;
        Mock<ILiveQualityIncidentRepository> incidents =
            new Mock<ILiveQualityIncidentRepository>(MockBehavior.Strict);
        incidents
            .Setup(value => value.SaveAsync(
                It.IsAny<IReadOnlyCollection<LiveQualityIncident>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<LiveQualityIncident>, CancellationToken>(
                (values, _) => saved = values)
            .Returns(Task.CompletedTask);
        LiveLatestObservationIngestor ingestor = new LiveLatestObservationIngestor(
            mappings.Object,
            latest.Object,
            incidents.Object,
            CreateOperationalGate().Object,
            new FixedTimeProvider(ReceivedAtUtc));
        ExternalLiveObservation conflict = new ExternalLiveObservation(
            "external-1",
            "Attraction",
            LiveTargetType.ParkItem,
            LiveOperationalStatus.Closed,
            ReceivedAtUtc.AddMinutes(-1),
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, 20, false) });
        LiveLatestObservationIngestionRequest request = CreateRequest(conflict) with
        {
            Diagnostics = new[]
            {
                new LiveProviderDiagnostic(
                    LiveProviderDiagnosticCodes.UnknownStatus,
                    "external-2",
                    "status"),
            },
        };

        LiveLatestObservationIngestionResult result = await ingestor.IngestAsync(
            request,
            CancellationToken.None);

        Assert.Equal(1, result.QuarantinedCount);
        Assert.Equal(1, result.DiagnosticIncidentCount);
        Assert.Collection(
            saved!.OrderBy(static incident => incident.Reason),
            incident => Assert.Equal(
                LiveQualityIncidentReason.StatusQueueConflict,
                incident.Reason),
            incident =>
            {
                Assert.Equal(LiveQualityIncidentReason.ProviderDiagnostic, incident.Reason);
                Assert.Equal("external-2", incident.DiagnosticExternalTargetId);
            });
    }

    [Fact]
    public async Task IngestAsync_ShouldNotWriteLatestWhenQuarantinePersistenceFails()
    {
        Mock<ILiveTargetMappingRepository> mappings = CreateMappingRepository(
            Array.Empty<ExternalLiveTargetMapping>());
        Mock<ILiveQualityIncidentRepository> incidents =
            new Mock<ILiveQualityIncidentRepository>(MockBehavior.Strict);
        incidents
            .Setup(value => value.SaveAsync(
                It.IsAny<IReadOnlyCollection<LiveQualityIncident>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("quarantine unavailable"));
        Mock<ILiveLatestObservationRepository> latest =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        LiveLatestObservationIngestor ingestor = new LiveLatestObservationIngestor(
            mappings.Object,
            latest.Object,
            incidents.Object,
            CreateOperationalGate().Object,
            new FixedTimeProvider(ReceivedAtUtc));

        await Assert.ThrowsAsync<InvalidOperationException>(() => ingestor.IngestAsync(
            CreateRequest(CreateObservation("unmapped", ReceivedAtUtc.AddMinutes(-1))),
            CancellationToken.None));

        latest.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IngestAsync_WhenHistoryFails_ShouldStillEvaluateAlertsThenReportFailure()
    {
        Mock<ILiveTargetMappingRepository> mappings = CreateMappingRepository(
            new[] { CreateVerifiedMapping("external-1", "item-1") });
        Mock<ILiveLatestObservationRepository> latest =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        latest.Setup(value => value.WriteLatestAsync(
                It.IsAny<IReadOnlyCollection<LiveLatestObservation>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                IReadOnlyCollection<LiveLatestObservation> observations,
                CancellationToken _) => new LiveLatestObservationWriteResult(
                    1,
                    0,
                    0,
                    observations));
        LiveDataSource source = new LiveDataSource(
            SourceId,
            LiveDataSourceType.AuthorizedAggregator,
            "Source",
            new SourceUsagePolicy(
                "usage-1",
                "https://example.org/terms",
                true,
                true,
                false,
                true,
                "live.source.attribution",
                ReceivedAtUtc),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(30),
            LiveDataSourceStatus.Active,
            new LiveHistoryRetentionPolicy(
                TimeSpan.FromDays(7),
                TimeSpan.FromDays(400),
                TimeSpan.FromHours(1)));
        Mock<ILiveDataSourceCatalog> catalog = new Mock<ILiveDataSourceCatalog>(
            MockBehavior.Strict);
        catalog.Setup(value => value.Find(SourceId))
            .Returns(new LiveDataSourcePresentation(
                source,
                100,
                "Source attribution",
                "https://example.org/"));
        catalog.SetupGet(value => value.PublicPollingTarget).Returns((LivePollingTarget?)null);
        catalog.SetupGet(value => value.IsPublicReadEnabled).Returns(false);
        Mock<ILiveHistoryRepository> history = new Mock<ILiveHistoryRepository>(
            MockBehavior.Strict);
        history.Setup(value => value.StoreAsync(
                It.IsAny<IReadOnlyCollection<LiveLatestObservation>>(),
                It.IsAny<LiveHistoryRetentionPolicy>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("history unavailable"));
        Mock<ILiveAlertSubscriptionRepository> subscriptions =
            new Mock<ILiveAlertSubscriptionRepository>(MockBehavior.Strict);
        Mock<ILiveAlertNotificationRepository> notifications =
            new Mock<ILiveAlertNotificationRepository>(MockBehavior.Strict);
        Mock<ILiveOperationalGate> gate = CreateOperationalGate();
        LiveLatestObservationIngestor ingestor = new LiveLatestObservationIngestor(
            mappings.Object,
            latest.Object,
            CreateIncidentRepository().Object,
            gate.Object,
            new LiveOperationalWriteCoordinator(),
            new LiveAlertEvaluationService(
                subscriptions.Object,
                notifications.Object,
                catalog.Object,
                gate.Object),
            new LiveHistoryCaptureService(catalog.Object, history.Object));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ingestor.IngestAsync(
                CreateRequest(CreateObservation("external-1", ReceivedAtUtc.AddMinutes(-1))),
                CancellationToken.None));

        Assert.Equal("history unavailable", exception.Message);
        catalog.VerifyGet(value => value.PublicPollingTarget, Times.Once);
        catalog.VerifyGet(value => value.IsPublicReadEnabled, Times.Once);
        subscriptions.VerifyNoOtherCalls();
        notifications.VerifyNoOtherCalls();
        history.VerifyAll();
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
            Array.Empty<LiveProviderDiagnostic>(),
            new string('b', 64));
    }

    private static Mock<ILiveQualityIncidentRepository> CreateIncidentRepository()
    {
        Mock<ILiveQualityIncidentRepository> repository =
            new Mock<ILiveQualityIncidentRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.SaveAsync(
                It.IsAny<IReadOnlyCollection<LiveQualityIncident>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return repository;
    }

    private static Mock<ILiveOperationalGate> CreateOperationalGate(bool collectionEnabled = true)
    {
        Mock<ILiveOperationalGate> gate = new Mock<ILiveOperationalGate>(MockBehavior.Strict);
        gate.Setup(value => value.LoadAsync(
                It.IsAny<LiveDataSourceId>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveDataSourceId _, string externalEntityId, CancellationToken _) =>
                CreateGateSnapshot(collectionEnabled, externalEntityId));
        return gate;
    }

    private static LiveOperationalGateSnapshot CreateGateSnapshot(
        bool collectionEnabled,
        string externalEntityId = "external-park")
    {
        return new LiveOperationalGateSnapshot(
            collectionEnabled,
            true,
            externalEntityId,
            Array.Empty<LiveOperationalControl>(),
            new LiveOperationalControlPolicy());
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
