using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Ports;

public interface IParkWeatherNotificationService
{
    Task NotifyRunStartedAsync(ParkWeatherRun run, CancellationToken cancellationToken);

    Task NotifyRunCompletedAsync(ParkWeatherRun run, CancellationToken cancellationToken);
}
