using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LivePollingOrchestrator
{
    private readonly IReadOnlyCollection<ILiveDataProviderAdapter> adapters;
    private readonly ILivePollingStateRepository stateRepository;
    private readonly TimeProvider timeProvider;
    private readonly Func<double> jitterSample;

    public LivePollingOrchestrator(
        IEnumerable<ILiveDataProviderAdapter> adapters,
        ILivePollingStateRepository stateRepository)
        : this(adapters, stateRepository, TimeProvider.System, Random.Shared.NextDouble)
    {
    }

    internal LivePollingOrchestrator(
        IEnumerable<ILiveDataProviderAdapter> adapters,
        ILivePollingStateRepository stateRepository,
        TimeProvider timeProvider,
        Func<double> jitterSample)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        ArgumentNullException.ThrowIfNull(stateRepository);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(jitterSample);

        this.adapters = adapters.ToArray();
        this.stateRepository = stateRepository;
        this.timeProvider = timeProvider;
        this.jitterSample = jitterSample;
    }

    public async Task<LivePollingExecutionResult> ExecuteAsync(
        LivePollingTarget target,
        string leaseOwner,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);
        DateTime acquisitionRequestedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        LivePollingLease? lease = await this.stateRepository.TryAcquireAsync(
            new LivePollingLeaseRequest(
                target.SourceId,
                target.ExternalEntityId,
                leaseOwner,
                acquisitionRequestedAtUtc,
                leaseDuration,
                target.Policy.PollingInterval),
            cancellationToken);
        if (lease is null)
        {
            return new LivePollingExecutionResult(LivePollingExecutionDisposition.NotDue);
        }

        DateTime executionStartedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        TimeSpan jitter = this.CreateJitter(target.MaximumJitter);
        if (!target.ActiveWindow.Contains(executionStartedAtUtc))
        {
            DateTime nextOpeningUtc = target.ActiveWindow.GetNextOpeningUtc(executionStartedAtUtc).Add(jitter);
            LivePollingCompletion outsideWindow = new LivePollingCompletion(
                lease,
                LivePollingCompletionDisposition.OutsideActiveWindow,
                executionStartedAtUtc,
                nextOpeningUtc,
                lease.ConsecutiveFailures,
                lease.CircuitOpenUntilUtc,
                null,
                false,
                null);
            await this.EnsureCompletedAsync(outsideWindow, cancellationToken);
            return new LivePollingExecutionResult(LivePollingExecutionDisposition.OutsideActiveWindow);
        }

        ILiveDataProviderAdapter? adapter = this.adapters.FirstOrDefault(
            candidate => candidate.SourceId == target.SourceId);
        if (adapter is null)
        {
            return await this.CompleteFailureAsync(
                target,
                lease,
                executionStartedAtUtc,
                jitter,
                cancellationToken);
        }

        LiveProviderReadResult providerResult;
        try
        {
            providerResult = await adapter.FetchLatestAsync(
                new LiveProviderReadRequest(target.ExternalEntityId, lease.EntityTag),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return await this.CompleteFailureAsync(
                target,
                lease,
                executionStartedAtUtc,
                jitter,
                cancellationToken);
        }

        return await this.CompleteProviderResultAsync(
            target,
            lease,
            providerResult,
            jitter,
            cancellationToken);
    }

    private async Task<LivePollingExecutionResult> CompleteProviderResultAsync(
        LivePollingTarget target,
        LivePollingLease lease,
        LiveProviderReadResult providerResult,
        TimeSpan jitter,
        CancellationToken cancellationToken)
    {
        LivePollingAttemptOutcome outcome = MapOutcome(providerResult.Disposition);
        TimeSpan retryAfter = providerResult.RetryAfter ?? TimeSpan.Zero;
        LivePollingSchedule schedule = target.Policy.PlanNext(
            providerResult.ReceivedAtUtc,
            outcome,
            lease.ConsecutiveFailures,
            retryAfter,
            jitter);
        LivePollingCompletionDisposition completionDisposition = MapCompletionDisposition(
            providerResult.Disposition);
        bool successful = providerResult.Disposition is LiveProviderReadDisposition.Success
            or LiveProviderReadDisposition.NotModified;
        bool replaceEntityTag = providerResult.Disposition == LiveProviderReadDisposition.Success;
        LivePollingCompletion completion = new LivePollingCompletion(
            lease,
            completionDisposition,
            providerResult.ReceivedAtUtc,
            schedule.NextAttemptAtUtc,
            schedule.ConsecutiveFailures,
            schedule.CircuitOpenUntilUtc,
            successful ? providerResult.ReceivedAtUtc : null,
            replaceEntityTag,
            providerResult.EntityTag);
        await this.EnsureCompletedAsync(completion, cancellationToken);

        return new LivePollingExecutionResult(
            MapExecutionDisposition(providerResult.Disposition),
            providerResult.Observations.Count,
            providerResult.Diagnostics.Count,
            schedule.CircuitOpened);
    }

    private async Task<LivePollingExecutionResult> CompleteFailureAsync(
        LivePollingTarget target,
        LivePollingLease lease,
        DateTime attemptedAtUtc,
        TimeSpan jitter,
        CancellationToken cancellationToken)
    {
        LivePollingSchedule schedule = target.Policy.PlanNext(
            attemptedAtUtc,
            LivePollingAttemptOutcome.Failure,
            lease.ConsecutiveFailures,
            TimeSpan.Zero,
            jitter);
        LivePollingCompletion completion = new LivePollingCompletion(
            lease,
            LivePollingCompletionDisposition.Failed,
            attemptedAtUtc,
            schedule.NextAttemptAtUtc,
            schedule.ConsecutiveFailures,
            schedule.CircuitOpenUntilUtc,
            null,
            false,
            null);
        await this.EnsureCompletedAsync(completion, cancellationToken);
        return new LivePollingExecutionResult(
            LivePollingExecutionDisposition.Failed,
            circuitOpened: schedule.CircuitOpened);
    }

    private async Task EnsureCompletedAsync(
        LivePollingCompletion completion,
        CancellationToken cancellationToken)
    {
        bool completed = await this.stateRepository.CompleteAsync(completion, cancellationToken);
        if (!completed)
        {
            throw new InvalidOperationException("The live polling lease was lost before completion.");
        }
    }

    private TimeSpan CreateJitter(TimeSpan maximumJitter)
    {
        double sample = this.jitterSample();
        if (double.IsNaN(sample) || sample < 0 || sample > 1)
        {
            throw new InvalidOperationException("The live polling jitter sample must be between zero and one.");
        }

        return TimeSpan.FromTicks((long)(maximumJitter.Ticks * sample));
    }

    private static LivePollingAttemptOutcome MapOutcome(LiveProviderReadDisposition disposition)
    {
        return disposition switch
        {
            LiveProviderReadDisposition.Success => LivePollingAttemptOutcome.Success,
            LiveProviderReadDisposition.NotModified => LivePollingAttemptOutcome.NotModified,
            LiveProviderReadDisposition.RateLimited => LivePollingAttemptOutcome.RateLimited,
            _ => LivePollingAttemptOutcome.Failure,
        };
    }

    private static LivePollingCompletionDisposition MapCompletionDisposition(
        LiveProviderReadDisposition disposition)
    {
        return disposition switch
        {
            LiveProviderReadDisposition.Success => LivePollingCompletionDisposition.Success,
            LiveProviderReadDisposition.NotModified => LivePollingCompletionDisposition.NotModified,
            LiveProviderReadDisposition.RateLimited => LivePollingCompletionDisposition.RateLimited,
            _ => LivePollingCompletionDisposition.Failed,
        };
    }

    private static LivePollingExecutionDisposition MapExecutionDisposition(
        LiveProviderReadDisposition disposition)
    {
        return disposition switch
        {
            LiveProviderReadDisposition.Success => LivePollingExecutionDisposition.Success,
            LiveProviderReadDisposition.NotModified => LivePollingExecutionDisposition.NotModified,
            LiveProviderReadDisposition.RateLimited => LivePollingExecutionDisposition.RateLimited,
            _ => LivePollingExecutionDisposition.Failed,
        };
    }
}
