using AmusementPark.Application.Features.Trips.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Trips;

internal sealed class TripPreferenceCleanupReconciliationBackgroundService : BackgroundService
{
    internal const int BatchSize = 25;
    internal static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TripPreferenceCleanupReconciliationBackgroundService> logger;
    private readonly TimeProvider timeProvider;

    public TripPreferenceCleanupReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TripPreferenceCleanupReconciliationBackgroundService> logger)
        : this(scopeFactory, logger, TimeProvider.System)
    {
    }

    internal TripPreferenceCleanupReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TripPreferenceCleanupReconciliationBackgroundService> logger,
        TimeProvider timeProvider)
    {
        this.scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
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
                    "Unable to reconcile departed trip member private-data cleanup.");
            }

            await Task.Delay(PollInterval, this.timeProvider, stoppingToken);
        }
    }

    internal async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = this.scopeFactory.CreateScope();
        TripPreferenceCleanupReconciler reconciler =
            scope.ServiceProvider.GetRequiredService<TripPreferenceCleanupReconciler>();
        _ = await reconciler.ReconcileAsync(BatchSize, cancellationToken);
    }
}
