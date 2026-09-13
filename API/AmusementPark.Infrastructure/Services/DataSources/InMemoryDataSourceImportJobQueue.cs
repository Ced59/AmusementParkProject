using System.Threading.Channels;
using AmusementPark.Application.Features.DataSources.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.DataSources;

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
