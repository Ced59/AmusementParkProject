namespace AmusementPark.Application.Features.ParkWeather.Ports;

public interface IParkWeatherProviderStrategyResolver
{
    IParkWeatherProviderStrategy Resolve();
}
