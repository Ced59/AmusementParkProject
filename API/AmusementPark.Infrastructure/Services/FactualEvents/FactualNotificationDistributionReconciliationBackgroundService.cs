using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.FactualEvents;

internal sealed class FactualNotificationDistributionReconciliationBackgroundService : BackgroundService
{
    internal static readonly TimeSpan ReconciliationInterval = TimeSpan.FromMinutes(1);
    internal static readonly TimeSpan CompletedAuditInterval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<FactualNotificationDistributionReconciliationBackgroundService> logger;
    private readonly TimeProvider timeProvider;
    private PublishedFactualEventCursor? cursor;

    public FactualNotificationDistributionReconciliationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<FactualNotificationDistributionReconciliationBackgroundService> logger)
        : this(serviceScopeFactory, logger, TimeProvider.System)
    {
    }

    internal FactualNotificationDistributionReconciliationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<FactualNotificationDistributionReconciliationBackgroundService> logger,
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
            TimeSpan nextDelay = ReconciliationInterval;
            try
            {
                using IServiceScope scope = this.serviceScopeFactory.CreateScope();
                IFactualNotificationDistributionScheduler scheduler = scope.ServiceProvider
                    .GetRequiredService<IFactualNotificationDistributionScheduler>();
                this.cursor = await scheduler.ReconcileAsync(this.cursor, stoppingToken);
                if (this.cursor is null)
                {
                    nextDelay = CompletedAuditInterval;
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
                    "Unable to reconcile published factual notifications; the bounded scan will retry.");
            }

            try
            {
                await Task.Delay(nextDelay, this.timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
