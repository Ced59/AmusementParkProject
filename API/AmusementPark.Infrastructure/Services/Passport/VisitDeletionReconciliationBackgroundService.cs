using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Passport.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Passport;

internal sealed class VisitDeletionReconciliationBackgroundService : BackgroundService
{
    internal const int BatchSize = 50;
    internal static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<VisitDeletionReconciliationBackgroundService> logger;
    private readonly TimeProvider timeProvider;

    public VisitDeletionReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<VisitDeletionReconciliationBackgroundService> logger)
        : this(scopeFactory, logger, TimeProvider.System)
    {
    }

    internal VisitDeletionReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<VisitDeletionReconciliationBackgroundService> logger,
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
                    "Unable to reconcile deleted visit side effects.");
            }

            await Task.Delay(PollInterval, this.timeProvider, stoppingToken);
        }
    }

    internal async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = this.scopeFactory.CreateScope();
        IVisitDeletionStore deletionStore =
            scope.ServiceProvider.GetRequiredService<IVisitDeletionStore>();
        IPassportExportRepository exportRepository =
            scope.ServiceProvider.GetRequiredService<IPassportExportRepository>();
        VisitPurgeScheduler scheduler =
            scope.ServiceProvider.GetRequiredService<VisitPurgeScheduler>();
        IReadOnlyCollection<VisitDeletionReconciliationCandidate> candidates =
            await deletionStore.ListPendingDeletionReconciliationAsync(
                BatchSize,
                cancellationToken);
        foreach (VisitDeletionReconciliationCandidate candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!candidate.IsExportInvalidationEnsured)
                {
                    DateTime claimedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
                    VisitExportInvalidationClaim? claim =
                        await deletionStore.TryClaimExportInvalidationAsync(
                            candidate.VisitId,
                            candidate.UserId,
                            candidate.DeletionVersion,
                            claimedAtUtc,
                            claimedAtUtc.Add(
                                VisitDeletionPolicy.ExportInvalidationClaimDuration),
                            cancellationToken);
                    if (claim is not null)
                    {
                        await exportRepository.InvalidateOwnedAsync(
                            candidate.UserId,
                            claim.FenceAtUtc,
                            this.timeProvider.GetUtcNow().UtcDateTime,
                            cancellationToken);
                        _ = await deletionStore.CompleteExportInvalidationAsync(
                            candidate.VisitId,
                            candidate.UserId,
                            candidate.DeletionVersion,
                            claim.Token,
                            this.timeProvider.GetUtcNow().UtcDateTime,
                            cancellationToken);
                    }
                }

                if (!candidate.IsPurgeJobEnsured)
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
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Unable to reconcile side effects for deleted visit {VisitId}.",
                    candidate.VisitId.Value);
            }
        }
    }
}
