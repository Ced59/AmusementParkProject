using System.Diagnostics;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Services;
using AmusementPark.Infrastructure.Configuration.LiveData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.LiveData;

internal sealed class LivePollingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly LiveDataPollingSettings settings;
    private readonly LivePollingMetrics metrics;
    private readonly ILogger<LivePollingBackgroundService> logger;
    private readonly IReadOnlyCollection<LivePollingTarget> targets;
    private readonly string leaseOwner = Guid.NewGuid().ToString("N");

    public LivePollingBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        LiveDataPollingSettings settings,
        LivePollingMetrics metrics,
        ILogger<LivePollingBackgroundService> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(logger);
        this.serviceScopeFactory = serviceScopeFactory;
        this.settings = settings;
        this.metrics = metrics;
        this.logger = logger;
        this.targets = settings.BuildEnabledTargets();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!this.settings.Enabled)
        {
            this.logger.LogInformation("The live polling pilot is disabled by configuration.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (LivePollingTarget target in this.targets)
            {
                await this.ExecuteTargetSafelyAsync(target, stoppingToken);
            }

            try
            {
                await Task.Delay(this.settings.LoopDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ExecuteTargetSafelyAsync(
        LivePollingTarget target,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            using IServiceScope scope = this.serviceScopeFactory.CreateScope();
            LivePollingOrchestrator orchestrator =
                scope.ServiceProvider.GetRequiredService<LivePollingOrchestrator>();
            LivePollingExecutionResult result = await orchestrator.ExecuteAsync(
                target,
                this.leaseOwner,
                this.settings.LeaseDuration,
                cancellationToken);
            stopwatch.Stop();
            this.metrics.Record(target, result, stopwatch.Elapsed);
            if (result.Disposition != LivePollingExecutionDisposition.NotDue)
            {
                this.logger.LogInformation(
                    "Live polling for source {SourceId} completed with {Disposition}, {ObservationCount} observations and {DiagnosticCount} diagnostics.",
                    target.SourceId.Value,
                    result.Disposition,
                    result.ObservationCount,
                    result.DiagnosticCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Live polling for source {SourceId} failed; the bounded loop will retry later.",
                target.SourceId.Value);
        }
    }
}
