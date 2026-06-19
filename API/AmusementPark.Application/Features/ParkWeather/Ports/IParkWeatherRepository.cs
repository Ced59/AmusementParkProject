using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Ports;

public interface IParkWeatherRepository
{
    Task UpsertSnapshotsAsync(IReadOnlyCollection<ParkWeatherDailySnapshot> snapshots, CancellationToken cancellationToken);

    Task DeleteForecastsCoveredByObservationsAsync(string parkId, IReadOnlyCollection<DateOnly> observationDates, CancellationToken cancellationToken);

    Task DeleteExpiredForecastsAsync(DateOnly oldestLocalDateToKeep, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ParkWeatherDailySnapshot>> GetForecastAsync(string parkId, DateOnly fromLocalDate, int dayCount, CancellationToken cancellationToken);
}
