using AmusementPark.Application.Features.Trips.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Trips;

internal sealed class TripAdmissionReconciliationBackgroundService : BackgroundService
{
    internal static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TripAdmissionReconciliationBackgroundService> logger;
    private readonly TimeProvider timeProvider;

    public TripAdmissionReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TripAdmissionReconciliationBackgroundService> logger)
        : this(scopeFactory, logger, TimeProvider.System)
    {
    }

    internal TripAdmissionReconciliationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TripAdmissionReconciliationBackgroundService> logger,
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
                using IServiceScope scope = this.scopeFactory.CreateScope();
                TripAdmissionReconciler reconciler =
                    scope.ServiceProvider.GetRequiredService<TripAdmissionReconciler>();
                _ = await reconciler.ReconcileBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Unable to reconcile trip member admissions.");
            }

            await Task.Delay(PollInterval, this.timeProvider, stoppingToken);
        }
    }
}
