using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.BackgroundJobs.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.BackgroundJobs.Services;

public sealed class DurableBackgroundJobExecutionOrchestratorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenHandlerIsUnknown_ShouldDeadLetterTheLeasedJob()
    {
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .Setup(item => item.DeadLetterAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                7,
                DurableBackgroundJobErrorCodes.UnknownKind,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTransition(DurableBackgroundJobStatus.DeadLetter));
        DurableBackgroundJobExecutionOrchestrator orchestrator = CreateOrchestrator(repository.Object);

        DurableBackgroundJobExecutionResult result = await orchestrator.ExecuteAsync(
            CreateLeasedJob(requestedRevision: 7),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobExecutionDisposition.DeadLettered, result.Disposition);
        Assert.Equal(DurableBackgroundJobErrorCodes.UnknownKind, result.ErrorCode);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHandlerSucceeds_ShouldCompleteTheAttemptedRevision()
    {
        StubHandler handler = CreateHandler(
            maximumAttempts: 3,
            static (_, _) => Task.FromResult(DurableBackgroundJobHandlerResult.Success()));
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .Setup(item => item.CompleteAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DurableBackgroundJobCompletionResult(
                "job-1",
                DurableBackgroundJobStatus.Succeeded,
                7,
                7));
        DurableBackgroundJobExecutionOrchestrator orchestrator = CreateOrchestrator(repository.Object, handler);

        DurableBackgroundJobExecutionResult result = await orchestrator.ExecuteAsync(
            CreateLeasedJob(requestedRevision: 7),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobExecutionDisposition.Completed, result.Disposition);
        Assert.Equal(DurableBackgroundJobStatus.Succeeded, result.PersistedStatus);
        repository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCompletionFindsANewerRevision_ShouldReportTheReplayState()
    {
        StubHandler handler = CreateHandler(
            maximumAttempts: 3,
            static (_, _) => Task.FromResult(DurableBackgroundJobHandlerResult.Success()));
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .Setup(item => item.CompleteAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DurableBackgroundJobCompletionResult(
                "job-1",
                DurableBackgroundJobStatus.Pending,
                8,
                7));
        DurableBackgroundJobExecutionOrchestrator orchestrator = CreateOrchestrator(repository.Object, handler);

        DurableBackgroundJobExecutionResult result = await orchestrator.ExecuteAsync(
            CreateLeasedJob(requestedRevision: 7),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobExecutionDisposition.RevisionReplayQueued, result.Disposition);
        Assert.Equal(DurableBackgroundJobStatus.Pending, result.PersistedStatus);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPayloadVersionIsUnsupported_ShouldDeadLetterWithoutCallingTheHandler()
    {
        int handlerCallCount = 0;
        StubHandler handler = CreateHandler(
            maximumAttempts: 3,
            (_, _) =>
            {
                handlerCallCount++;
                return Task.FromResult(DurableBackgroundJobHandlerResult.Success());
            });
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .Setup(item => item.DeadLetterAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                7,
                DurableBackgroundJobErrorCodes.UnsupportedPayloadVersion,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTransition(DurableBackgroundJobStatus.DeadLetter));
        DurableBackgroundJobExecutionOrchestrator orchestrator = CreateOrchestrator(repository.Object, handler);

        DurableBackgroundJobExecutionResult result = await orchestrator.ExecuteAsync(
            CreateLeasedJob(requestedRevision: 7, payloadVersion: 2),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobExecutionDisposition.DeadLettered, result.Disposition);
        Assert.Equal(0, handlerCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFailureIsTransient_ShouldScheduleABoundedRetry()
    {
        StubHandler handler = CreateHandler(
            maximumAttempts: 3,
            static (_, _) => Task.FromResult(DurableBackgroundJobHandlerResult.Retry("temporary")));
        TimeSpan capturedDelay = TimeSpan.Zero;
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .Setup(item => item.ScheduleRetryAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                7,
                It.IsAny<TimeSpan>(),
                "temporary",
                It.IsAny<CancellationToken>()))
            .Callback<DurableBackgroundJobLease, long?, TimeSpan, string, CancellationToken>(
                (_, _, delay, _, _) => capturedDelay = delay)
            .ReturnsAsync(CreateTransition(DurableBackgroundJobStatus.RetryScheduled));
        DurableBackgroundJobExecutionOrchestrator orchestrator = CreateOrchestrator(repository.Object, handler);

        DurableBackgroundJobExecutionResult result = await orchestrator.ExecuteAsync(
            CreateLeasedJob(requestedRevision: 7, attemptCount: 1),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobExecutionDisposition.RetryScheduled, result.Disposition);
        Assert.Equal(TimeSpan.FromSeconds(8), capturedDelay);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRetryFindsANewerRevision_ShouldReportTheReplayState()
    {
        StubHandler handler = CreateHandler(
            maximumAttempts: 3,
            static (_, _) => Task.FromResult(DurableBackgroundJobHandlerResult.Retry("temporary")));
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .Setup(item => item.ScheduleRetryAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                7,
                It.IsAny<TimeSpan>(),
                "temporary",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTransition(DurableBackgroundJobStatus.Pending, requestedRevision: 8));
        DurableBackgroundJobExecutionOrchestrator orchestrator = CreateOrchestrator(repository.Object, handler);

        DurableBackgroundJobExecutionResult result = await orchestrator.ExecuteAsync(
            CreateLeasedJob(requestedRevision: 7, attemptCount: 1),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobExecutionDisposition.RevisionReplayQueued, result.Disposition);
        Assert.Equal(DurableBackgroundJobStatus.Pending, result.PersistedStatus);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAttemptBudgetIsExhausted_ShouldDeadLetterTheRootError()
    {
        StubHandler handler = CreateHandler(
            maximumAttempts: 3,
            static (_, _) => Task.FromResult(DurableBackgroundJobHandlerResult.Retry("temporary")));
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .Setup(item => item.DeadLetterAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                7,
                "temporary",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTransition(DurableBackgroundJobStatus.DeadLetter));
        DurableBackgroundJobExecutionOrchestrator orchestrator = CreateOrchestrator(repository.Object, handler);

        DurableBackgroundJobExecutionResult result = await orchestrator.ExecuteAsync(
            CreateLeasedJob(requestedRevision: 7, attemptCount: 3),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobExecutionDisposition.DeadLettered, result.Disposition);
        repository.Verify(
            item => item.ScheduleRetryAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                It.IsAny<long?>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenARecoveredLeaseExceedsTheAttemptBudget_ShouldDeadLetterWithoutCallingTheHandler()
    {
        int handlerCallCount = 0;
        StubHandler handler = CreateHandler(
            maximumAttempts: 3,
            (_, _) =>
            {
                handlerCallCount++;
                return Task.FromResult(DurableBackgroundJobHandlerResult.Success());
            });
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .Setup(item => item.DeadLetterAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                7,
                DurableBackgroundJobErrorCodes.AttemptBudgetExhausted,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTransition(DurableBackgroundJobStatus.DeadLetter));
        DurableBackgroundJobExecutionOrchestrator orchestrator = CreateOrchestrator(repository.Object, handler);

        DurableBackgroundJobExecutionResult result = await orchestrator.ExecuteAsync(
            CreateLeasedJob(requestedRevision: 7, attemptCount: 4),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobExecutionDisposition.DeadLettered, result.Disposition);
        Assert.Equal(0, handlerCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHandlerReportsPermanentFailure_ShouldDeadLetterImmediately()
    {
        StubHandler handler = CreateHandler(
            maximumAttempts: 5,
            static (_, _) => Task.FromResult(DurableBackgroundJobHandlerResult.DeadLetter("invalid-payload")));
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .Setup(item => item.DeadLetterAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                7,
                "invalid-payload",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTransition(DurableBackgroundJobStatus.DeadLetter));
        DurableBackgroundJobExecutionOrchestrator orchestrator = CreateOrchestrator(repository.Object, handler);

        DurableBackgroundJobExecutionResult result = await orchestrator.ExecuteAsync(
            CreateLeasedJob(requestedRevision: 7, attemptCount: 1),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobExecutionDisposition.DeadLettered, result.Disposition);
        repository.Verify(
            item => item.ScheduleRetryAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                It.IsAny<long?>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHandlerTimesOut_ShouldScheduleRetry()
    {
        StubHandler handler = CreateHandler(
            maximumAttempts: 3,
            static async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return DurableBackgroundJobHandlerResult.Success();
            },
            timeout: TimeSpan.FromMilliseconds(20));
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .Setup(item => item.ScheduleRetryAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                7,
                It.IsAny<TimeSpan>(),
                DurableBackgroundJobErrorCodes.HandlerTimeout,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTransition(DurableBackgroundJobStatus.RetryScheduled));
        DurableBackgroundJobExecutionOrchestrator orchestrator = CreateOrchestrator(repository.Object, handler);

        DurableBackgroundJobExecutionResult result = await orchestrator.ExecuteAsync(
            CreateLeasedJob(requestedRevision: 7),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(30),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobExecutionDisposition.RetryScheduled, result.Disposition);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLeaseRenewalIsRejected_ShouldAbandonWithoutTransition()
    {
        StubHandler handler = CreateHandler(
            maximumAttempts: 3,
            static async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return DurableBackgroundJobHandlerResult.Success();
            },
            timeout: TimeSpan.FromSeconds(5));
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        repository
            .Setup(item => item.RenewLeaseAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                TimeSpan.FromSeconds(1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        DurableBackgroundJobExecutionOrchestrator orchestrator = CreateOrchestrator(repository.Object, handler);

        DurableBackgroundJobExecutionResult result = await orchestrator.ExecuteAsync(
            CreateLeasedJob(requestedRevision: 7),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobExecutionDisposition.LeaseLost, result.Disposition);
        repository.Verify(
            item => item.CompleteAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                It.IsAny<long?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHostStops_ShouldCancelTheHandlerWithoutPersistingSuccess()
    {
        TaskCompletionSource handlerStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        StubHandler handler = CreateHandler(
            maximumAttempts: 3,
            async (_, cancellationToken) =>
            {
                handlerStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return DurableBackgroundJobHandlerResult.Success();
            });
        Mock<IDurableBackgroundJobRepository> repository = new Mock<IDurableBackgroundJobRepository>();
        DurableBackgroundJobExecutionOrchestrator orchestrator = CreateOrchestrator(repository.Object, handler);
        using CancellationTokenSource stoppingSource = new CancellationTokenSource();

        Task<DurableBackgroundJobExecutionResult> execution = orchestrator.ExecuteAsync(
            CreateLeasedJob(requestedRevision: 7),
            TimeSpan.FromMinutes(2),
            TimeSpan.FromSeconds(30),
            stoppingSource.Token);
        await handlerStarted.Task;
        stoppingSource.Cancel();
        DurableBackgroundJobExecutionResult result = await execution;

        Assert.Equal(DurableBackgroundJobExecutionDisposition.Cancelled, result.Disposition);
        repository.Verify(
            item => item.CompleteAsync(
                It.IsAny<DurableBackgroundJobLease>(),
                It.IsAny<long?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static DurableBackgroundJobExecutionOrchestrator CreateOrchestrator(
        IDurableBackgroundJobRepository repository,
        params IDurableBackgroundJobHandler[] handlers)
    {
        DurableBackgroundJobHandlerRegistry registry = new DurableBackgroundJobHandlerRegistry(handlers);
        DurableBackgroundJobRetryDelayCalculator retryDelayCalculator =
            new DurableBackgroundJobRetryDelayCalculator(static () => 0.5);
        return new DurableBackgroundJobExecutionOrchestrator(
            repository,
            registry,
            retryDelayCalculator,
            NullLogger<DurableBackgroundJobExecutionOrchestrator>.Instance);
    }

    private static StubHandler CreateHandler(
        int maximumAttempts,
        Func<DurableBackgroundJobExecutionContext, CancellationToken, Task<DurableBackgroundJobHandlerResult>> execute,
        TimeSpan? timeout = null)
    {
        DurableBackgroundJobHandlerDefinition definition = new DurableBackgroundJobHandlerDefinition(
            "test.kind",
            DurableBackgroundJobWorkload.Light,
            new[] { 1 },
            timeout ?? TimeSpan.FromSeconds(5),
            maximumAttempts,
            TimeSpan.FromSeconds(8),
            TimeSpan.FromMinutes(1));
        return new StubHandler(definition, execute);
    }

    private static DurableBackgroundJob CreateLeasedJob(
        long? requestedRevision,
        int payloadVersion = 1,
        int attemptCount = 1)
    {
        using JsonDocument payload = JsonDocument.Parse("{\"scope\":\"parks:global\"}");
        DateTime nowUtc = new DateTime(2026, 8, 31, 20, 0, 0, DateTimeKind.Utc);
        return new DurableBackgroundJob(
            "job-1",
            "test.kind",
            "parks:global",
            null,
            payloadVersion,
            payload.RootElement.Clone(),
            requestedRevision,
            requestedRevision.HasValue ? requestedRevision - 1 : null,
            DurableBackgroundJobStatus.Leased,
            0,
            attemptCount,
            nowUtc,
            "worker-1",
            "lease-token-1",
            nowUtc.AddMinutes(2),
            nowUtc,
            nowUtc,
            null,
            null,
            "correlation-1");
    }

    private static DurableBackgroundJobStateTransitionResult CreateTransition(
        DurableBackgroundJobStatus status,
        long? requestedRevision = 7)
    {
        return new DurableBackgroundJobStateTransitionResult(
            "job-1",
            status,
            requestedRevision,
            requestedRevision.HasValue ? requestedRevision - 1 : null);
    }

    private sealed class StubHandler : IDurableBackgroundJobHandler
    {
        private readonly Func<DurableBackgroundJobExecutionContext, CancellationToken, Task<DurableBackgroundJobHandlerResult>> execute;

        public StubHandler(
            DurableBackgroundJobHandlerDefinition definition,
            Func<DurableBackgroundJobExecutionContext, CancellationToken, Task<DurableBackgroundJobHandlerResult>> execute)
        {
            this.Definition = definition;
            this.execute = execute;
        }

        public DurableBackgroundJobHandlerDefinition Definition { get; }

        public Task<DurableBackgroundJobHandlerResult> HandleAsync(
            DurableBackgroundJobExecutionContext context,
            CancellationToken cancellationToken)
        {
            return this.execute(context, cancellationToken);
        }
    }
}
