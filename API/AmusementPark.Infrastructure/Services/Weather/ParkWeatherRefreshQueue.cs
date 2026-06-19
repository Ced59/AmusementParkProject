using System.Threading.Channels;
using AmusementPark.Application.Features.ParkWeather.Contracts;
using AmusementPark.Application.Features.ParkWeather.Ports;

namespace AmusementPark.Infrastructure.Services.Weather;

public sealed class ParkWeatherRefreshQueue : IParkWeatherRefreshQueue
{
    private readonly Channel<ParkWeatherRefreshJob> channel;

    public ParkWeatherRefreshQueue()
    {
        this.channel = Channel.CreateUnbounded<ParkWeatherRefreshJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public ValueTask EnqueueAsync(ParkWeatherRefreshJob job, CancellationToken cancellationToken)
    {
        return this.channel.Writer.WriteAsync(job, cancellationToken);
    }

    public ValueTask<ParkWeatherRefreshJob> DequeueAsync(CancellationToken cancellationToken)
    {
        return this.channel.Reader.ReadAsync(cancellationToken);
    }
}
