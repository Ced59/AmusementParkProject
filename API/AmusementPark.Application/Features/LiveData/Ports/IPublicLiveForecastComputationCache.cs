using AmusementPark.Application.Features.LiveData.Models;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface IPublicLiveForecastComputationCache
{
    Task<PublicLiveForecastComputation?> GetOrCreateAsync(
        string cacheKey,
        Func<CancellationToken, Task<PublicLiveForecastComputation?>> factory,
        CancellationToken cancellationToken);
}
