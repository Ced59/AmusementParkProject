using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Ports;

public interface IParkWeatherProviderStrategy
{
    string ProviderKey { get; }

    Task<ParkWeatherProviderResult> FetchDailyForecastAsync(
        Park park,
        int forecastDays,
        bool includeYesterdayObservation,
        CancellationToken cancellationToken);

    Task<ParkWeatherProviderResult> FetchDailyObservationsAsync(
        Park park,
        IReadOnlyCollection<DateOnly> localDates,
        CancellationToken cancellationToken);
}
