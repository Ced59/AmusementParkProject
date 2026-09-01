using AmusementPark.Application.Features.Ratings.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Ratings;

internal sealed class RatingRankingRebuildActivationHostedService : IHostedService
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<RatingRankingRebuildActivationHostedService> logger;

    public RatingRankingRebuildActivationHostedService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<RatingRankingRebuildActivationHostedService> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = this.serviceScopeFactory.CreateScope();
        IRatingRankingRebuildScheduler scheduler =
            scope.ServiceProvider.GetRequiredService<IRatingRankingRebuildScheduler>();
        await scheduler.ScheduleOutstandingAsync(cancellationToken);
        this.logger.LogInformation(
            "Outstanding canonical ranking source revisions were scheduled for bounded reconstruction.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
