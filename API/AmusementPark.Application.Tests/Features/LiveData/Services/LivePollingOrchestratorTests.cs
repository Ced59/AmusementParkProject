using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Core.Domain.LiveData;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LiveData.Services;

public sealed class LivePollingOrchestratorTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly LiveDataSourceId SourceId = LiveDataSourceId.Parse("pilot");

    [Fact]
    public async Task ExecuteAsync_WhenOperationalCollectionIsStopped_ShouldNotAcquireLeaseOrCallProvider()
    {
        Mock<ILivePollingStateRepository> repository =
            new Mock<ILivePollingStateRepository>(MockBehavior.Strict);
        Mock<ILiveDataProviderAdapter> adapter = CreateAdapter();
        LivePollingOrchestrator orchestrator = new LivePollingOrchestrator(
            new[] { adapter.Object },
            repository.Object,
            CreateIngestor().Object,
            CreateOperationalGate(collectionEnabled: false).Object,
            new FixedTimeProvider(NowUtc),
            static () => 0);

        LivePollingExecutionResult result = await orchestrator.ExecuteAsync(
            CreateTarget(),
            "worker-1",
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        Assert.Equal(LivePollingExecutionDisposition.Suspended, result.Disposition);
        repository.VerifyNoOtherCalls();
        adapter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenTargetIsNotDue_ShouldNotCallProvider()
    {
        Mock<ILivePollingStateRepository> repository = CreateRepositoryWithoutLease();
        Mock<ILiveDataProviderAdapter> adapter = CreateAdapter();
        LivePollingOrchestrator orchestrator = CreateOrchestrator(repository, adapter);

        LivePollingExecutionResult result = await orchestrator.ExecuteAsync(
            CreateTarget(),
            "worker-1",
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        Assert.Equal(LivePollingExecutionDisposition.NotDue, result.Disposition);
        adapter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAcquiringLease_ShouldPersistCrashRecoveryCooldown()
    {
        LivePollingLeaseRequest? savedRequest = null;
        Mock<ILivePollingStateRepository> repository =
            new Mock<ILivePollingStateRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.TryAcquireAsync(
                It.IsAny<LivePollingLeaseRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<LivePollingLeaseRequest, CancellationToken>(
                (request, _) => savedRequest = request)
            .ReturnsAsync((LivePollingLease?)null);
        Mock<ILiveDataProviderAdapter> adapter = CreateAdapter();
        LivePollingOrchestrator orchestrator = CreateOrchestrator(repository, adapter);

        await orchestrator.ExecuteAsync(
            CreateTarget(),
            "worker-1",
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        Assert.NotNull(savedRequest);
        Assert.Equal(TimeSpan.FromMinutes(5), savedRequest.CrashRecoveryCooldown);
    }

    [Fact]
    public async Task ExecuteAsync_OutsideActiveWindow_ShouldScheduleNextOpeningWithoutProviderCall()
    {
        DateTime nightUtc = new DateTime(2026, 9, 29, 2, 0, 0, DateTimeKind.Utc);
        Mock<ILivePollingStateRepository> repository = CreateRepositoryWithLease();
        LivePollingCompletion? savedCompletion = null;
        repository
            .Setup(value => value.CompleteAsync(
                It.IsAny<LivePollingCompletion>(),
                It.IsAny<CancellationToken>()))
            .Callback<LivePollingCompletion, CancellationToken>(
                (completion, _) => savedCompletion = completion)
            .ReturnsAsync(true);
        Mock<ILiveDataProviderAdapter> adapter = CreateAdapter();
        LivePollingOrchestrator orchestrator = new LivePollingOrchestrator(
            new[] { adapter.Object },
            repository.Object,
            CreateIngestor().Object,
            CreateOperationalGate().Object,
            new FixedTimeProvider(nightUtc),
            static () => 0);

        LivePollingExecutionResult result = await orchestrator.ExecuteAsync(
            CreateTarget(),
            "worker-1",
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        Assert.Equal(LivePollingExecutionDisposition.OutsideActiveWindow, result.Disposition);
        Assert.NotNull(savedCompletion);
        Assert.Equal(
            new DateTime(2026, 9, 29, 6, 0, 0, DateTimeKind.Utc),
            savedCompletion.NextAttemptAtUtc);
        adapter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_WhenLeaseAcquisitionCrossesClosingTime_ShouldUseFreshTime()
    {
        DateTime requestedAtUtc = new DateTime(2026, 9, 29, 22, 59, 59, DateTimeKind.Utc);
        DateTime acquiredAtUtc = new DateTime(2026, 9, 29, 23, 0, 0, DateTimeKind.Utc);
        LivePollingLeaseRequest? savedRequest = null;
        LivePollingCompletion? savedCompletion = null;
        Mock<ILivePollingStateRepository> repository = CreateRepositoryWithLease();
        repository
            .Setup(value => value.TryAcquireAsync(
                It.IsAny<LivePollingLeaseRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<LivePollingLeaseRequest, CancellationToken>(
                (request, _) => savedRequest = request)
            .ReturnsAsync(new LivePollingLease(
                SourceId,
                "entity-1",
                "worker-1",
                "lease-1",
                null,
                0,
                null));
        repository
            .Setup(value => value.CompleteAsync(
                It.IsAny<LivePollingCompletion>(),
                It.IsAny<CancellationToken>()))
            .Callback<LivePollingCompletion, CancellationToken>(
                (completion, _) => savedCompletion = completion)
            .ReturnsAsync(true);
        Mock<ILiveDataProviderAdapter> adapter = CreateAdapter();
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider
            .SetupSequence(static value => value.GetUtcNow())
            .Returns(new DateTimeOffset(requestedAtUtc))
            .Returns(new DateTimeOffset(acquiredAtUtc));
        LivePollingOrchestrator orchestrator = new LivePollingOrchestrator(
            new[] { adapter.Object },
            repository.Object,
            CreateIngestor().Object,
            CreateOperationalGate().Object,
            timeProvider.Object,
            static () => 0);

        LivePollingExecutionResult result = await orchestrator.ExecuteAsync(
            CreateTarget(),
            "worker-1",
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        Assert.Equal(LivePollingExecutionDisposition.OutsideActiveWindow, result.Disposition);
        Assert.NotNull(savedRequest);
        Assert.Equal(requestedAtUtc, savedRequest.NowUtc);
        Assert.NotNull(savedCompletion);
        Assert.Equal(acquiredAtUtc, savedCompletion.CompletedAtUtc);
        Assert.Equal(
            new DateTime(2026, 9, 30, 6, 0, 0, DateTimeKind.Utc),
            savedCompletion.NextAttemptAtUtc);
        adapter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_AfterSuccess_ShouldForwardEntityTagAndResetSchedule()
    {
        Mock<ILivePollingStateRepository> repository = CreateRepositoryWithLease(
            entityTag: "\"old\"",
            consecutiveFailures: 3);
        LivePollingCompletion? savedCompletion = null;
        repository
            .Setup(value => value.CompleteAsync(
                It.IsAny<LivePollingCompletion>(),
                It.IsAny<CancellationToken>()))
            .Callback<LivePollingCompletion, CancellationToken>(
                (completion, _) => savedCompletion = completion)
            .ReturnsAsync(true);
        Mock<ILiveDataProviderAdapter> adapter = CreateAdapter();
        adapter
            .Setup(value => value.FetchLatestAsync(
                It.Is<LiveProviderReadRequest>(request => request.EntityTag == "\"old\""),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveProviderReadResult(
                LiveProviderReadDisposition.Success,
                NowUtc,
                entityTag: "\"new\""));
        LivePollingOrchestrator orchestrator = CreateOrchestrator(repository, adapter);

        LivePollingExecutionResult result = await orchestrator.ExecuteAsync(
            CreateTarget(),
            "worker-1",
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        Assert.Equal(LivePollingExecutionDisposition.Success, result.Disposition);
        Assert.NotNull(savedCompletion);
        Assert.Equal(0, savedCompletion.ConsecutiveFailures);
        Assert.Equal(NowUtc.AddMinutes(5), savedCompletion.NextAttemptAtUtc);
        Assert.True(savedCompletion.ReplaceEntityTag);
        Assert.Equal("\"new\"", savedCompletion.EntityTag);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationalControlSuppressesData_ShouldClearEntityTag()
    {
        Mock<ILivePollingStateRepository> repository = CreateRepositoryWithLease(
            entityTag: "\"old\"");
        LivePollingCompletion? savedCompletion = null;
        repository
            .Setup(value => value.CompleteAsync(
                It.IsAny<LivePollingCompletion>(),
                It.IsAny<CancellationToken>()))
            .Callback<LivePollingCompletion, CancellationToken>(
                (completion, _) => savedCompletion = completion)
            .ReturnsAsync(true);
        Mock<ILiveDataProviderAdapter> adapter = CreateAdapter();
        adapter
            .Setup(value => value.FetchLatestAsync(
                It.IsAny<LiveProviderReadRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveProviderReadResult(
                LiveProviderReadDisposition.Success,
                NowUtc,
                entityTag: "\"new\""));
        Mock<ILiveLatestObservationIngestor> ingestor = CreateIngestor();
        ingestor
            .Setup(value => value.IngestAsync(
                It.IsAny<LiveLatestObservationIngestionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveLatestObservationIngestionResult(
                0,
                0,
                0,
                0,
                1,
                0,
                0,
                0));
        LivePollingOrchestrator orchestrator = CreateOrchestrator(
            repository,
            adapter,
            ingestor);

        LivePollingExecutionResult result = await orchestrator.ExecuteAsync(
            CreateTarget(),
            "worker-1",
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        Assert.Equal(LivePollingExecutionDisposition.Success, result.Disposition);
        Assert.NotNull(savedCompletion);
        Assert.True(savedCompletion.ReplaceEntityTag);
        Assert.Null(savedCompletion.EntityTag);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLatestPersistenceFails_ShouldFailWithoutReplacingEntityTag()
    {
        Mock<ILivePollingStateRepository> repository = CreateRepositoryWithLease(
            entityTag: "\"old\"");
        LivePollingCompletion? savedCompletion = null;
        repository
            .Setup(value => value.CompleteAsync(
                It.IsAny<LivePollingCompletion>(),
                It.IsAny<CancellationToken>()))
            .Callback<LivePollingCompletion, CancellationToken>(
                (completion, _) => savedCompletion = completion)
            .ReturnsAsync(true);
        Mock<ILiveDataProviderAdapter> adapter = CreateAdapter();
        adapter
            .Setup(value => value.FetchLatestAsync(
                It.IsAny<LiveProviderReadRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveProviderReadResult(
                LiveProviderReadDisposition.Success,
                NowUtc,
                entityTag: "\"new\""));
        Mock<ILiveLatestObservationIngestor> ingestor = CreateIngestor();
        ingestor
            .Setup(value => value.IngestAsync(
                It.IsAny<LiveLatestObservationIngestionRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Mongo unavailable"));
        LivePollingOrchestrator orchestrator = CreateOrchestrator(repository, adapter, ingestor);

        LivePollingExecutionResult result = await orchestrator.ExecuteAsync(
            CreateTarget(),
            "worker-1",
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        Assert.Equal(LivePollingExecutionDisposition.Failed, result.Disposition);
        Assert.NotNull(savedCompletion);
        Assert.False(savedCompletion.ReplaceEntityTag);
        Assert.Null(savedCompletion.EntityTag);
        Assert.Null(savedCompletion.LastSuccessfulPollAtUtc);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRateLimited_ShouldHonorRetryAfter()
    {
        Mock<ILivePollingStateRepository> repository = CreateRepositoryWithLease();
        LivePollingCompletion? savedCompletion = null;
        repository
            .Setup(value => value.CompleteAsync(
                It.IsAny<LivePollingCompletion>(),
                It.IsAny<CancellationToken>()))
            .Callback<LivePollingCompletion, CancellationToken>(
                (completion, _) => savedCompletion = completion)
            .ReturnsAsync(true);
        Mock<ILiveDataProviderAdapter> adapter = CreateAdapter();
        DateTime receivedAtUtc = NowUtc.AddSeconds(10);
        adapter
            .Setup(value => value.FetchLatestAsync(
                It.IsAny<LiveProviderReadRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveProviderReadResult(
                LiveProviderReadDisposition.RateLimited,
                receivedAtUtc,
                retryAfter: TimeSpan.FromMinutes(20)));
        LivePollingOrchestrator orchestrator = CreateOrchestrator(repository, adapter);

        LivePollingExecutionResult result = await orchestrator.ExecuteAsync(
            CreateTarget(),
            "worker-1",
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        Assert.Equal(LivePollingExecutionDisposition.RateLimited, result.Disposition);
        Assert.NotNull(savedCompletion);
        Assert.Equal(receivedAtUtc.AddMinutes(20), savedCompletion.NextAttemptAtUtc);
        Assert.Equal(1, savedCompletion.ConsecutiveFailures);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProviderThrowsAtThreshold_ShouldOpenCircuit()
    {
        Mock<ILivePollingStateRepository> repository = CreateRepositoryWithLease(
            consecutiveFailures: 4);
        LivePollingCompletion? savedCompletion = null;
        repository
            .Setup(value => value.CompleteAsync(
                It.IsAny<LivePollingCompletion>(),
                It.IsAny<CancellationToken>()))
            .Callback<LivePollingCompletion, CancellationToken>(
                (completion, _) => savedCompletion = completion)
            .ReturnsAsync(true);
        Mock<ILiveDataProviderAdapter> adapter = CreateAdapter();
        adapter
            .Setup(value => value.FetchLatestAsync(
                It.IsAny<LiveProviderReadRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Provider unavailable"));
        LivePollingOrchestrator orchestrator = CreateOrchestrator(repository, adapter);

        LivePollingExecutionResult result = await orchestrator.ExecuteAsync(
            CreateTarget(),
            "worker-1",
            TimeSpan.FromMinutes(1),
            CancellationToken.None);

        Assert.Equal(LivePollingExecutionDisposition.Failed, result.Disposition);
        Assert.True(result.CircuitOpened);
        Assert.NotNull(savedCompletion);
        Assert.Equal(NowUtc.AddMinutes(30), savedCompletion.CircuitOpenUntilUtc);
        Assert.Equal(NowUtc.AddHours(1), savedCompletion.NextAttemptAtUtc);
    }

    private static LivePollingOrchestrator CreateOrchestrator(
        Mock<ILivePollingStateRepository> repository,
        Mock<ILiveDataProviderAdapter> adapter,
        Mock<ILiveLatestObservationIngestor>? ingestor = null)
    {
        return new LivePollingOrchestrator(
            new[] { adapter.Object },
            repository.Object,
            (ingestor ?? CreateIngestor()).Object,
            CreateOperationalGate().Object,
            new FixedTimeProvider(NowUtc),
            static () => 0);
    }

    private static Mock<ILivePollingStateRepository> CreateRepositoryWithoutLease()
    {
        Mock<ILivePollingStateRepository> repository =
            new Mock<ILivePollingStateRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.TryAcquireAsync(
                It.IsAny<LivePollingLeaseRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((LivePollingLease?)null);
        return repository;
    }

    private static Mock<ILivePollingStateRepository> CreateRepositoryWithLease(
        string? entityTag = null,
        int consecutiveFailures = 0)
    {
        Mock<ILivePollingStateRepository> repository =
            new Mock<ILivePollingStateRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.TryAcquireAsync(
                It.IsAny<LivePollingLeaseRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LivePollingLease(
                SourceId,
                "entity-1",
                "worker-1",
                "lease-1",
                entityTag,
                consecutiveFailures,
                null));
        return repository;
    }

    private static Mock<ILiveDataProviderAdapter> CreateAdapter()
    {
        Mock<ILiveDataProviderAdapter> adapter =
            new Mock<ILiveDataProviderAdapter>(MockBehavior.Strict);
        adapter.SetupGet(static value => value.SourceId).Returns(SourceId);
        adapter.SetupGet(static value => value.AdapterVersion).Returns("adapter-1");
        adapter.SetupGet(static value => value.UsagePolicyVersion).Returns("policy-1");
        adapter.SetupGet(static value => value.TransformationVersion).Returns("transform-1");
        adapter.SetupGet(static value => value.Confidence).Returns(LiveDataConfidence.Medium);
        adapter.SetupGet(static value => value.FreshnessPolicy).Returns(
            new LiveFreshnessPolicy(
                "freshness-1",
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(1)));
        return adapter;
    }

    private static Mock<ILiveLatestObservationIngestor> CreateIngestor()
    {
        Mock<ILiveLatestObservationIngestor> ingestor =
            new Mock<ILiveLatestObservationIngestor>(MockBehavior.Strict);
        ingestor
            .Setup(value => value.IngestAsync(
                It.IsAny<LiveLatestObservationIngestionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LiveLatestObservationIngestionResult(0, 0, 0, 0, 0, 0, 0, 0));
        return ingestor;
    }

    private static Mock<ILiveOperationalGate> CreateOperationalGate(bool collectionEnabled = true)
    {
        Mock<ILiveOperationalGate> gate = new Mock<ILiveOperationalGate>(MockBehavior.Strict);
        gate.Setup(value => value.LoadAsync(
                It.IsAny<LiveDataSourceId>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((LiveDataSourceId _, string externalEntityId, CancellationToken _) =>
                new LiveOperationalGateSnapshot(
                    collectionEnabled,
                    true,
                    externalEntityId,
                    Array.Empty<LiveOperationalControl>(),
                    new LiveOperationalControlPolicy()));
        return gate;
    }

    private static LivePollingTarget CreateTarget()
    {
        return new LivePollingTarget(
            SourceId,
            "entity-1",
            new LivePollingActiveWindow(TimeZoneInfo.Utc, 6, 23),
            new LivePollingPolicy(
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromHours(1),
                5,
                TimeSpan.FromMinutes(30)),
            TimeSpan.Zero);
    }
}
