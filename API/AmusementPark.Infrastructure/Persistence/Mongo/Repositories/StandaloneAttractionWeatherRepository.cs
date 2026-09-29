using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Configuration.Mongo;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class StandaloneAttractionWeatherRepository : IStandaloneAttractionWeatherRepository
{
    private readonly ParkWeatherRepository repository;

    public StandaloneAttractionWeatherRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.repository = new ParkWeatherRepository(
            database,
            settings.StandaloneAttractionWeatherDailySnapshotsCollectionName);
    }

    public Task UpsertSnapshotsAsync(
        IReadOnlyCollection<ParkWeatherDailySnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        return this.repository.UpsertSnapshotsAsync(snapshots, cancellationToken);
    }

    public Task DeleteForecastsCoveredByObservationsAsync(
        string standaloneAttractionId,
        IReadOnlyCollection<DateOnly> observationDates,
        CancellationToken cancellationToken)
    {
        return this.repository.DeleteForecastsCoveredByObservationsAsync(
            standaloneAttractionId,
            observationDates,
            cancellationToken);
    }

    public Task DeleteExpiredForecastsAsync(
        DateOnly oldestLocalDateToKeep,
        CancellationToken cancellationToken)
    {
        return this.repository.DeleteExpiredForecastsAsync(oldestLocalDateToKeep, cancellationToken);
    }

    public Task DeleteExpiredObservationsAsync(
        DateOnly oldestLocalDateToKeep,
        CancellationToken cancellationToken)
    {
        return this.repository.DeleteExpiredObservationsAsync(oldestLocalDateToKeep, cancellationToken);
    }

    public Task<ParkWeatherDailySnapshot?> GetLatestForecastSnapshotAsync(
        string standaloneAttractionId,
        CancellationToken cancellationToken)
    {
        return this.repository.GetLatestForecastSnapshotAsync(standaloneAttractionId, cancellationToken);
    }

    public Task<IReadOnlyCollection<ParkWeatherDailySnapshot>> GetForecastAsync(
        string standaloneAttractionId,
        DateOnly fromLocalDate,
        int dayCount,
        CancellationToken cancellationToken)
    {
        return this.repository.GetForecastAsync(
            standaloneAttractionId,
            fromLocalDate,
            dayCount,
            cancellationToken);
    }

    public Task<IReadOnlyCollection<DateOnly>> GetExistingObservationDatesAsync(
        string standaloneAttractionId,
        IReadOnlyCollection<DateOnly> localDates,
        CancellationToken cancellationToken)
    {
        return this.repository.GetExistingObservationDatesAsync(
            standaloneAttractionId,
            localDates,
            cancellationToken);
    }
}
