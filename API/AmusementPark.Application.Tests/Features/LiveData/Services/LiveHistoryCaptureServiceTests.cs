using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Core.Domain.LiveData;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LiveData.Services;

public sealed class LiveHistoryCaptureServiceTests
{
    private static readonly DateTime ObservedAtUtc =
        new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly LiveDataSourceId SourceId = LiveDataSourceId.Parse("source");

    [Fact]
    public async Task CaptureAsync_WhenCurrentPolicyAllowsHistory_ShouldPersistEligibleObservation()
    {
        Mock<ILiveHistoryRepository> repository = new Mock<ILiveHistoryRepository>(
            MockBehavior.Strict);
        repository.Setup(value => value.StoreAsync(
                It.Is<IReadOnlyCollection<LiveLatestObservation>>(items => items.Count == 1),
                It.Is<LiveHistoryRetentionPolicy>(policy =>
                    policy.RawRetention == TimeSpan.FromDays(7)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        LiveHistoryCaptureService service = new LiveHistoryCaptureService(
            CreateCatalog(CreateSource(historicalStorageAllowed: true)).Object,
            repository.Object);

        int capturedCount = await service.CaptureAsync(
            new[] { CreateObservation("usage-1") },
            CancellationToken.None);

        Assert.Equal(1, capturedCount);
        repository.VerifyAll();
    }

    [Fact]
    public async Task OperationalWriter_ShouldCaptureValueCommittedByLatestStore()
    {
        LiveLatestObservation observation = CreateObservation("usage-1");
        Mock<ILiveHistoryRepository> historyRepository = new Mock<ILiveHistoryRepository>(
            MockBehavior.Strict);
        historyRepository.Setup(value => value.StoreAsync(
                It.Is<IReadOnlyCollection<LiveLatestObservation>>(items =>
                    items.Single() == observation),
                It.IsAny<LiveHistoryRetentionPolicy>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        LiveHistoryCaptureService historyCaptureService = new LiveHistoryCaptureService(
            CreateCatalog(CreateSource(historicalStorageAllowed: true)).Object,
            historyRepository.Object);
        Mock<ILiveLatestObservationRepository> latestRepository =
            new Mock<ILiveLatestObservationRepository>(MockBehavior.Strict);
        latestRepository.Setup(value => value.WriteLatestAsync(
                It.IsAny<IReadOnlyCollection<LiveLatestObservation>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveLatestObservationWriteResult(
                1,
                0,
                0,
                new[] { observation }));
        Mock<ILiveOperationalGate> gate = new Mock<ILiveOperationalGate>(MockBehavior.Strict);
        gate.Setup(value => value.LoadAsync(
                SourceId,
                "external-park",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveOperationalGateSnapshot(
                true,
                true,
                "external-park",
                Array.Empty<LiveOperationalControl>(),
                new LiveOperationalControlPolicy()));
        LiveOperationalObservationWriter writer = new LiveOperationalObservationWriter(
            latestRepository.Object,
            gate.Object,
            new LiveOperationalWriteCoordinator(),
            historyCaptureService);

        await writer.WriteAsync(
            SourceId,
            "external-park",
            new[] { observation },
            CancellationToken.None);

        historyRepository.VerifyAll();
    }

    [Theory]
    [InlineData(false, "usage-1")]
    [InlineData(true, "superseded-policy")]
    public async Task CaptureAsync_WhenStorageIsNotCurrentlyAuthorized_ShouldFailClosed(
        bool historicalStorageAllowed,
        string observationPolicyVersion)
    {
        Mock<ILiveHistoryRepository> repository = new Mock<ILiveHistoryRepository>(
            MockBehavior.Strict);
        LiveHistoryCaptureService service = new LiveHistoryCaptureService(
            CreateCatalog(CreateSource(historicalStorageAllowed)).Object,
            repository.Object);

        int capturedCount = await service.CaptureAsync(
            new[] { CreateObservation(observationPolicyVersion) },
            CancellationToken.None);

        Assert.Equal(0, capturedCount);
        repository.VerifyNoOtherCalls();
    }

    private static Mock<ILiveDataSourceCatalog> CreateCatalog(LiveDataSource source)
    {
        Mock<ILiveDataSourceCatalog> catalog = new Mock<ILiveDataSourceCatalog>(
            MockBehavior.Strict);
        catalog.Setup(value => value.Find(SourceId))
            .Returns(new LiveDataSourcePresentation(
                source,
                100,
                "Source attribution",
                "https://example.org/"));
        return catalog;
    }

    private static LiveDataSource CreateSource(bool historicalStorageAllowed)
    {
        return new LiveDataSource(
            SourceId,
            LiveDataSourceType.AuthorizedAggregator,
            "Source",
            new SourceUsagePolicy(
                "usage-1",
                "https://example.org/terms",
                true,
                historicalStorageAllowed,
                false,
                true,
                "live.source.attribution",
                ObservedAtUtc),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(30),
            LiveDataSourceStatus.Active,
            new LiveHistoryRetentionPolicy(
                TimeSpan.FromDays(7),
                TimeSpan.FromDays(400),
                TimeSpan.FromHours(1)));
    }

    private static LiveLatestObservation CreateObservation(string usagePolicyVersion)
    {
        return new LiveLatestObservation(
            new LiveTargetReference(
                LiveTargetType.ParkItem,
                "item-1",
                "park-1",
                "Attraction",
                "Park",
                "FR"),
            LiveOperationalStatus.Open,
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, 20, false) },
            new LiveObservationProvenance(
                SourceId,
                "external-1",
                ObservedAtUtc,
                ObservedAtUtc.AddSeconds(5),
                ObservedAtUtc.AddSeconds(5),
                "correlation",
                "adapter-1",
                "mapping-1",
                LiveDataConfidence.Medium,
                usagePolicyVersion,
                "transform-1"),
            new LiveFreshnessPolicy(
                "freshness-1",
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(1)),
            new string('a', 64));
    }
}
