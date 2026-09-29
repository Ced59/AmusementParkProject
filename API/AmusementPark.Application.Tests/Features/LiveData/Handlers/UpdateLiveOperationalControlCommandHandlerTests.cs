using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Handlers;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Tests.Features.LiveData.Services;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LiveData.Handlers;

public sealed class UpdateLiveOperationalControlCommandHandlerTests
{
    private static readonly LiveDataSourceId SourceId = LiveDataSourceId.Parse("source");
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 29, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_WhenCreatingSourceStop_ShouldAppendAuditedRevision()
    {
        LiveOperationalControl? saved = null;
        Mock<ILiveOperationalControlRepository> repository =
            new Mock<ILiveOperationalControlRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetLatestAsync(
                It.IsAny<LiveOperationalControlScope>(),
                CancellationToken.None))
            .ReturnsAsync((LiveOperationalControl?)null);
        repository.Setup(value => value.AppendRevisionAsync(
                It.IsAny<LiveOperationalControl>(),
                0,
                CancellationToken.None))
            .Callback<LiveOperationalControl, int, CancellationToken>(
                (control, _, _) => saved = control)
            .ReturnsAsync(LiveOperationalControlWriteOutcome.Created);
        Mock<ILiveDataSourceCatalog> catalog = CreateCatalog();
        Mock<ILiveOperationalGate> gate = new Mock<ILiveOperationalGate>(MockBehavior.Strict);
        gate.Setup(value => value.LoadAsync(SourceId, "external-park", CancellationToken.None))
            .ReturnsAsync(() => new LiveOperationalGateSnapshot(
                true,
                true,
                "external-park",
                saved is null ? Array.Empty<LiveOperationalControl>() : new[] { saved },
                new LiveOperationalControlPolicy()));
        UpdateLiveOperationalControlCommandHandler handler = new UpdateLiveOperationalControlCommandHandler(
            repository.Object,
            new Mock<ILiveTargetMappingRepository>(MockBehavior.Strict).Object,
            catalog.Object,
            new LiveTargetReferenceResolver(
                new Mock<IParkRepository>(MockBehavior.Strict).Object,
                new Mock<IParkItemRepository>(MockBehavior.Strict).Object),
            gate.Object,
            new LiveOperationalScopeResultFactory(),
            new FixedTimeProvider(NowUtc));

