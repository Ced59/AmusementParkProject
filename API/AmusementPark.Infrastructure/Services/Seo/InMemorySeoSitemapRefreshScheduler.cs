using System.Threading.Channels;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AmusementPark.Infrastructure.Services.Seo;

public sealed class InMemorySeoSitemapRefreshScheduler : BackgroundService, ISeoSitemapRefreshScheduler
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly Channel<bool> requests;
    private int isQueued;

    public InMemorySeoSitemapRefreshScheduler(IServiceScopeFactory serviceScopeFactory)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.requests = Channel.CreateUnbounded<bool>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public Task RequestRefreshAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref this.isQueued, 1) == 0)
        {
            this.requests.Writer.TryWrite(true);
        }

        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await this.requests.Reader.WaitToReadAsync(stoppingToken))
        {
            DrainRequests();

            try
            {
                await Task.Delay(DebounceDelay, stoppingToken);
                DrainRequests();
                Interlocked.Exchange(ref this.isQueued, 0);
                await this.GenerateSitemapSnapshotAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                Interlocked.Exchange(ref this.isQueued, 0);
            }
        }
    }

    private async Task GenerateSitemapSnapshotAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = this.serviceScopeFactory.CreateScope();
        IPublicSeoContextProvider contextProvider = scope.ServiceProvider.GetRequiredService<IPublicSeoContextProvider>();
        SeoSitemapGenerationOrchestrator orchestrator = scope.ServiceProvider.GetRequiredService<SeoSitemapGenerationOrchestrator>();
        PublicSeoContext context = await contextProvider.GetAsync(cancellationToken);

        await orchestrator.GenerateAsync(
            context.PublicBaseUrl,
            new SitemapGenerationContext
            {
                SupportedLanguages = context.SupportedLanguages,
            },
            SitemapGenerationTrigger.Automatic,
            submitToIndexNow: false,
            triggeredByUserId: null,
            triggeredByUserEmail: null,
            cancellationToken);
    }

    private void DrainRequests()
    {
        while (this.requests.Reader.TryRead(out bool _))
        {
        }
    }
}
