using AmusementPark.Application.Features.Watchlists.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.LiveData;

internal sealed class LiveAlertPendingDeliveryBackgroundService : BackgroundService
{
    internal static readonly TimeSpan RetryInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<LiveAlertPendingDeliveryBackgroundService> logger;

    public LiveAlertPendingDeliveryBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<LiveAlertPendingDeliveryBackgroundService> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory
            ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await this.RetryPendingSafelyAsync(stoppingToken);

            try
            {
                await Task.Delay(RetryInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RetryPendingSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = this.serviceScopeFactory.CreateScope();
            LiveAlertEvaluationService evaluator =
                scope.ServiceProvider.GetRequiredService<LiveAlertEvaluationService>();
            await evaluator.RetryPendingAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "The independent pending live alert reconciliation failed and will retry later.");
        }
    }
}
