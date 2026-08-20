using System.Threading.Channels;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AmusementPark.Infrastructure.Services.Seo;

public sealed class InMemorySeoSitemapRefreshScheduler : BackgroundService, ISeoSitemapRefreshScheduler
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly IPublicSeoResponseCacheInvalidator? responseCacheInvalidator;
    private readonly Channel<bool> requests;
    private int isQueued;

    public InMemorySeoSitemapRefreshScheduler(
        IServiceScopeFactory serviceScopeFactory,
        IPublicSeoResponseCacheInvalidator? responseCacheInvalidator = null)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.responseCacheInvalidator = responseCacheInvalidator;
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
        ISsrPageCacheInvalidator? ssrPageCacheInvalidator = scope.ServiceProvider.GetService<ISsrPageCacheInvalidator>();

        SitemapGenerationResult result = await orchestrator.GenerateAsync(
            context.PublicBaseUrl,
            new SitemapGenerationContext
            {
                SupportedLanguages = context.SupportedLanguages,
            },
            SitemapGenerationTrigger.Automatic,
            triggeredByUserId: null,
            triggeredByUserEmail: null,
            cancellationToken);

        await this.InvalidatePublicResponsesAfterSuccessfulGenerationAsync(
            result,
            ssrPageCacheInvalidator,
            cancellationToken);
    }

    internal async Task InvalidatePublicResponsesAfterSuccessfulGenerationAsync(
        SitemapGenerationResult result,
        ISsrPageCacheInvalidator? ssrPageCacheInvalidator,
        CancellationToken cancellationToken)
    {
        if (result.Status != SitemapGenerationStatus.Succeeded)
        {
            return;
        }

        if (this.responseCacheInvalidator is not null)
        {
            await this.responseCacheInvalidator.InvalidateAsync(cancellationToken);
        }

        if (ssrPageCacheInvalidator is not null)
        {
            await ssrPageCacheInvalidator.InvalidateAsync(
                new SsrPageCacheInvalidationRequest
                {
                    IncludeSeoDocuments = true,
                    AllowStale = false,
                    Refresh = false,
                },
                cancellationToken);
        }
    }

    private void DrainRequests()
    {
        while (this.requests.Reader.TryRead(out bool _))
        {
        }
    }
}
