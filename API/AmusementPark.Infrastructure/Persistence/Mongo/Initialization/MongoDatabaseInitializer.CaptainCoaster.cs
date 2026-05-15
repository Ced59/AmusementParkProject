using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Configuration.Initialization;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

/// <summary>
/// Initialise les collections MongoDB métier, leurs index et les seeds de base.
/// </summary>
public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeCaptainCoasterSettingsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CaptainCoasterSettingsDocument> collection = this.database.GetCollection<CaptainCoasterSettingsDocument>(this.settings.CaptainCoasterSettingsCollectionName);
        List<CreateIndexModel<CaptainCoasterSettingsDocument>> indexes = new List<CreateIndexModel<CaptainCoasterSettingsDocument>>
        {
            new CreateIndexModel<CaptainCoasterSettingsDocument>(
                Builders<CaptainCoasterSettingsDocument>.IndexKeys.Ascending(item => item.Source),
                new CreateIndexOptions { Name = "idx_cc_settings_source_unique", Unique = true }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeCaptainCoasterParksIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CaptainCoasterParkSnapshotDocument> collection = this.database.GetCollection<CaptainCoasterParkSnapshotDocument>(this.settings.CaptainCoasterParksCollectionName);
        await this.DropIndexIfExistsAsync(collection, "idx_cc_parks_session_external_id_unique", cancellationToken);

        List<CreateIndexModel<CaptainCoasterParkSnapshotDocument>> indexes = new List<CreateIndexModel<CaptainCoasterParkSnapshotDocument>>
        {
            new CreateIndexModel<CaptainCoasterParkSnapshotDocument>(
                Builders<CaptainCoasterParkSnapshotDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.CaptainCoasterId),
                new CreateIndexOptions { Name = "idx_cc_parks_session_external_id" }),
            new CreateIndexModel<CaptainCoasterParkSnapshotDocument>(
                Builders<CaptainCoasterParkSnapshotDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_cc_parks_session_name" }),
            new CreateIndexModel<CaptainCoasterParkSnapshotDocument>(
                Builders<CaptainCoasterParkSnapshotDocument>.IndexKeys.Ascending(item => item.ScrapedAtUtc),
                new CreateIndexOptions { Name = "idx_cc_parks_scraped_at" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeCaptainCoasterCoastersIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CaptainCoasterCoasterSnapshotDocument> collection = this.database.GetCollection<CaptainCoasterCoasterSnapshotDocument>(this.settings.CaptainCoasterCoastersCollectionName);
        await this.DropIndexIfExistsAsync(collection, "idx_cc_coasters_session_external_id_unique", cancellationToken);

        List<CreateIndexModel<CaptainCoasterCoasterSnapshotDocument>> indexes = new List<CreateIndexModel<CaptainCoasterCoasterSnapshotDocument>>
        {
            new CreateIndexModel<CaptainCoasterCoasterSnapshotDocument>(
                Builders<CaptainCoasterCoasterSnapshotDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.CaptainCoasterId),
                new CreateIndexOptions { Name = "idx_cc_coasters_session_external_id" }),
            new CreateIndexModel<CaptainCoasterCoasterSnapshotDocument>(
                Builders<CaptainCoasterCoasterSnapshotDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.ParkCaptainCoasterId),
                new CreateIndexOptions { Name = "idx_cc_coasters_session_park_external_id" }),
            new CreateIndexModel<CaptainCoasterCoasterSnapshotDocument>(
                Builders<CaptainCoasterCoasterSnapshotDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.ParkName),
                new CreateIndexOptions { Name = "idx_cc_coasters_session_park_name" }),
            new CreateIndexModel<CaptainCoasterCoasterSnapshotDocument>(
                Builders<CaptainCoasterCoasterSnapshotDocument>.IndexKeys.Ascending(item => item.Manufacturer),
                new CreateIndexOptions { Name = "idx_cc_coasters_manufacturer" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeCaptainCoasterDiscoveredUrlsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CaptainCoasterDiscoveredUrlDocument> collection = this.database.GetCollection<CaptainCoasterDiscoveredUrlDocument>(this.settings.CaptainCoasterDiscoveredUrlsCollectionName);
        List<CreateIndexModel<CaptainCoasterDiscoveredUrlDocument>> indexes = new List<CreateIndexModel<CaptainCoasterDiscoveredUrlDocument>>
        {
            new CreateIndexModel<CaptainCoasterDiscoveredUrlDocument>(
                Builders<CaptainCoasterDiscoveredUrlDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.Sequence),
                new CreateIndexOptions { Name = "idx_cc_discovered_urls_session_sequence" }),
            new CreateIndexModel<CaptainCoasterDiscoveredUrlDocument>(
                Builders<CaptainCoasterDiscoveredUrlDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.CaptainCoasterId),
                new CreateIndexOptions { Name = "idx_cc_discovered_urls_session_external_id" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeCaptainCoasterSyncSessionsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CaptainCoasterSyncSessionDocument> collection = this.database.GetCollection<CaptainCoasterSyncSessionDocument>(this.settings.CaptainCoasterSyncSessionsCollectionName);
        List<CreateIndexModel<CaptainCoasterSyncSessionDocument>> indexes = new List<CreateIndexModel<CaptainCoasterSyncSessionDocument>>
        {
            new CreateIndexModel<CaptainCoasterSyncSessionDocument>(
                Builders<CaptainCoasterSyncSessionDocument>.IndexKeys.Descending(item => item.StartedAtUtc),
                new CreateIndexOptions { Name = "idx_cc_sessions_started_at_desc" }),
            new CreateIndexModel<CaptainCoasterSyncSessionDocument>(
                Builders<CaptainCoasterSyncSessionDocument>.IndexKeys.Ascending(item => item.Status).Descending(item => item.StartedAtUtc),
                new CreateIndexOptions { Name = "idx_cc_sessions_status_started_at" }),
            new CreateIndexModel<CaptainCoasterSyncSessionDocument>(
                Builders<CaptainCoasterSyncSessionDocument>.IndexKeys.Ascending(item => item.SourceKey).Descending(item => item.StartedAtUtc),
                new CreateIndexOptions { Name = "idx_cc_sessions_source_started_at" }),
            new CreateIndexModel<CaptainCoasterSyncSessionDocument>(
                Builders<CaptainCoasterSyncSessionDocument>.IndexKeys.Ascending(item => item.SourceKey).Ascending(item => item.Status).Descending(item => item.StartedAtUtc),
                new CreateIndexOptions { Name = "idx_cc_sessions_source_status_started_at" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeCaptainCoasterComparisonResultsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CaptainCoasterComparisonResultDocument> collection = this.database.GetCollection<CaptainCoasterComparisonResultDocument>(this.settings.CaptainCoasterComparisonResultsCollectionName);
        List<CreateIndexModel<CaptainCoasterComparisonResultDocument>> indexes = new List<CreateIndexModel<CaptainCoasterComparisonResultDocument>>
        {
            new CreateIndexModel<CaptainCoasterComparisonResultDocument>(
                Builders<CaptainCoasterComparisonResultDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.EntityType).Ascending(item => item.ChangeType).Ascending(item => item.DisplayName),
                new CreateIndexOptions { Name = "idx_cc_comparisons_session_entity_change_display" }),
            new CreateIndexModel<CaptainCoasterComparisonResultDocument>(
                Builders<CaptainCoasterComparisonResultDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.IsApplied),
                new CreateIndexOptions { Name = "idx_cc_comparisons_session_is_applied" }),
            new CreateIndexModel<CaptainCoasterComparisonResultDocument>(
                Builders<CaptainCoasterComparisonResultDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.ChangeType),
                new CreateIndexOptions { Name = "idx_cc_comparisons_session_change_type" }),
            new CreateIndexModel<CaptainCoasterComparisonResultDocument>(
                Builders<CaptainCoasterComparisonResultDocument>.IndexKeys.Ascending(item => item.SyncSessionId).Ascending(item => item.ExternalEntityId),
                new CreateIndexOptions<CaptainCoasterComparisonResultDocument>
                {
                    Name = "idx_cc_comparisons_session_external_entity_id",
                    PartialFilterExpression = Builders<CaptainCoasterComparisonResultDocument>.Filter.Type(item => item.ExternalEntityId, BsonType.String),
                }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }
}
