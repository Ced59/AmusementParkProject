using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.BackgroundJobs.Services;
using AmusementPark.Infrastructure.Configuration.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.BackgroundJobs;

internal sealed class DurableBackgroundJobWorkerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly DurableBackgroundJobWorkerSettings settings;
    private readonly DurableBackgroundJobMetrics metrics;
    private readonly ILogger<DurableBackgroundJobWorkerBackgroundService> logger;
    private readonly TimeProvider timeProvider;
    private readonly string workerInstanceId;

    public DurableBackgroundJobWorkerBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        DurableBackgroundJobWorkerSettings settings,
        DurableBackgroundJobMetrics metrics,
        ILogger<DurableBackgroundJobWorkerBackgroundService> logger)
        : this(serviceScopeFactory, settings, metrics, logger, TimeProvider.System)
    {
    }

    internal DurableBackgroundJobWorkerBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        DurableBackgroundJobWorkerSettings settings,
        DurableBackgroundJobMetrics metrics,
        ILogger<DurableBackgroundJobWorkerBackgroundService> logger,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.serviceScopeFactory = serviceScopeFactory;
        this.settings = settings;
        this.metrics = metrics;
        this.logger = logger;
        this.timeProvider = timeProvider;
        this.workerInstanceId = Guid.NewGuid().ToString("N");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!this.settings.Enabled)
        {
            this.logger.LogInformation("The durable background job worker is disabled by configuration.");
            return;
        }

        IReadOnlyCollection<DurableBackgroundJobHandlerDefinition> definitions = this.LoadDefinitions();
        DurableBackgroundJobClaimCoordinator claimCoordinator =
            new DurableBackgroundJobClaimCoordinator(definitions);
        List<Task> workers = new List<Task>();
        this.AddWorkers(
            workers,
            definitions,
            claimCoordinator,
            DurableBackgroundJobWorkload.Heavy,
            this.settings.HeavyWorkerCount,
            stoppingToken);
        this.AddWorkers(
            workers,
            definitions,
            claimCoordinator,
            DurableBackgroundJobWorkload.Light,
            this.settings.LightWorkerCount,
            stoppingToken);
        workers.Add(this.RunUnknownKindLoopAsync(claimCoordinator, stoppingToken));
        workers.Add(this.RunLeaseRecoveryLoopAsync(stoppingToken));

        if (definitions.Count == 0)
        {
            this.logger.LogInformation(
                "The durable background job worker is ready but no business handler is registered yet.");
        }

        await Task.WhenAll(workers);
    }

    private IReadOnlyCollection<DurableBackgroundJobHandlerDefinition> LoadDefinitions()
    {
        using IServiceScope scope = this.serviceScopeFactory.CreateScope();
        IDurableBackgroundJobHandlerResolver resolver =
            scope.ServiceProvider.GetRequiredService<IDurableBackgroundJobHandlerResolver>();
        return resolver.Definitions.ToArray();
    }

    private void AddWorkers(
        ICollection<Task> workers,
        IReadOnlyCollection<DurableBackgroundJobHandlerDefinition> definitions,
        DurableBackgroundJobClaimCoordinator claimCoordinator,
        DurableBackgroundJobWorkload workload,
        int workerCount,
        CancellationToken stoppingToken)
    {
        if (!definitions.Any(definition => definition.Workload == workload))
        {
            return;
        }

        for (int workerIndex = 0; workerIndex < workerCount; workerIndex++)
        {
            workers.Add(this.RunWorkerLoopAsync(claimCoordinator, workload, workerIndex, stoppingToken));
        }
    }

    private async Task RunWorkerLoopAsync(
        DurableBackgroundJobClaimCoordinator claimCoordinator,
        DurableBackgroundJobWorkload workload,
        int workerIndex,
        CancellationToken stoppingToken)
    {
        string leaseOwner = $"{this.workerInstanceId}:{workload.ToString().ToLowerInvariant()}:{workerIndex}";
        DurableBackgroundJobIdleBackoff idleBackoff = new DurableBackgroundJobIdleBackoff(
            this.settings.EmptyQueueInitialDelay,
            this.settings.EmptyQueueMaximumDelay,
            this.settings.EmptyQueueDelayMultiplier);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = this.serviceScopeFactory.CreateScope();
                IDurableBackgroundJobRepository repository =
                    scope.ServiceProvider.GetRequiredService<IDurableBackgroundJobRepository>();
                using DurableBackgroundJobClaim? claim = await claimCoordinator.TryClaimAsync(
                    repository,
                    workload,
                    leaseOwner,
                    this.settings.LeaseDuration,
                    stoppingToken);
                if (claim is null)
                {
                    await Task.Delay(idleBackoff.TakeNextDelay(), this.timeProvider, stoppingToken);
                    continue;
                }

                idleBackoff.Reset();
                await this.ExecuteClaimAsync(scope, claim, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "The {Workload} durable background job worker {WorkerIndex} failed; polling will resume with backoff.",
                    workload,
                    workerIndex);
                await DelayAfterFailureAsync(idleBackoff, this.timeProvider, stoppingToken);
            }
        }
    }

    private async Task RunUnknownKindLoopAsync(
        DurableBackgroundJobClaimCoordinator claimCoordinator,
        CancellationToken stoppingToken)
    {
        string leaseOwner = $"{this.workerInstanceId}:unknown:0";
        TimeSpan maximumIdleDelay = this.settings.LeaseRecoveryInterval > this.settings.EmptyQueueMaximumDelay
            ? this.settings.LeaseRecoveryInterval
            : this.settings.EmptyQueueMaximumDelay;
        DurableBackgroundJobIdleBackoff idleBackoff = new DurableBackgroundJobIdleBackoff(
            this.settings.EmptyQueueMaximumDelay,
            maximumIdleDelay,
            this.settings.EmptyQueueDelayMultiplier);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = this.serviceScopeFactory.CreateScope();
                IDurableBackgroundJobRepository repository =
                    scope.ServiceProvider.GetRequiredService<IDurableBackgroundJobRepository>();
                using DurableBackgroundJobClaim? claim = await claimCoordinator.TryClaimUnknownKindAsync(
                    repository,
                    leaseOwner,
                    this.settings.LeaseDuration,
                    this.settings.UnknownKindGracePeriod,
                    stoppingToken);
                if (claim is null)
                {
                    await Task.Delay(idleBackoff.TakeNextDelay(), this.timeProvider, stoppingToken);
                    continue;
                }

                idleBackoff.Reset();
                await this.ExecuteClaimAsync(scope, claim, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "The durable background job unknown-kind worker failed; polling will resume with backoff.");
                await DelayAfterFailureAsync(idleBackoff, this.timeProvider, stoppingToken);
            }
        }
    }

    private async Task ExecuteClaimAsync(
        IServiceScope scope,
        DurableBackgroundJobClaim claim,
        CancellationToken stoppingToken)
    {
        DurableBackgroundJobExecutionOrchestrator orchestrator =
            scope.ServiceProvider.GetRequiredService<DurableBackgroundJobExecutionOrchestrator>();
        long startedAt = this.timeProvider.GetTimestamp();
        DurableBackgroundJobExecutionResult result = await orchestrator.ExecuteAsync(
            claim.Job,
            this.settings.LeaseDuration,
            this.settings.LeaseRenewalInterval,
            stoppingToken);
        TimeSpan elapsed = this.timeProvider.GetElapsedTime(startedAt);
        this.metrics.RecordExecution(claim.Job, result, elapsed);
        this.logger.LogInformation(
            "Durable background job {JobId} of kind {Kind} finished with {Disposition} at attempt {AttemptCount} in {ElapsedMilliseconds} ms.",
            claim.Job.Id,
            claim.Job.Kind,
            result.Disposition,
            claim.Job.AttemptCount,
            elapsed.TotalMilliseconds);
    }

    private async Task RunLeaseRecoveryLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = this.serviceScopeFactory.CreateScope();
                IDurableBackgroundJobRepository repository =
                    scope.ServiceProvider.GetRequiredService<IDurableBackgroundJobRepository>();
                int recoveredCount = await repository.ReleaseExpiredLeasesAsync(
                    this.settings.LeaseRecoveryBatchSize,
                    stoppingToken);
                this.metrics.RecordRecoveredLeases(recoveredCount);
                if (recoveredCount > 0)
                {
                    this.logger.LogWarning(
                        "Recovered {RecoveredCount} expired durable background job leases.",
                        recoveredCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Unable to recover expired durable background job leases.");
            }

            try
            {
                await Task.Delay(this.settings.LeaseRecoveryInterval, this.timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private static async Task DelayAfterFailureAsync(
        DurableBackgroundJobIdleBackoff idleBackoff,
        TimeProvider timeProvider,
        CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(idleBackoff.TakeNextDelay(), timeProvider, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
