using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Ports;

public interface IParkWeatherRepository
{
    Task UpsertSnapshotsAsync(IReadOnlyCollection<ParkWeatherDailySnapshot> snapshots, CancellationToken cancellationToken);

    Task DeleteForecastsCoveredByObservationsAsync(string parkId, IReadOnlyCollection<DateOnly> observationDates, CancellationToken cancellationToken);

    Task DeleteExpiredForecastsAsync(DateOnly oldestLocalDateToKeep, CancellationToken cancellationToken);

    Task DeleteExpiredObservationsAsync(DateOnly oldestLocalDateToKeep, CancellationToken cancellationToken);

    Task<ParkWeatherDailySnapshot?> GetLatestForecastSnapshotAsync(string parkId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ParkWeatherDailySnapshot>> GetForecastAsync(string parkId, DateOnly fromLocalDate, int dayCount, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DateOnly>> GetExistingObservationDatesAsync(string parkId, IReadOnlyCollection<DateOnly> localDates, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ParkWeatherDailySnapshot>> GetObservationsByDatesAsync(string parkId, IReadOnlyCollection<DateOnly> localDates, CancellationToken cancellationToken);
}
