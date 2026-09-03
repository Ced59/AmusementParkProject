using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Passport;

internal sealed class PassportAuditReconciliationBackgroundService : BackgroundService
{
    internal static readonly TimeSpan ReconciliationInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<PassportAuditReconciliationBackgroundService> logger;
    private readonly TimeProvider timeProvider;

    public PassportAuditReconciliationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PassportAuditReconciliationBackgroundService> logger)
        : this(serviceScopeFactory, logger, TimeProvider.System)
    {
    }

    internal PassportAuditReconciliationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PassportAuditReconciliationBackgroundService> logger,
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
                IPassportAuditReconciler reconciler =
                    scope.ServiceProvider.GetRequiredService<IPassportAuditReconciler>();
                _ = await reconciler.ReconcileBatchAsync(
                    PassportAuditStore.MaximumReconciliationBatchSize,
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
                    "Unable to reconcile pending passport audit events; the bounded scan will retry.");
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
