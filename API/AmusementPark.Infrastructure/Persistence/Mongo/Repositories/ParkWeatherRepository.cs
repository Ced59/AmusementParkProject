using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Core.Domain.Weather;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Weather;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ParkWeatherRepository : IParkWeatherRepository
{
    private readonly IMongoCollection<ParkWeatherDailySnapshotDocument> collection;

    public ParkWeatherRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ParkWeatherDailySnapshotDocument>(settings.ParkWeatherDailySnapshotsCollectionName);
    }

    public async Task UpsertSnapshotsAsync(IReadOnlyCollection<ParkWeatherDailySnapshot> snapshots, CancellationToken cancellationToken)
    {
        if (snapshots.Count == 0)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        List<WriteModel<ParkWeatherDailySnapshotDocument>> writes = new List<WriteModel<ParkWeatherDailySnapshotDocument>>(snapshots.Count);
        foreach (ParkWeatherDailySnapshot snapshot in snapshots)
        {
            ParkWeatherDailySnapshotDocument document = snapshot.ToDocument();
            FilterDefinition<ParkWeatherDailySnapshotDocument> filter = Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.ParkId, document.ParkId)
                & Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.LocalDate, document.LocalDate)
                & Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.DataKind, document.DataKind);

            UpdateDefinition<ParkWeatherDailySnapshotDocument> update = Builders<ParkWeatherDailySnapshotDocument>.Update
                .SetOnInsert(item => item.Id, document.Id)
                .SetOnInsert(item => item.CreatedAt, now)
                .Set(item => item.UpdatedAt, now)
                .Set(item => item.SourceProvider, document.SourceProvider)
                .Set(item => item.FetchedAtUtc, document.FetchedAtUtc)
                .Set(item => item.ProviderGeneratedAtUtc, document.ProviderGeneratedAtUtc)
                .Set(item => item.TimeZone, document.TimeZone)
                .Set(item => item.UtcOffsetSeconds, document.UtcOffsetSeconds)
                .Set(item => item.Latitude, document.Latitude)
                .Set(item => item.Longitude, document.Longitude)
                .Set(item => item.WeatherCode, document.WeatherCode)
                .Set(item => item.TemperatureMinCelsius, document.TemperatureMinCelsius)
                .Set(item => item.TemperatureMaxCelsius, document.TemperatureMaxCelsius)
                .Set(item => item.ApparentTemperatureMinCelsius, document.ApparentTemperatureMinCelsius)
                .Set(item => item.ApparentTemperatureMaxCelsius, document.ApparentTemperatureMaxCelsius)
                .Set(item => item.PrecipitationProbabilityMaxPercent, document.PrecipitationProbabilityMaxPercent)
                .Set(item => item.PrecipitationSumMillimeters, document.PrecipitationSumMillimeters)
                .Set(item => item.WindSpeedMaxKilometersPerHour, document.WindSpeedMaxKilometersPerHour)
                .Set(item => item.WindGustsMaxKilometersPerHour, document.WindGustsMaxKilometersPerHour);

            writes.Add(new UpdateOneModel<ParkWeatherDailySnapshotDocument>(filter, update)
            {
                IsUpsert = true,
            });
        }

        await this.collection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
    }

    public async Task DeleteForecastsCoveredByObservationsAsync(string parkId, IReadOnlyCollection<DateOnly> observationDates, CancellationToken cancellationToken)
    {
        List<string> localDates = observationDates
            .Select(EntityMongoMappers.FormatDate)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (localDates.Count == 0)
        {
            return;
        }

        FilterDefinition<ParkWeatherDailySnapshotDocument> filter = Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.ParkId, parkId)
            & Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.DataKind, ParkWeatherDataKind.Forecast)
            & Builders<ParkWeatherDailySnapshotDocument>.Filter.In(item => item.LocalDate, localDates);

        await this.collection.DeleteManyAsync(filter, cancellationToken);
    }

    public async Task DeleteExpiredForecastsAsync(DateOnly oldestLocalDateToKeep, CancellationToken cancellationToken)
    {
        string oldestDate = EntityMongoMappers.FormatDate(oldestLocalDateToKeep);
        FilterDefinition<ParkWeatherDailySnapshotDocument> filter = Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.DataKind, ParkWeatherDataKind.Forecast)
            & Builders<ParkWeatherDailySnapshotDocument>.Filter.Lt(item => item.LocalDate, oldestDate);

        await this.collection.DeleteManyAsync(filter, cancellationToken);
    }

    public async Task DeleteExpiredObservationsAsync(DateOnly oldestLocalDateToKeep, CancellationToken cancellationToken)
    {
        string oldestDate = EntityMongoMappers.FormatDate(oldestLocalDateToKeep);
        FilterDefinition<ParkWeatherDailySnapshotDocument> filter = Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.DataKind, ParkWeatherDataKind.Observation)
            & Builders<ParkWeatherDailySnapshotDocument>.Filter.Lt(item => item.LocalDate, oldestDate);

        await this.collection.DeleteManyAsync(filter, cancellationToken);
    }

    public async Task<ParkWeatherDailySnapshot?> GetLatestForecastSnapshotAsync(string parkId, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkWeatherDailySnapshotDocument> filter = Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.ParkId, parkId)
            & Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.DataKind, ParkWeatherDataKind.Forecast);

        ParkWeatherDailySnapshotDocument? document = await this.collection.Find(filter)
            .SortByDescending(item => item.FetchedAtUtc)
            .ThenByDescending(item => item.LocalDate)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<ParkWeatherDailySnapshot>> GetForecastAsync(string parkId, DateOnly fromLocalDate, int dayCount, CancellationToken cancellationToken)
    {
        string fromDate = EntityMongoMappers.FormatDate(fromLocalDate);
        FilterDefinition<ParkWeatherDailySnapshotDocument> filter = Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.ParkId, parkId)
            & Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.DataKind, ParkWeatherDataKind.Forecast)
            & Builders<ParkWeatherDailySnapshotDocument>.Filter.Gte(item => item.LocalDate, fromDate);

        List<ParkWeatherDailySnapshotDocument> documents = await this.collection.Find(filter)
            .SortBy(item => item.LocalDate)
            .Limit(Math.Max(1, dayCount))
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<DateOnly>> GetExistingObservationDatesAsync(string parkId, IReadOnlyCollection<DateOnly> localDates, CancellationToken cancellationToken)
    {
        List<string> dateKeys = localDates
            .Select(EntityMongoMappers.FormatDate)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (dateKeys.Count == 0)
        {
            return Array.Empty<DateOnly>();
        }

        FilterDefinition<ParkWeatherDailySnapshotDocument> filter = Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.ParkId, parkId)
            & Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.DataKind, ParkWeatherDataKind.Observation)
            & Builders<ParkWeatherDailySnapshotDocument>.Filter.In(item => item.LocalDate, dateKeys);

        List<string> existingDateKeys = await this.collection.Find(filter)
            .Project(item => item.LocalDate)
            .ToListAsync(cancellationToken);

        return existingDateKeys
            .Distinct(StringComparer.Ordinal)
            .Select(EntityMongoMappers.ParseDate)
            .ToList();
    }

    public async Task<IReadOnlyCollection<ParkWeatherDailySnapshot>> GetObservationsByDatesAsync(string parkId, IReadOnlyCollection<DateOnly> localDates, CancellationToken cancellationToken)
    {
        List<string> dateKeys = localDates
            .Select(EntityMongoMappers.FormatDate)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (dateKeys.Count == 0)
        {
            return Array.Empty<ParkWeatherDailySnapshot>();
        }

        FilterDefinition<ParkWeatherDailySnapshotDocument> filter = Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.ParkId, parkId)
            & Builders<ParkWeatherDailySnapshotDocument>.Filter.Eq(item => item.DataKind, ParkWeatherDataKind.Observation)
            & Builders<ParkWeatherDailySnapshotDocument>.Filter.In(item => item.LocalDate, dateKeys);

        List<ParkWeatherDailySnapshotDocument> documents = await this.collection.Find(filter)
            .SortBy(item => item.LocalDate)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }
}
