using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Core.Domain.LiveData;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LiveData.Services;

public sealed class LiveOperationsDashboardReaderTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReadAsync_ShouldExposeConfiguredCeilingsAndOperationalState()
    {
        LiveDataSourceId sourceId = LiveDataSourceId.Parse("source");
        LivePollingTarget pollingTarget = new LivePollingTarget(
            sourceId,
            "external-park",
            new LivePollingActiveWindow(TimeZoneInfo.Utc, 6, 23),
            new LivePollingPolicy(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromHours(1),
                5,
                TimeSpan.FromMinutes(30)),
            TimeSpan.Zero);
        LiveDataSourcePresentation source = new LiveDataSourcePresentation(
            new LiveDataSource(
                sourceId,
                LiveDataSourceType.AuthorizedAggregator,
                "Source",
                new SourceUsagePolicy(
                    "policy-1",
                    "https://example.com/terms",
                    true,
                    true,
                    false,
                    true,
                    "source.attribution",
                    NowUtc),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(30),
                LiveDataSourceStatus.Active),
            100,
            "Source attribution",
            "https://example.com/");
        Mock<ILiveDataSourceCatalog> catalog = new Mock<ILiveDataSourceCatalog>(MockBehavior.Strict);
        catalog.SetupGet(static value => value.ConfiguredPollingTarget).Returns(pollingTarget);
        catalog.Setup(value => value.Find(sourceId)).Returns(source);
        Mock<ILiveDataProviderAdapter> adapter = new Mock<ILiveDataProviderAdapter>(MockBehavior.Strict);
        adapter.SetupGet(static value => value.SourceId).Returns(sourceId);
        adapter.SetupGet(static value => value.AdapterVersion).Returns("adapter-v1");
        adapter.SetupGet(static value => value.TransformationVersion).Returns("transform-v1");
        adapter.SetupGet(static value => value.UsagePolicyVersion).Returns("policy-1");
        Mock<ILiveTargetMappingRepository> mappings =
            new Mock<ILiveTargetMappingRepository>(MockBehavior.Strict);
        mappings.Setup(value => value.GetLatestByExternalEntityAsync(
                sourceId,
                "external-park",
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<ExternalLiveTargetMapping>());
        Mock<ILivePollingStateRepository> polling =
            new Mock<ILivePollingStateRepository>(MockBehavior.Strict);
        polling.Setup(value => value.GetAsync(sourceId, "external-park", CancellationToken.None))
            .ReturnsAsync((LivePollingStateSnapshot?)null);
        Mock<ILiveQualityIncidentRepository> incidents =
            new Mock<ILiveQualityIncidentRepository>(MockBehavior.Strict);
        incidents.Setup(value => value.CountPendingAsync(sourceId, NowUtc, CancellationToken.None))
            .ReturnsAsync(3);
        Mock<ILiveOperationalGate> gate = new Mock<ILiveOperationalGate>(MockBehavior.Strict);
        gate.Setup(value => value.LoadAsync(sourceId, "external-park", CancellationToken.None))
            .ReturnsAsync(new LiveOperationalGateSnapshot(
                false,
                true,
                "external-park",
                Array.Empty<LiveOperationalControl>(),
                new LiveOperationalControlPolicy()));
        LiveOperationsDashboardReader reader = new LiveOperationsDashboardReader(
            catalog.Object,
            new[] { adapter.Object },
            mappings.Object,
            polling.Object,
            incidents.Object,
            gate.Object,
            new LiveOperationalScopeResultFactory(),
            new FixedTimeProvider(NowUtc));

        AmusementPark.Application.Errors.ApplicationResult<LiveOperationsDashboardResult> result =
            await reader.ReadAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.ConfiguredCollectionEnabled);
        Assert.True(result.Value.ConfiguredPublicReadEnabled);
        Assert.Equal(3, result.Value.Summary.PendingIncidentCount);
        LiveOperationalScopeResult scope = Assert.Single(result.Value.Scopes);
        Assert.Equal(LiveOperationalScopeType.Source, scope.ScopeType);
        Assert.False(scope.EffectiveCollectionEnabled);
        Assert.True(scope.EffectivePublicReadEnabled);
    }
}