        AmusementPark.Application.Errors.ApplicationResult<LiveOperationalScopeResult> result =
            await handler.HandleAsync(
                new UpdateLiveOperationalControlCommand(
                    LiveOperationalScopeType.Source,
                    SourceId.Value,
                    null,
                    null,
                    null,
                    null,
                    false,
                    false,
                    0,
                    "Provider incident",
                    "admin-1"),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(saved);
        Assert.Equal(1, saved.Revision);
        Assert.Equal("admin-1", saved.ChangedByUserId);
        Assert.Equal("Provider incident", saved.Reason);
        Assert.False(result.Value!.EffectiveCollectionEnabled);
        Assert.False(result.Value.EffectivePublicReadEnabled);
        gate.Verify(value => value.LoadAsync(
            SourceId,
            "external-park",
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenRequestIsCancelledAfterAppend_ShouldCompleteCommittedMutation()
    {
        using CancellationTokenSource requestCancellation = new CancellationTokenSource();
        LiveOperationalControl? saved = null;
        Mock<ILiveOperationalControlRepository> repository =
            new Mock<ILiveOperationalControlRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetLatestAsync(
                It.IsAny<LiveOperationalControlScope>(),
                CancellationToken.None))
            .ReturnsAsync((LiveOperationalControl?)null);
        repository.Setup(value => value.AppendRevisionAsync(
                It.IsAny<LiveOperationalControl>(),
                0,
                CancellationToken.None))
            .Callback<LiveOperationalControl, int, CancellationToken>((control, _, _) =>
            {
                saved = control;
                requestCancellation.Cancel();
            })
            .ReturnsAsync(LiveOperationalControlWriteOutcome.Created);
        Mock<ILiveOperationalGate> gate = new Mock<ILiveOperationalGate>(MockBehavior.Strict);
        gate.Setup(value => value.LoadAsync(SourceId, "external-park", CancellationToken.None))
            .ReturnsAsync(() => new LiveOperationalGateSnapshot(
                true,
                true,
                "external-park",
                saved is null ? Array.Empty<LiveOperationalControl>() : new[] { saved },
                new LiveOperationalControlPolicy()));
        UpdateLiveOperationalControlCommandHandler handler =
            new UpdateLiveOperationalControlCommandHandler(
                repository.Object,
                new Mock<ILiveTargetMappingRepository>(MockBehavior.Strict).Object,
                CreateCatalog().Object,
                new LiveTargetReferenceResolver(
                    new Mock<IParkRepository>(MockBehavior.Strict).Object,
                    new Mock<IParkItemRepository>(MockBehavior.Strict).Object),
                gate.Object,
                new LiveOperationalScopeResultFactory(),
                new FixedTimeProvider(NowUtc));

        AmusementPark.Application.Errors.ApplicationResult<LiveOperationalScopeResult> result =
            await handler.HandleAsync(
                new UpdateLiveOperationalControlCommand(
                    LiveOperationalScopeType.Source,
                    SourceId.Value,
                    null,
                    null,
                    null,
                    null,
                    false,
                    false,
                    0,
                    "Provider incident",
                    "admin-1"),
                requestCancellation.Token);

        Assert.True(requestCancellation.IsCancellationRequested);
        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.EffectiveCollectionEnabled);
        gate.Verify(value => value.LoadAsync(
            SourceId,
            "external-park",
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenOnlyParkItemsAreMapped_ShouldAllowParkControl()
    {
        Mock<ILiveOperationalControlRepository> repository =
            new Mock<ILiveOperationalControlRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetLatestAsync(
                It.IsAny<LiveOperationalControlScope>(),
                CancellationToken.None))
            .ReturnsAsync((LiveOperationalControl?)null);
        repository.Setup(value => value.AppendRevisionAsync(
                It.Is<LiveOperationalControl>(control =>
                    control.Scope.Type == LiveOperationalScopeType.Park
                    && control.Scope.InternalParkId == "park-1"),
                0,
                CancellationToken.None))
            .ReturnsAsync(LiveOperationalControlWriteOutcome.Created);
        Mock<ILiveTargetMappingRepository> mappings =
            new Mock<ILiveTargetMappingRepository>(MockBehavior.Strict);
        mappings.Setup(value => value.GetEligiblePublicTargetCoverageByParkAsync(
                SourceId,
                "external-park",
                "park-1",
                CancellationToken.None))
            .ReturnsAsync(new[] { new LivePublicTargetCoverage("item-1", "external-item") });
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(value => value.GetByIdAsync("park-1", true, CancellationToken.None))
            .ReturnsAsync(new Park
            {
                Id = "park-1",
                Name = "Park",
                CountryCode = "FR",
                IsVisible = true,
            });
        Mock<ILiveOperationalGate> gate = new Mock<ILiveOperationalGate>(MockBehavior.Strict);
        gate.Setup(value => value.LoadAsync(SourceId, "external-park", CancellationToken.None))
            .ReturnsAsync(new LiveOperationalGateSnapshot(
                true,
                true,
                "external-park",
                Array.Empty<LiveOperationalControl>(),
                new LiveOperationalControlPolicy()));
        UpdateLiveOperationalControlCommandHandler handler =
            new UpdateLiveOperationalControlCommandHandler(
                repository.Object,
                mappings.Object,
                CreateCatalog().Object,
                new LiveTargetReferenceResolver(
                    parks.Object,
                    new Mock<IParkItemRepository>(MockBehavior.Strict).Object),
                gate.Object,
                new LiveOperationalScopeResultFactory(),
                new FixedTimeProvider(NowUtc));

        AmusementPark.Application.Errors.ApplicationResult<LiveOperationalScopeResult> result =
            await handler.HandleAsync(
                new UpdateLiveOperationalControlCommand(
                    LiveOperationalScopeType.Park,
                    SourceId.Value,
                    "external-park",
                    "park-1",
                    null,
                    null,
                    false,
                    false,
                    0,
                    "Park incident",
                    "admin-1"),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Park", result.Value!.DisplayName);
        Assert.False(result.Value.EffectiveCollectionEnabled);
    }

    private static Mock<ILiveDataSourceCatalog> CreateCatalog()
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
        catalog.Setup(value => value.Find(SourceId)).Returns(new LiveDataSourcePresentation(
            new LiveDataSource(
                SourceId,
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
            "Source",
            "https://example.com/"));
        return catalog;
    }
}
