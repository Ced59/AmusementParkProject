using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Infrastructure.Configuration.Weather;

namespace AmusementPark.Infrastructure.Services.Weather;

public sealed class ParkWeatherProviderStrategyResolver : IParkWeatherProviderStrategyResolver
{
    private readonly IEnumerable<IParkWeatherProviderStrategy> strategies;
    private readonly ParkWeatherSettings settings;

    public ParkWeatherProviderStrategyResolver(
        IEnumerable<IParkWeatherProviderStrategy> strategies,
        ParkWeatherSettings settings)
    {
        this.strategies = strategies;
        this.settings = settings;
    }

    public IParkWeatherProviderStrategy Resolve()
    {
        IParkWeatherProviderStrategy? strategy = this.strategies.FirstOrDefault(item =>
            string.Equals(item.ProviderKey, this.settings.ActiveProviderKey, StringComparison.OrdinalIgnoreCase));

        if (strategy is null)
        {
            throw new InvalidOperationException($"No weather provider strategy is registered for '{this.settings.ActiveProviderKey}'.");
        }

        return strategy;
    }
}
