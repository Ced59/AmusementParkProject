using AmusementPark.Application.Features.ParkWeather.Contracts;

namespace AmusementPark.Application.Features.ParkWeather.Ports;

public interface IParkWeatherRefreshQueue
{
    ValueTask EnqueueAsync(ParkWeatherRefreshJob job, CancellationToken cancellationToken);

    ValueTask<ParkWeatherRefreshJob> DequeueAsync(CancellationToken cancellationToken);
}
