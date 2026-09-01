using AmusementPark.Application.Features.Ratings.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Ratings;

internal sealed class RatingRankingRebuildReconciliationBackgroundService : BackgroundService
{
    internal static readonly TimeSpan ReconciliationInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<RatingRankingRebuildReconciliationBackgroundService> logger;
    private readonly TimeProvider timeProvider;

    public RatingRankingRebuildReconciliationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<RatingRankingRebuildReconciliationBackgroundService> logger)
        : this(serviceScopeFactory, logger, TimeProvider.System)
    {
    }

    internal RatingRankingRebuildReconciliationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<RatingRankingRebuildReconciliationBackgroundService> logger,
        TimeProvider timeProvider)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
        this.timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = this.serviceScopeFactory.CreateScope();
                IRatingRankingRecoveryCoordinator recoveryCoordinator =
                    scope.ServiceProvider.GetRequiredService<IRatingRankingRecoveryCoordinator>();
                bool recoveryCompleted =
                    await recoveryCoordinator.ReconcileRecoveredRatingMutationsAsync(stoppingToken);
                if (recoveryCompleted)
                {
                    IRatingRankingRebuildScheduler scheduler =
                        scope.ServiceProvider.GetRequiredService<IRatingRankingRebuildScheduler>();
                    await scheduler.ScheduleOutstandingAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Unable to reconcile outstanding canonical ranking rebuilds; the bounded scan will retry.");
            }

            try
            {
                await Task.Delay(ReconciliationInterval, this.timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
