using System.Threading.Channels;
using AmusementPark.Application.Features.DataSources.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.DataSources;

internal sealed record DataSourceImportJob(string SourceKey, string SessionId, DataSourceImportDescriptor ImportDescriptor);

internal interface IDataSourceImportJobQueue
{
    ValueTask EnqueueAsync(DataSourceImportJob job, CancellationToken cancellationToken);

    ValueTask<DataSourceImportJob> DequeueAsync(CancellationToken cancellationToken);
}

internal sealed class InMemoryDataSourceImportJobQueue : IDataSourceImportJobQueue
{
    private readonly Channel<DataSourceImportJob> channel;

    public InMemoryDataSourceImportJobQueue()
    {
        this.channel = Channel.CreateUnbounded<DataSourceImportJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public ValueTask EnqueueAsync(DataSourceImportJob job, CancellationToken cancellationToken)
    {
        return this.channel.Writer.WriteAsync(job, cancellationToken);
    }

    public ValueTask<DataSourceImportJob> DequeueAsync(CancellationToken cancellationToken)
    {
        return this.channel.Reader.ReadAsync(cancellationToken);
    }
}

internal interface IDataSourceImportJobProcessor
{
    Task ProcessAsync(DataSourceImportJob job, CancellationToken cancellationToken);
}

internal sealed class DataSourceImportJobProcessor : IDataSourceImportJobProcessor
{
    private readonly IEnumerable<IDataSourceProvider> providers;

    public DataSourceImportJobProcessor(IEnumerable<IDataSourceProvider> providers)
    {
        this.providers = providers;
    }

    public async Task ProcessAsync(DataSourceImportJob job, CancellationToken cancellationToken)
    {
        IDataSourceProvider? provider = this.providers.FirstOrDefault(provider => string.Equals(provider.SourceKey, job.SourceKey, StringComparison.OrdinalIgnoreCase));
        if (provider is not IDataSourceImportExecutor executor)
        {
            throw new InvalidOperationException($"Aucun exécuteur d'import n'est enregistré pour la source '{job.SourceKey}'.");
        }

        await executor.ExecuteImportAsync(job, cancellationToken);
    }
}

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

internal interface IDataSourceImportExecutor
{
    Task ExecuteImportAsync(DataSourceImportJob job, CancellationToken cancellationToken);
}
