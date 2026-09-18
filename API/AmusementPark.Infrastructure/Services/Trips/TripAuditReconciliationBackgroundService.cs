using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Trips;

internal sealed class TripAuditReconciliationBackgroundService : BackgroundService
{
    internal static readonly TimeSpan ReconciliationInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<TripAuditReconciliationBackgroundService> logger;
    private readonly TimeProvider timeProvider;

    public TripAuditReconciliationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TripAuditReconciliationBackgroundService> logger)
        : this(serviceScopeFactory, logger, TimeProvider.System)
    {
    }

    internal TripAuditReconciliationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TripAuditReconciliationBackgroundService> logger,
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
                ITripAuditReconciler reconciler =
                    scope.ServiceProvider.GetRequiredService<ITripAuditReconciler>();
                _ = await reconciler.ReconcilePendingAsync(
                    TripAuditRepository.MaximumReconciliationBatchSize,
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Unable to reconcile pending trip activities; the bounded scan will retry.");
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
