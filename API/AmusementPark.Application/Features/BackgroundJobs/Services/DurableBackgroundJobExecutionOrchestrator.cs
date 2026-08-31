using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Application.Features.BackgroundJobs.Services;

public sealed class DurableBackgroundJobExecutionOrchestrator
{
    private readonly IDurableBackgroundJobRepository repository;
    private readonly IDurableBackgroundJobHandlerResolver handlerResolver;
    private readonly DurableBackgroundJobRetryDelayCalculator retryDelayCalculator;
    private readonly ILogger<DurableBackgroundJobExecutionOrchestrator> logger;
    private readonly TimeProvider timeProvider;

    public DurableBackgroundJobExecutionOrchestrator(
        IDurableBackgroundJobRepository repository,
        IDurableBackgroundJobHandlerResolver handlerResolver,
        DurableBackgroundJobRetryDelayCalculator retryDelayCalculator,
        ILogger<DurableBackgroundJobExecutionOrchestrator> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(handlerResolver);
        ArgumentNullException.ThrowIfNull(retryDelayCalculator);
        ArgumentNullException.ThrowIfNull(logger);

        this.repository = repository;
        this.handlerResolver = handlerResolver;
        this.retryDelayCalculator = retryDelayCalculator;
        this.logger = logger;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<DurableBackgroundJobExecutionResult> ExecuteAsync(
        DurableBackgroundJob job,
        TimeSpan leaseDuration,
        TimeSpan leaseRenewalInterval,
        CancellationToken stoppingToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ValidateLeaseTimings(leaseDuration, leaseRenewalInterval);

        DurableBackgroundJobLease? lease = CreateLease(job);
        if (lease is null)
        {
            this.logger.LogWarning(
                "Durable background job {JobId} cannot be executed because its lease metadata is incomplete.",
                job.Id);
            return new DurableBackgroundJobExecutionResult(DurableBackgroundJobExecutionDisposition.LeaseLost);
        }

        if (!this.handlerResolver.TryResolve(job.Kind, out IDurableBackgroundJobHandler? handler) || handler is null)
        {
            return await this.ApplyDeadLetterAsync(
                job,
                lease,
                DurableBackgroundJobErrorCodes.UnknownKind,
                stoppingToken);
        }

        DurableBackgroundJobHandlerDefinition definition = handler.Definition;
        if (job.AttemptCount > definition.MaximumAttempts)
        {
            return await this.ApplyDeadLetterAsync(
                job,
                lease,
                DurableBackgroundJobErrorCodes.AttemptBudgetExhausted,
                stoppingToken);
        }

        if (!definition.SupportsPayloadVersion(job.PayloadVersion))
        {
            return await this.ApplyDeadLetterAsync(
                job,
                lease,
                DurableBackgroundJobErrorCodes.UnsupportedPayloadVersion,
                stoppingToken);
        }

        using CancellationTokenSource leaseLostSource = new CancellationTokenSource();
        using CancellationTokenSource leaseMonitorStopSource =
            CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        using CancellationTokenSource timeoutSource = new CancellationTokenSource();
        timeoutSource.CancelAfter(definition.Timeout);
        using CancellationTokenSource executionSource = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken,
            leaseLostSource.Token,
            timeoutSource.Token);

        Task leaseMonitor = this.MonitorLeaseAsync(
            lease,
            leaseDuration,
            leaseRenewalInterval,
            leaseMonitorStopSource.Token,
            leaseLostSource);
        DurableBackgroundJobHandlerResult? handlerResult = null;
        Task<DurableBackgroundJobHandlerResult>? handlerTask = null;
        bool timeoutObserved = false;

        try
        {
            DurableBackgroundJobExecutionContext context = new DurableBackgroundJobExecutionContext(
                job.Id,
                job.PayloadVersion,
                job.Payload,
                job.RequestedRevision,
                job.AttemptCount,
                job.CorrelationId);
            handlerTask = handler.HandleAsync(context, executionSource.Token);
            handlerResult = await handlerTask.WaitAsync(executionSource.Token);
            timeoutObserved = timeoutSource.IsCancellationRequested;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            this.ObserveDetachedHandler(handlerTask, job);
            return new DurableBackgroundJobExecutionResult(DurableBackgroundJobExecutionDisposition.Cancelled);
        }
        catch (OperationCanceledException) when (leaseLostSource.IsCancellationRequested)
        {
            this.ObserveDetachedHandler(handlerTask, job);
            return new DurableBackgroundJobExecutionResult(DurableBackgroundJobExecutionDisposition.LeaseLost);
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            this.ObserveDetachedHandler(handlerTask, job);
            timeoutObserved = true;
        }
        catch (OperationCanceledException exception)
        {
            this.logger.LogWarning(
                exception,
                "Durable background job {JobId} of kind {Kind} was cancelled by its handler.",
                job.Id,
                job.Kind);
            handlerResult = DurableBackgroundJobHandlerResult.Retry(
                DurableBackgroundJobErrorCodes.HandlerCancelled);
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Unhandled failure while executing durable background job {JobId} of kind {Kind} at attempt {AttemptCount}.",
                job.Id,
                job.Kind,
                job.AttemptCount);
            handlerResult = DurableBackgroundJobHandlerResult.Retry(
                DurableBackgroundJobErrorCodes.UnhandledException);
        }
        finally
        {
            timeoutSource.CancelAfter(Timeout.InfiniteTimeSpan);
            leaseMonitorStopSource.Cancel();
            await AwaitLeaseMonitorAsync(leaseMonitor);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return new DurableBackgroundJobExecutionResult(DurableBackgroundJobExecutionDisposition.Cancelled);
        }

