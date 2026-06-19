using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkWeather.Ports;

public interface IParkWeatherCacheInvalidator
{
    Task InvalidateUpdatedWeatherAsync(IReadOnlyCollection<Park> parks, CancellationToken cancellationToken);
}
