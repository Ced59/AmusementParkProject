using System.Threading.Channels;
using AmusementPark.Application.Features.DataSources.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.DataSources;

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