        if (leaseLostSource.IsCancellationRequested)
        {
            return new DurableBackgroundJobExecutionResult(DurableBackgroundJobExecutionDisposition.LeaseLost);
        }

        if (timeoutObserved)
        {
            handlerResult = DurableBackgroundJobHandlerResult.Retry(
                DurableBackgroundJobErrorCodes.HandlerTimeout);
        }

        if (handlerResult is null)
        {
            handlerResult = DurableBackgroundJobHandlerResult.DeadLetter(
                DurableBackgroundJobErrorCodes.InvalidHandlerResult);
        }

        return await this.ApplyHandlerResultAsync(job, lease, definition, handlerResult, stoppingToken);
    }

    private async Task<DurableBackgroundJobExecutionResult> ApplyHandlerResultAsync(
        DurableBackgroundJob job,
        DurableBackgroundJobLease lease,
        DurableBackgroundJobHandlerDefinition definition,
        DurableBackgroundJobHandlerResult handlerResult,
        CancellationToken cancellationToken)
    {
        if (handlerResult.Outcome == DurableBackgroundJobHandlerOutcome.Succeeded)
        {
            return await this.ApplyCompletionAsync(job, lease, cancellationToken);
        }

        string errorCode = handlerResult.ErrorCode ?? DurableBackgroundJobErrorCodes.InvalidHandlerResult;
        if (handlerResult.Outcome == DurableBackgroundJobHandlerOutcome.DeadLetter ||
            job.AttemptCount >= definition.MaximumAttempts)
        {
            return await this.ApplyDeadLetterAsync(job, lease, errorCode, cancellationToken);
        }

        TimeSpan retryDelay = this.retryDelayCalculator.Calculate(definition, job.AttemptCount);
        try
        {
            DurableBackgroundJobStateTransitionResult? transition = await this.repository.ScheduleRetryAsync(
                lease,
                job.RequestedRevision,
                retryDelay,
                errorCode,
                cancellationToken);
            return transition is null
                ? new DurableBackgroundJobExecutionResult(DurableBackgroundJobExecutionDisposition.LeaseLost)
                : this.CreatePersistedTransitionResult(
                    job,
                    transition.Status,
                    DurableBackgroundJobStatus.RetryScheduled,
                    DurableBackgroundJobExecutionDisposition.RetryScheduled,
                    errorCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new DurableBackgroundJobExecutionResult(DurableBackgroundJobExecutionDisposition.Cancelled);
        }
        catch (Exception exception)
        {
            this.LogTransitionFailure(job, exception, "retry");
            return new DurableBackgroundJobExecutionResult(
                DurableBackgroundJobExecutionDisposition.TransitionFailed,
                ErrorCode: errorCode);
        }
    }

    private async Task<DurableBackgroundJobExecutionResult> ApplyCompletionAsync(
        DurableBackgroundJob job,
        DurableBackgroundJobLease lease,
        CancellationToken cancellationToken)
    {
        try
        {
            DurableBackgroundJobCompletionResult? completion = await this.repository.CompleteAsync(
                lease,
                job.RequestedRevision,
                cancellationToken);
            return completion is null
                ? new DurableBackgroundJobExecutionResult(DurableBackgroundJobExecutionDisposition.LeaseLost)
                : this.CreatePersistedTransitionResult(
                    job,
                    completion.Status,
                    DurableBackgroundJobStatus.Succeeded,
                    DurableBackgroundJobExecutionDisposition.Completed,
                    null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new DurableBackgroundJobExecutionResult(DurableBackgroundJobExecutionDisposition.Cancelled);
        }
        catch (Exception exception)
        {
            this.LogTransitionFailure(job, exception, "completion");
            return new DurableBackgroundJobExecutionResult(DurableBackgroundJobExecutionDisposition.TransitionFailed);
        }
    }

    private async Task<DurableBackgroundJobExecutionResult> ApplyDeadLetterAsync(
        DurableBackgroundJob job,
        DurableBackgroundJobLease lease,
        string errorCode,
        CancellationToken cancellationToken)
    {
        try
        {
            DurableBackgroundJobStateTransitionResult? transition = await this.repository.DeadLetterAsync(
                lease,
                job.RequestedRevision,
                errorCode,
                cancellationToken);
            return transition is null
                ? new DurableBackgroundJobExecutionResult(DurableBackgroundJobExecutionDisposition.LeaseLost)
                : this.CreatePersistedTransitionResult(
                    job,
                    transition.Status,
                    DurableBackgroundJobStatus.DeadLetter,
                    DurableBackgroundJobExecutionDisposition.DeadLettered,
                    errorCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new DurableBackgroundJobExecutionResult(DurableBackgroundJobExecutionDisposition.Cancelled);
        }
        catch (Exception exception)
        {
            this.LogTransitionFailure(job, exception, "dead-letter");
            return new DurableBackgroundJobExecutionResult(
                DurableBackgroundJobExecutionDisposition.TransitionFailed,
                ErrorCode: errorCode);
        }
    }

    private async Task MonitorLeaseAsync(
        DurableBackgroundJobLease lease,
        TimeSpan leaseDuration,
        TimeSpan leaseRenewalInterval,
        CancellationToken cancellationToken,
        CancellationTokenSource leaseLostSource)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(leaseRenewalInterval, this.timeProvider, cancellationToken);
                bool renewed = await this.repository.RenewLeaseAsync(
                    lease,
                    leaseDuration,
                    cancellationToken);
                if (!renewed)
                {
                    this.logger.LogWarning(
                        "Lease ownership was lost for durable background job {JobId}.",
                        lease.JobId);
                    leaseLostSource.Cancel();
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Lease renewal failed for durable background job {JobId}; execution will be abandoned.",
                lease.JobId);
            leaseLostSource.Cancel();
        }
    }

    private void LogTransitionFailure(DurableBackgroundJob job, Exception exception, string transition)
    {
        this.logger.LogError(
            exception,
            "Unable to persist {Transition} for durable background job {JobId} of kind {Kind}; the lease will expire for recovery.",
            transition,
            job.Id,
            job.Kind);
    }

    private void ObserveDetachedHandler(
        Task<DurableBackgroundJobHandlerResult>? handlerTask,
        DurableBackgroundJob job)
    {
        if (handlerTask is null || handlerTask.IsCompleted)
        {
            return;
        }

        _ = this.ObserveDetachedHandlerAsync(handlerTask, job);
    }

    private async Task ObserveDetachedHandlerAsync(
        Task<DurableBackgroundJobHandlerResult> handlerTask,
        DurableBackgroundJob job)
    {
        try
        {
            await handlerTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(
                exception,
                "Detached handler for durable background job {JobId} of kind {Kind} failed after its execution was abandoned.",
                job.Id,
                job.Kind);
        }
    }

    private DurableBackgroundJobExecutionResult CreatePersistedTransitionResult(
        DurableBackgroundJob job,
        DurableBackgroundJobStatus persistedStatus,
        DurableBackgroundJobStatus expectedStatus,
        DurableBackgroundJobExecutionDisposition expectedDisposition,
        string? errorCode)
    {
        if (persistedStatus == DurableBackgroundJobStatus.Pending)
        {
            return new DurableBackgroundJobExecutionResult(
                DurableBackgroundJobExecutionDisposition.RevisionReplayQueued,
                persistedStatus);
        }

        if (persistedStatus == expectedStatus)
        {
            return new DurableBackgroundJobExecutionResult(
                expectedDisposition,
                persistedStatus,
                errorCode);
        }

        this.logger.LogError(
            "Durable background job {JobId} of kind {Kind} reached unexpected status {PersistedStatus} after a state transition.",
            job.Id,
            job.Kind,
            persistedStatus);
        return new DurableBackgroundJobExecutionResult(
            DurableBackgroundJobExecutionDisposition.TransitionFailed,
            persistedStatus,
            errorCode);
    }

    private static DurableBackgroundJobLease? CreateLease(DurableBackgroundJob job)
    {
        if (job.Status != DurableBackgroundJobStatus.Leased ||
            string.IsNullOrWhiteSpace(job.LeaseOwner) ||
            string.IsNullOrWhiteSpace(job.LeaseToken))
        {
            return null;
        }

        return new DurableBackgroundJobLease(job.Id, job.LeaseOwner, job.LeaseToken);
    }

    private static void ValidateLeaseTimings(TimeSpan leaseDuration, TimeSpan leaseRenewalInterval)
    {
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        if (leaseRenewalInterval <= TimeSpan.Zero || leaseRenewalInterval >= leaseDuration)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseRenewalInterval));
        }
    }

    private static async Task AwaitLeaseMonitorAsync(Task leaseMonitor)
    {
        try
        {
            await leaseMonitor;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
