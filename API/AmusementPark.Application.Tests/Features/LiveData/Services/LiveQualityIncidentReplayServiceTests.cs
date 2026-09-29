using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Core.Domain.LiveData;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LiveData.Services;

public sealed class LiveQualityIncidentReplayServiceTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly LiveDataSourceId SourceId = LiveDataSourceId.Parse("source");

    [Fact]
    public async Task ReplayAsync_ShouldResolveCorrectedMappingAndPreserveProvenance()
    {
        LiveQualityIncident incident = CreateIncident();
        Mock<ILiveQualityIncidentRepository> incidents =
            new Mock<ILiveQualityIncidentRepository>(MockBehavior.Strict);
        incidents
            .Setup(value => value.GetReplayCandidatesAsync(
                SourceId,
                It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("external-1")),
                25,
                NowUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { incident });
        incidents
            .Setup(value => value.MarkResolvedAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == incident.Id),
                NowUtc,
                "admin-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        incidents
            .Setup(value => value.MarkReplayAttemptedAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == incident.Id),
                NowUtc,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ILiveTargetMappingRepository> mappings =
            new Mock<ILiveTargetMappingRepository>(MockBehavior.Strict);
        ExternalLiveTargetMapping mapping = CreateVerifiedMapping();
        SetupConfiguredMappings(mappings, new[] { mapping });
        mappings
            .Setup(value => value.GetLatestByExternalTargetIdsAsync(
                SourceId,
                It.Is<IReadOnlyCollection<string>>(ids => ids.Single() == "external-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mapping });
        LiveLatestObservation? stored = null;
        Mock<ILiveLatestObservationRepository> latest =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        latest
            .Setup(value => value.WriteLatestAsync(
                It.IsAny<IReadOnlyCollection<LiveLatestObservation>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<LiveLatestObservation>, CancellationToken>(
                (observations, _) => stored = observations.Single())
            .ReturnsAsync(new LiveLatestObservationWriteResult(1, 0, 0));
        LiveQualityIncidentReplayService service = new LiveQualityIncidentReplayService(
            incidents.Object,
            mappings.Object,
            latest.Object,
            CreateAllowingGate(),
            CreateCatalog(),
            new FixedTimeProvider(NowUtc));

        LiveQualityReplayResult result = await service.ReplayAsync(
            25,
            "admin-1",
            CancellationToken.None);

        Assert.Equal(1, result.ResolvedCount);
        Assert.Equal("item-1", stored!.Target.Id);
        Assert.Equal(incident.ReceivedAtUtc, stored.Provenance.ReceivedAtUtc);
        Assert.Equal(mapping.Version, stored.Provenance.MappingVersion);
    }

    [Fact]
    public async Task ReplayAsync_ShouldLeaveIncidentPendingWhileMappingIsStillMissing()
    {
        LiveQualityIncident incident = CreateIncident();
        Mock<ILiveQualityIncidentRepository> incidents =
            new Mock<ILiveQualityIncidentRepository>(MockBehavior.Strict);
        incidents
            .Setup(value => value.GetReplayCandidatesAsync(
                SourceId,
                It.IsAny<IReadOnlyCollection<string>>(),
                1,
                NowUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { incident });
        incidents
            .Setup(value => value.MarkResolvedAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 0),
                NowUtc,
                "admin-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        incidents
            .Setup(value => value.MarkReplayAttemptedAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == incident.Id),
                NowUtc,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        Mock<ILiveTargetMappingRepository> mappings =
            new Mock<ILiveTargetMappingRepository>(MockBehavior.Strict);
        SetupConfiguredMappings(mappings, new[] { CreateVerifiedMapping() });
        mappings
            .Setup(value => value.GetLatestByExternalTargetIdsAsync(
                SourceId,
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ExternalLiveTargetMapping>());
        Mock<ILiveLatestObservationRepository> latest =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        latest
            .Setup(value => value.WriteLatestAsync(
                It.Is<IReadOnlyCollection<LiveLatestObservation>>(values => values.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveLatestObservationWriteResult(0, 0, 0));
        LiveQualityIncidentReplayService service = new LiveQualityIncidentReplayService(
            incidents.Object,
            mappings.Object,
            latest.Object,
            CreateAllowingGate(),
            CreateCatalog(),
            new FixedTimeProvider(NowUtc));

        LiveQualityReplayResult result = await service.ReplayAsync(
            1,
            "admin-1",
            CancellationToken.None);

        Assert.Equal(0, result.ResolvedCount);
        Assert.Equal(1, result.StillBlockedCount);
    }

    [Fact]
    public async Task ReplayAsync_ShouldSelectCurrentEntityAliasDeterministically()
    {
        LiveQualityIncident aliasZ = CreateIncident(SourceId, "external-z");
        LiveQualityIncident aliasA = CreateIncident(SourceId, "external-a");
        LiveQualityIncident[] candidates = { aliasZ, aliasA };
        Mock<ILiveQualityIncidentRepository> incidents =
            new Mock<ILiveQualityIncidentRepository>(MockBehavior.Strict);
        incidents
            .Setup(value => value.GetReplayCandidatesAsync(
                SourceId,
                It.IsAny<IReadOnlyCollection<string>>(),
                10,
                NowUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);
        incidents
            .Setup(value => value.MarkReplayAttemptedAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
                NowUtc,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        incidents
            .Setup(value => value.MarkResolvedAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
                NowUtc,
                "admin-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        Mock<ILiveTargetMappingRepository> mappings =
            new Mock<ILiveTargetMappingRepository>(MockBehavior.Strict);
        SetupConfiguredMappings(mappings, new[]
        {
            CreateVerifiedMapping(SourceId, "external-z", "item-1"),
            CreateVerifiedMapping(SourceId, "external-a", "item-1"),
        });
        mappings
            .Setup(value => value.GetLatestByExternalTargetIdsAsync(
                SourceId,
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                CreateVerifiedMapping(SourceId, "external-z", "item-1"),
                CreateVerifiedMapping(SourceId, "external-a", "item-1"),
            });
        List<LiveLatestObservation> stored = new List<LiveLatestObservation>();
        Mock<ILiveLatestObservationRepository> latest =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        latest
            .Setup(value => value.WriteLatestAsync(
                It.IsAny<IReadOnlyCollection<LiveLatestObservation>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<LiveLatestObservation>, CancellationToken>(
                (observations, _) => stored.AddRange(observations))
            .ReturnsAsync((IReadOnlyCollection<LiveLatestObservation> observations, CancellationToken _) =>
                new LiveLatestObservationWriteResult(observations.Count, 0, 0));
        LiveQualityIncidentReplayService service = new LiveQualityIncidentReplayService(
            incidents.Object,
            mappings.Object,
            latest.Object,
            CreateAllowingGate(),
            CreateCatalog(),
            new FixedTimeProvider(NowUtc));

        LiveQualityReplayResult result = await service.ReplayAsync(
            10,
            "admin-1",
            CancellationToken.None);

        Assert.Equal(2, result.ResolvedCount);
        LiveLatestObservation selected = Assert.Single(stored);
        Assert.Equal("external-a", selected.Provenance.ExternalTargetId);
    }

    [Fact]
    public async Task ReplayAsync_WhenCollectionIsStopped_ShouldKeepIncidentPending()
    {
        LiveQualityIncident incident = CreateIncident();
        Mock<ILiveQualityIncidentRepository> incidents =
            new Mock<ILiveQualityIncidentRepository>(MockBehavior.Strict);
        incidents.Setup(value => value.GetReplayCandidatesAsync(
                SourceId,
                It.IsAny<IReadOnlyCollection<string>>(),
                1,
                NowUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { incident });
        incidents.Setup(value => value.MarkReplayAttemptedAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Single() == incident.Id),
                NowUtc,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        incidents.Setup(value => value.MarkResolvedAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 0),
                NowUtc,
                "admin-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        Mock<ILiveTargetMappingRepository> mappings =
            new Mock<ILiveTargetMappingRepository>(MockBehavior.Strict);
        SetupConfiguredMappings(mappings, new[] { CreateVerifiedMapping() });
        mappings.Setup(value => value.GetLatestByExternalTargetIdsAsync(
                SourceId,
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateVerifiedMapping() });
        Mock<ILiveLatestObservationRepository> latest =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        latest.Setup(value => value.WriteLatestAsync(
                It.Is<IReadOnlyCollection<LiveLatestObservation>>(values => values.Count == 0),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveLatestObservationWriteResult(0, 0, 0));
        Mock<ILiveOperationalGate> gate = new Mock<ILiveOperationalGate>(MockBehavior.Strict);
        gate.Setup(value => value.LoadAsync(
                SourceId,
                "external-park",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveOperationalGateSnapshot(
                false,
                true,
                "external-park",
                Array.Empty<LiveOperationalControl>(),
                new LiveOperationalControlPolicy()));
        LiveQualityIncidentReplayService service = new LiveQualityIncidentReplayService(
            incidents.Object,
            mappings.Object,
            latest.Object,
            gate.Object,
            CreateCatalog(),
            new FixedTimeProvider(NowUtc));

        LiveQualityReplayResult result = await service.ReplayAsync(
            1,
            "admin-1",
            CancellationToken.None);

        Assert.Equal(0, result.ResolvedCount);
        Assert.Equal(1, result.StillBlockedCount);
        Assert.Equal(0, result.PersistedCount);
    }

    private static LiveQualityIncident CreateIncident()
    {
        return CreateIncident(SourceId, "external-1");
    }

    private static ILiveDataSourceCatalog CreateCatalog()
    {
        Mock<ILiveDataSourceCatalog> catalog =
            new Mock<ILiveDataSourceCatalog>(MockBehavior.Strict);
        catalog.SetupGet(static value => value.ConfiguredPollingTarget)
            .Returns(new LivePollingTarget(
                SourceId,
                "external-park",
                new LivePollingActiveWindow(TimeZoneInfo.Utc, 6, 23),
                new LivePollingPolicy(
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromHours(1),
                    5,
                    TimeSpan.FromMinutes(30)),
                TimeSpan.Zero));
        return catalog.Object;
    }

    private static void SetupConfiguredMappings(
        Mock<ILiveTargetMappingRepository> mappings,
        IReadOnlyCollection<ExternalLiveTargetMapping> configuredMappings)
    {
        mappings.Setup(value => value.GetLatestByExternalEntityAsync(
                SourceId,
                "external-park",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuredMappings);
    }

    private static LiveQualityIncident CreateIncident(
        LiveDataSourceId sourceId,
        string externalTargetId)
    {
        ExternalLiveObservation observation = new ExternalLiveObservation(
            externalTargetId,
            "Attraction",
            LiveTargetType.ParkItem,
            LiveOperationalStatus.Open,
            NowUtc.AddMinutes(-2),
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, 15, false) });
        return new LiveQualityIncident(
            Guid.NewGuid(),
            sourceId,
            observation,
            LiveQualityIncidentReason.UnmappedTarget,
            null,
            null,
            null,
            NowUtc.AddMinutes(-1),
            NowUtc.AddMinutes(-1),
            NowUtc.AddDays(7),
            "correlation",
            "adapter-1",
            "usage-1",
            "transform-1",
            LiveDataConfidence.Medium,
            CreateFreshnessPolicy(),
            new string('b', 64));
    }

    private static ExternalLiveTargetMapping CreateVerifiedMapping()
    {
        return CreateVerifiedMapping(SourceId, "external-1", "item-1");
    }

    private static ExternalLiveTargetMapping CreateVerifiedMapping(
        LiveDataSourceId sourceId,
        string externalTargetId,
        string internalTargetId)
    {
        ExternalLiveTargetMapping candidate = ExternalLiveTargetMapping.CreateCandidate(
            Guid.NewGuid(),
            sourceId,
            new ExternalLiveTargetDescriptor(
                LiveTargetType.ParkItem,
                externalTargetId,
                "external-park",
                "Attraction",
                "Park",
                "FR"),
            null,
            LiveMappingConfidence.Medium,
            NowUtc.AddDays(-1));
        return candidate.Verify(
            new LiveTargetReference(
                LiveTargetType.ParkItem,
                internalTargetId,
                "park-1",
                "Attraction",
                "Park",
                "FR"),
            "admin-1",
            null,
            NowUtc.AddHours(-1));
    }

    private static LiveFreshnessPolicy CreateFreshnessPolicy()
    {
        return new LiveFreshnessPolicy(
            "freshness-1",
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(20),
            TimeSpan.FromMinutes(1));
    }

    private static ILiveOperationalGate CreateAllowingGate()
    {
        Mock<ILiveOperationalGate> gate = new Mock<ILiveOperationalGate>(MockBehavior.Strict);
        gate.Setup(value => value.LoadAsync(
                It.IsAny<LiveDataSourceId>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns<LiveDataSourceId, string, CancellationToken>(
                (sourceId, externalEntityId, _) => Task.FromResult(
                    new LiveOperationalGateSnapshot(
                        true,
                        true,
                        externalEntityId,
                        Array.Empty<LiveOperationalControl>(),
                        new LiveOperationalControlPolicy())));
        return gate.Object;
    }
}
