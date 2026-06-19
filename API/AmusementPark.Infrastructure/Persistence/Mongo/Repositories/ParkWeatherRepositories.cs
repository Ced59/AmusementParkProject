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
}

public sealed class ParkWeatherRunRepository : IParkWeatherRunRepository
{
    private readonly IMongoCollection<ParkWeatherRunDocument> runsCollection;
    private readonly IMongoCollection<ParkWeatherRunItemDocument> itemsCollection;

    public ParkWeatherRunRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.runsCollection = database.GetCollection<ParkWeatherRunDocument>(settings.ParkWeatherRunsCollectionName);
        this.itemsCollection = database.GetCollection<ParkWeatherRunItemDocument>(settings.ParkWeatherRunItemsCollectionName);
    }

    public async Task<ParkWeatherRun> CreateAsync(ParkWeatherRun run, CancellationToken cancellationToken)
    {
        ParkWeatherRunDocument document = run.ToDocument();
        DateTime now = DateTime.UtcNow;
        document.CreatedAt = now;
        document.UpdatedAt = now;
        await this.runsCollection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<ParkWeatherRun?> GetByIdAsync(string runId, CancellationToken cancellationToken)
    {
        ParkWeatherRunDocument? document = await this.runsCollection.Find(item => item.Id == runId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<ParkWeatherRun?> GetLatestAsync(CancellationToken cancellationToken)
    {
        ParkWeatherRunDocument? document = await this.runsCollection.Find(Builders<ParkWeatherRunDocument>.Filter.Empty)
            .SortByDescending(item => item.RequestedAtUtc)
            .ThenByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<bool> HasActiveRunAsync(CancellationToken cancellationToken)
    {
        FilterDefinition<ParkWeatherRunDocument> filter = Builders<ParkWeatherRunDocument>.Filter.In(
            item => item.Status,
            new[] { ParkWeatherRunStatus.Queued, ParkWeatherRunStatus.Running });

        long count = await this.runsCollection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, cancellationToken);
        return count > 0;
    }

    public async Task<bool> HasAutomaticCancellationAsync(DateOnly automaticRunLocalDate, CancellationToken cancellationToken)
    {
        string localDate = EntityMongoMappers.FormatDate(automaticRunLocalDate);
        FilterDefinition<ParkWeatherRunDocument> filter = Builders<ParkWeatherRunDocument>.Filter.Eq(item => item.CancelsAutomaticRunLocalDate, localDate)
            & Builders<ParkWeatherRunDocument>.Filter.Ne(item => item.Status, ParkWeatherRunStatus.Failed);

        long count = await this.runsCollection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, cancellationToken);
        return count > 0;
    }

    public async Task UpdateAsync(ParkWeatherRun run, CancellationToken cancellationToken)
    {
        ParkWeatherRunDocument document = run.ToDocument();
        UpdateDefinition<ParkWeatherRunDocument> update = Builders<ParkWeatherRunDocument>.Update
            .Set(item => item.UpdatedAt, DateTime.UtcNow)
            .Set(item => item.Trigger, document.Trigger)
            .Set(item => item.Scope, document.Scope)
            .Set(item => item.Status, document.Status)
            .Set(item => item.SourceRunId, document.SourceRunId)
            .Set(item => item.TargetParkId, document.TargetParkId)
            .Set(item => item.CancelsAutomaticRunLocalDate, document.CancelsAutomaticRunLocalDate)
            .Set(item => item.RequestedAtUtc, document.RequestedAtUtc)
            .Set(item => item.StartedAtUtc, document.StartedAtUtc)
            .Set(item => item.CompletedAtUtc, document.CompletedAtUtc)
            .Set(item => item.TotalParkCount, document.TotalParkCount)
            .Set(item => item.SucceededParkCount, document.SucceededParkCount)
            .Set(item => item.FailedParkCount, document.FailedParkCount)
            .Set(item => item.SkippedParkCount, document.SkippedParkCount)
            .Set(item => item.WarningParkCount, document.WarningParkCount)
            .Set(item => item.Message, document.Message);

        await this.runsCollection.UpdateOneAsync(
            item => item.Id == document.Id,
            update,
            cancellationToken: cancellationToken);
    }

    public async Task UpsertItemAsync(ParkWeatherRunItem item, CancellationToken cancellationToken)
    {
        ParkWeatherRunItemDocument document = item.ToDocument();
        DateTime now = DateTime.UtcNow;
        FilterDefinition<ParkWeatherRunItemDocument> filter = Builders<ParkWeatherRunItemDocument>.Filter.Eq(current => current.RunId, document.RunId)
            & Builders<ParkWeatherRunItemDocument>.Filter.Eq(current => current.ParkId, document.ParkId);

        UpdateDefinition<ParkWeatherRunItemDocument> update = Builders<ParkWeatherRunItemDocument>.Update
            .SetOnInsert(current => current.Id, document.Id)
            .SetOnInsert(current => current.CreatedAt, now)
            .Set(current => current.UpdatedAt, now)
            .Set(current => current.ParkName, document.ParkName)
            .Set(current => current.Status, document.Status)
            .Set(current => current.AttemptCount, document.AttemptCount)
            .Set(current => current.StartedAtUtc, document.StartedAtUtc)
            .Set(current => current.CompletedAtUtc, document.CompletedAtUtc)
            .Set(current => current.ForecastDayCount, document.ForecastDayCount)
            .Set(current => current.ObservationDayCount, document.ObservationDayCount)
            .Set(current => current.WarningMessage, document.WarningMessage)
            .Set(current => current.ErrorCode, document.ErrorCode)
            .Set(current => current.ErrorMessage, document.ErrorMessage);

        await this.itemsCollection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ParkWeatherRunItem>> GetRunItemsAsync(string runId, ParkWeatherRunItemStatus? status, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkWeatherRunItemDocument> filter = Builders<ParkWeatherRunItemDocument>.Filter.Eq(item => item.RunId, runId);
        if (status.HasValue)
        {
            filter &= Builders<ParkWeatherRunItemDocument>.Filter.Eq(item => item.Status, status.Value);
        }

        List<ParkWeatherRunItemDocument> documents = await this.itemsCollection.Find(filter)
            .SortBy(item => item.ParkName)
            .ThenBy(item => item.ParkId)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }
}
