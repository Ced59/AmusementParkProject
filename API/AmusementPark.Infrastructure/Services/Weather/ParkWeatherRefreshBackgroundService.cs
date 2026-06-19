using AmusementPark.Application.Features.ParkWeather.Contracts;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Weather;

public sealed class ParkWeatherRefreshBackgroundService : BackgroundService
{
    private readonly IParkWeatherRefreshQueue queue;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<ParkWeatherRefreshBackgroundService> logger;

    public ParkWeatherRefreshBackgroundService(
        IParkWeatherRefreshQueue queue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ParkWeatherRefreshBackgroundService> logger)
    {
        this.queue = queue;
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ParkWeatherRefreshJob job = await this.queue.DequeueAsync(stoppingToken);

            try
            {
                using IServiceScope scope = this.serviceScopeFactory.CreateScope();
                ParkWeatherRefreshOrchestrator orchestrator = scope.ServiceProvider.GetRequiredService<ParkWeatherRefreshOrchestrator>();
                await orchestrator.ProcessRunAsync(job.RunId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Error while processing park weather refresh run '{RunId}'.", job.RunId);
            }
        }
    }
}
