using System.Threading.Channels;
using AmusementPark.Application.Features.DataSources.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.DataSources;

internal sealed class DataSourceImportBackgroundService : BackgroundService
{
    private readonly IDataSourceImportJobQueue queue;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<DataSourceImportBackgroundService> logger;

    public DataSourceImportBackgroundService(
        IDataSourceImportJobQueue queue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<DataSourceImportBackgroundService> logger)
    {
        this.queue = queue;
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            DataSourceImportJob job = await this.queue.DequeueAsync(stoppingToken);

            try
            {
                using IServiceScope scope = this.serviceScopeFactory.CreateScope();
                IDataSourceImportJobProcessor processor = scope.ServiceProvider.GetRequiredService<IDataSourceImportJobProcessor>();
                await processor.ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Erreur pendant l'exécution du job d'import de source '{SourceKey}' pour la session '{SessionId}'.", job.SourceKey, job.SessionId);
            }
        }
    }
}
