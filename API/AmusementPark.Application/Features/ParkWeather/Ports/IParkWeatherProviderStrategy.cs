using AmusementPark.Application.Features.ParkWeather.Contracts;
using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Ports;

public interface IParkWeatherProviderStrategy
{
    string ProviderKey { get; }

    Task<ParkWeatherProviderResult> FetchDailyForecastAsync(
        ParkWeatherLocation location,
        int forecastDays,
        bool includeYesterdayObservation,
        CancellationToken cancellationToken);

    Task<ParkWeatherProviderResult> FetchDailyObservationsAsync(
        ParkWeatherLocation location,
        IReadOnlyCollection<DateOnly> localDates,
        CancellationToken cancellationToken);
}
