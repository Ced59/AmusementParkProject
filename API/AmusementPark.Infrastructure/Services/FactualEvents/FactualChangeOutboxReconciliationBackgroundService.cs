using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.FactualEvents;

internal sealed class FactualChangeOutboxReconciliationBackgroundService : BackgroundService
{
    internal static readonly TimeSpan ReconciliationInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<FactualChangeOutboxReconciliationBackgroundService> logger;
    private readonly TimeProvider timeProvider;
    private FactualChangeOutboxCursor? cursor;
    private ParkOpeningHoursFactualChangeCursor? openingHoursCursor;

    public FactualChangeOutboxReconciliationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<FactualChangeOutboxReconciliationBackgroundService> logger)
        : this(serviceScopeFactory, logger, TimeProvider.System)
    {
    }

    internal FactualChangeOutboxReconciliationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<FactualChangeOutboxReconciliationBackgroundService> logger,
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
                IParkOpeningHoursFactualChangeCapture openingHoursCapture =
                    scope.ServiceProvider.GetRequiredService<IParkOpeningHoursFactualChangeCapture>();
                this.openingHoursCursor = await openingHoursCapture.ReconcilePendingAsync(
                    this.openingHoursCursor,
                    100,
                    stoppingToken);
                IFactualChangeMaterializationScheduler scheduler =
                    scope.ServiceProvider.GetRequiredService<IFactualChangeMaterializationScheduler>();
                this.cursor = await scheduler.ReconcilePendingAsync(
                    this.cursor,
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
                    "Unable to reconcile the factual outbox; the bounded scan will retry.");
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
