using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Passport;

internal sealed class VisitPurgeReconciliationBackgroundService : BackgroundService
{
    internal const int BatchSize = 50;
    internal static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<VisitPurgeReconciliationBackgroundService> logger;
    private readonly TimeProvider timeProvider;

    public VisitPurgeReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<VisitPurgeReconciliationBackgroundService> logger)
        : this(scopeFactory, logger, TimeProvider.System)
    {
    }

    internal VisitPurgeReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<VisitPurgeReconciliationBackgroundService> logger,
        TimeProvider timeProvider)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
        this.timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await this.ReconcileAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Unable to reconcile deleted visit purge scheduling.");
            }

            await Task.Delay(PollInterval, this.timeProvider, stoppingToken);
        }
    }

    internal async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = this.scopeFactory.CreateScope();
        IVisitDeletionStore deletionStore =
            scope.ServiceProvider.GetRequiredService<IVisitDeletionStore>();
        VisitPurgeScheduler scheduler =
            scope.ServiceProvider.GetRequiredService<VisitPurgeScheduler>();
        IReadOnlyCollection<VisitDeletionPurgeCandidate> candidates =
            await deletionStore.ListPendingPurgeSchedulingAsync(
                BatchSize,
                cancellationToken);
        foreach (VisitDeletionPurgeCandidate candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
                TimeSpan remainingDelay = candidate.PurgeScheduledForUtc - nowUtc;
                await scheduler.ScheduleAsync(
                    candidate.VisitId,
                    candidate.UserId,
                    candidate.DeletionVersion,
                    remainingDelay > TimeSpan.Zero ? remainingDelay : TimeSpan.Zero,
                    cancellationToken);
                _ = await deletionStore.MarkPurgeJobEnsuredAsync(
                    candidate.VisitId,
                    candidate.UserId,
                    candidate.DeletionVersion,
                    this.timeProvider.GetUtcNow().UtcDateTime,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Unable to ensure purge job for deleted visit {VisitId}.",
                    candidate.VisitId.Value);
            }
        }
    }
}
