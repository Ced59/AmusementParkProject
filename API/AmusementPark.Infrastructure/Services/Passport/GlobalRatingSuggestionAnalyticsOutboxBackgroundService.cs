using AmusementPark.Application.Features.Passport.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Passport;

internal sealed class GlobalRatingSuggestionAnalyticsOutboxBackgroundService : BackgroundService
{
    internal const int MaximumBatchSize = 100;
    internal static readonly TimeSpan ReconciliationInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<GlobalRatingSuggestionAnalyticsOutboxBackgroundService> logger;
    private readonly TimeProvider timeProvider;

    public GlobalRatingSuggestionAnalyticsOutboxBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<GlobalRatingSuggestionAnalyticsOutboxBackgroundService> logger)
        : this(serviceScopeFactory, logger, TimeProvider.System)
    {
    }

    internal GlobalRatingSuggestionAnalyticsOutboxBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<GlobalRatingSuggestionAnalyticsOutboxBackgroundService> logger,
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
                IGlobalRatingSuggestionAnalyticsOutboxReconciler reconciler =
                    scope.ServiceProvider.GetRequiredService<
                        IGlobalRatingSuggestionAnalyticsOutboxReconciler>();
                _ = await reconciler.ReconcileBatchAsync(MaximumBatchSize, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                this.logger.LogError(
                    exception,
                    "Unable to publish pending global rating suggestion analytics; the durable outbox will retry.");
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
