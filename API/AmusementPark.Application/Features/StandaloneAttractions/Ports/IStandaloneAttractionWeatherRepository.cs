using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.StandaloneAttractions.Ports;

public interface IStandaloneAttractionWeatherRepository
{
    Task UpsertSnapshotsAsync(
        IReadOnlyCollection<ParkWeatherDailySnapshot> snapshots,
        CancellationToken cancellationToken);

    Task DeleteForecastsCoveredByObservationsAsync(
        string standaloneAttractionId,
        IReadOnlyCollection<DateOnly> observationDates,
        CancellationToken cancellationToken);

    Task DeleteExpiredForecastsAsync(DateOnly oldestLocalDateToKeep, CancellationToken cancellationToken);

    Task DeleteExpiredObservationsAsync(DateOnly oldestLocalDateToKeep, CancellationToken cancellationToken);

    Task<ParkWeatherDailySnapshot?> GetLatestForecastSnapshotAsync(
        string standaloneAttractionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ParkWeatherDailySnapshot>> GetForecastAsync(
        string standaloneAttractionId,
        DateOnly fromLocalDate,
        int dayCount,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DateOnly>> GetExistingObservationDatesAsync(
        string standaloneAttractionId,
        IReadOnlyCollection<DateOnly> localDates,
        CancellationToken cancellationToken);
}
