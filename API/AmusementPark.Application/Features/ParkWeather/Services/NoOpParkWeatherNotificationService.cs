using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Services;

public sealed class NoOpParkWeatherNotificationService : IParkWeatherNotificationService
{
    public Task NotifyRunStartedAsync(ParkWeatherRun run, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task NotifyRunCompletedAsync(ParkWeatherRun run, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
