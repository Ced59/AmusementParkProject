using AmusementPark.Application.Features.Seo.Ports;
using Microsoft.Extensions.Hosting;

namespace AmusementPark.Infrastructure.Services.Seo;

public sealed class ParkPricingSitemapRolloverRefreshService : BackgroundService
{
    private static readonly TimeSpan RolloverSafetyDelay = TimeSpan.FromSeconds(5);

    private readonly ISeoSitemapRefreshScheduler sitemapRefreshScheduler;
    private readonly TimeProvider timeProvider;

    public ParkPricingSitemapRolloverRefreshService(ISeoSitemapRefreshScheduler sitemapRefreshScheduler)
        : this(sitemapRefreshScheduler, TimeProvider.System)
    {
    }

    internal ParkPricingSitemapRolloverRefreshService(
        ISeoSitemapRefreshScheduler sitemapRefreshScheduler,
        TimeProvider timeProvider)
    {
        this.sitemapRefreshScheduler = sitemapRefreshScheduler;
        this.timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan delay = ResolveDelayUntilNextUtcRollover(this.timeProvider.GetUtcNow());
            await Task.Delay(delay, this.timeProvider, stoppingToken);
            await this.sitemapRefreshScheduler.RequestRefreshAsync(stoppingToken);
        }
    }

    internal static TimeSpan ResolveDelayUntilNextUtcRollover(DateTimeOffset nowUtc)
    {
        DateTimeOffset nextUtcRollover = new DateTimeOffset(
            nowUtc.UtcDateTime.Date.AddDays(1),
            TimeSpan.Zero).Add(RolloverSafetyDelay);
        return nextUtcRollover - nowUtc;
    }
}
