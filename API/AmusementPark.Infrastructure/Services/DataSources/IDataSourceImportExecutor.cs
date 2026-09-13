using System.Threading.Channels;
using AmusementPark.Application.Features.DataSources.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.DataSources;

internal interface IDataSourceImportExecutor
{
    Task ExecuteImportAsync(DataSourceImportJob job, CancellationToken cancellationToken);
}
