using AmusementPark.Application.Features.Comments.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Comments;

public sealed class CommentImageDraftCleanupBackgroundService : BackgroundService
{
    private static readonly TimeSpan DraftRetention = TimeSpan.FromHours(24);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);
    private const int MaximumDeletionsPerRun = 50;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<CommentImageDraftCleanupBackgroundService> logger;

    public CommentImageDraftCleanupBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CommentImageDraftCleanupBackgroundService> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await this.CleanupAsync(stoppingToken);
        using PeriodicTimer timer = new PeriodicTimer(CleanupInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await this.CleanupAsync(stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            using IServiceScope scope = this.serviceScopeFactory.CreateScope();
            CommentImageReconciler reconciler =
                scope.ServiceProvider.GetRequiredService<CommentImageReconciler>();
            DateTime nowUtc = DateTime.UtcNow;
            int reconciledCount = await reconciler.ReconcileAsync(
                nowUtc,
                nowUtc.Subtract(DraftRetention),
                MaximumDeletionsPerRun,
                cancellationToken);
            if (reconciledCount > 0)
            {
                this.logger.LogInformation(
                    "Reconciled {ReconciledCount} comment images.",
                    reconciledCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "Unable to clean expired comment image drafts.");
        }
    }
}
