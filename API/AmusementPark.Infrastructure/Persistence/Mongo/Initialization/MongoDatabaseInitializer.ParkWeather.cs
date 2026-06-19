using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Weather;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeParkWeatherIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkWeatherDailySnapshotDocument> snapshotsCollection =
            this.database.GetCollection<ParkWeatherDailySnapshotDocument>(this.settings.ParkWeatherDailySnapshotsCollectionName);

        List<CreateIndexModel<ParkWeatherDailySnapshotDocument>> snapshotIndexes = new List<CreateIndexModel<ParkWeatherDailySnapshotDocument>>
        {
            new CreateIndexModel<ParkWeatherDailySnapshotDocument>(
                Builders<ParkWeatherDailySnapshotDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.LocalDate)
                    .Ascending(item => item.DataKind),
                new CreateIndexOptions { Name = "idx_park_weather_snapshot_unique", Unique = true }),
            new CreateIndexModel<ParkWeatherDailySnapshotDocument>(
                Builders<ParkWeatherDailySnapshotDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.DataKind)
                    .Ascending(item => item.LocalDate),
                new CreateIndexOptions { Name = "idx_park_weather_snapshot_read" }),
        };

        await snapshotsCollection.Indexes.CreateManyAsync(snapshotIndexes, cancellationToken: cancellationToken);

        IMongoCollection<ParkWeatherRunDocument> runsCollection =
            this.database.GetCollection<ParkWeatherRunDocument>(this.settings.ParkWeatherRunsCollectionName);

        List<CreateIndexModel<ParkWeatherRunDocument>> runIndexes = new List<CreateIndexModel<ParkWeatherRunDocument>>
        {
            new CreateIndexModel<ParkWeatherRunDocument>(
                Builders<ParkWeatherRunDocument>.IndexKeys.Descending(item => item.RequestedAtUtc),
                new CreateIndexOptions { Name = "idx_park_weather_runs_requested" }),
            new CreateIndexModel<ParkWeatherRunDocument>(
                Builders<ParkWeatherRunDocument>.IndexKeys.Ascending(item => item.Status),
                new CreateIndexOptions { Name = "idx_park_weather_runs_status" }),
            new CreateIndexModel<ParkWeatherRunDocument>(
                Builders<ParkWeatherRunDocument>.IndexKeys.Ascending(item => item.CancelsAutomaticRunLocalDate),
                new CreateIndexOptions { Name = "idx_park_weather_runs_auto_cancel" }),
        };

        await runsCollection.Indexes.CreateManyAsync(runIndexes, cancellationToken: cancellationToken);

        IMongoCollection<ParkWeatherRunItemDocument> itemsCollection =
            this.database.GetCollection<ParkWeatherRunItemDocument>(this.settings.ParkWeatherRunItemsCollectionName);

        List<CreateIndexModel<ParkWeatherRunItemDocument>> itemIndexes = new List<CreateIndexModel<ParkWeatherRunItemDocument>>
        {
            new CreateIndexModel<ParkWeatherRunItemDocument>(
                Builders<ParkWeatherRunItemDocument>.IndexKeys
                    .Ascending(item => item.RunId)
                    .Ascending(item => item.ParkId),
                new CreateIndexOptions { Name = "idx_park_weather_run_items_unique", Unique = true }),
            new CreateIndexModel<ParkWeatherRunItemDocument>(
                Builders<ParkWeatherRunItemDocument>.IndexKeys
                    .Ascending(item => item.RunId)
                    .Ascending(item => item.Status)
                    .Ascending(item => item.ParkName)
                    .Ascending(item => item.ParkId),
                new CreateIndexOptions { Name = "idx_park_weather_run_items_status" }),
        };

        await itemsCollection.Indexes.CreateManyAsync(itemIndexes, cancellationToken: cancellationToken);
    }
}
