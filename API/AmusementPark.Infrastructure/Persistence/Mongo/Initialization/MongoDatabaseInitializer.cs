using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.AttractionAccessConditionTypes;
using AmusementPark.Infrastructure.Configuration.Initialization;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Countries;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.TechnicalPages;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Videos;
using AmusementPark.Infrastructure.Persistence.Mongo.Migrations;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.AdminAudit;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.BackgroundJobs;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Comments;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Contact;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkGraphUpserts;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkPricing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Weather;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Seo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialPublishing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialShare;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

/// <summary>
/// Initialise les collections MongoDB métier, leurs index et les seeds de base.
/// </summary>
public sealed class MongoDatabaseInitializer
{
private async Task InitializeAttractionAccessConditionTypesIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<AttractionAccessConditionTypeDefinitionDocument> collection = this.database.GetCollection<AttractionAccessConditionTypeDefinitionDocument>(this.settings.AttractionAccessConditionTypesCollectionName);
        List<CreateIndexModel<AttractionAccessConditionTypeDefinitionDocument>> indexes = new List<CreateIndexModel<AttractionAccessConditionTypeDefinitionDocument>>
        {
            new CreateIndexModel<AttractionAccessConditionTypeDefinitionDocument>(
                Builders<AttractionAccessConditionTypeDefinitionDocument>.IndexKeys.Ascending(item => item.Key),
                new CreateIndexOptions { Name = "idx_access_condition_types_key_unique", Unique = true }),
            new CreateIndexModel<AttractionAccessConditionTypeDefinitionDocument>(
                Builders<AttractionAccessConditionTypeDefinitionDocument>.IndexKeys.Ascending(item => item.IsActive).Ascending(item => item.SortOrder).Ascending(item => item.Key),
                new CreateIndexOptions { Name = "idx_access_condition_types_active_sort" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task SeedSystemAttractionAccessConditionTypesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<AttractionAccessConditionTypeDefinitionDocument> collection = this.database.GetCollection<AttractionAccessConditionTypeDefinitionDocument>(this.settings.AttractionAccessConditionTypesCollectionName);

        foreach (AttractionAccessConditionTypeDefinitionWriteModel definition in AttractionAccessConditionTypeDefaultCatalog.BuildSystemDefinitions())
        {
            string key = AttractionAccessConditionTypeKeyNormalizer.Normalize(definition.Key);
            AttractionAccessConditionTypeDefinitionDocument? existing = await collection
                .Find(item => item.Key == key)
                .FirstOrDefaultAsync(cancellationToken);

            AttractionAccessConditionTypeDefinitionDocument document = existing ?? new AttractionAccessConditionTypeDefinitionDocument
            {
                Id = Guid.NewGuid().ToString("N"),
                Key = key,
                CreatedAt = DateTime.UtcNow,
                Labels = CommonMongoMappers.ToDocuments(definition.Labels),
                Descriptions = CommonMongoMappers.ToDocuments(definition.Descriptions),
            };

            document.LegacyType = definition.LegacyType;
            document.IsSystem = true;
            document.IsActive = true;
            if (document.Labels.Count == 0)
            {
                document.Labels = CommonMongoMappers.ToDocuments(definition.Labels);
            }

            if (document.Descriptions.Count == 0)
            {
                document.Descriptions = CommonMongoMappers.ToDocuments(definition.Descriptions);
            }

            document.SortOrder = document.SortOrder <= 0 ? definition.SortOrder : document.SortOrder;
            document.UpdatedAt = DateTime.UtcNow;

            await collection.ReplaceOneAsync(
                item => item.Key == key,
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
    }

private async Task InitializeAdminAuditIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<AdminAuditLogDocument> collection = this.database.GetCollection<AdminAuditLogDocument>(this.settings.AdminAuditLogsCollectionName);

        CreateIndexModel<AdminAuditLogDocument>[] indexes =
        {
            new CreateIndexModel<AdminAuditLogDocument>(
                Builders<AdminAuditLogDocument>.IndexKeys.Descending(document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_admin_audit_occurred_at_desc" }),
            new CreateIndexModel<AdminAuditLogDocument>(
                Builders<AdminAuditLogDocument>.IndexKeys
                    .Ascending(document => document.ActorUserId)
                    .Descending(document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_admin_audit_actor_occurred_at" }),
            new CreateIndexModel<AdminAuditLogDocument>(
                Builders<AdminAuditLogDocument>.IndexKeys
                    .Ascending(document => document.Action)
                    .Descending(document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_admin_audit_action_occurred_at" }),
            new CreateIndexModel<AdminAuditLogDocument>(
                Builders<AdminAuditLogDocument>.IndexKeys
                    .Ascending(document => document.EntityType)
                    .Ascending(document => document.EntityId)
                    .Descending(document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_admin_audit_entity_occurred_at" }),
            new CreateIndexModel<AdminAuditLogDocument>(
                Builders<AdminAuditLogDocument>.IndexKeys.Ascending(document => document.TraceId),
                new CreateIndexOptions { Name = "idx_admin_audit_trace_id" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

private const string AdminFieldModeItemProgressCollectionName = "adminFieldModeItemProgress";

    private async Task InitializeAdminFieldModeItemProgressAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<BsonDocument> collection = this.database.GetCollection<BsonDocument>(AdminFieldModeItemProgressCollectionName);

        await this.CleanupAdminFieldModeItemProgressAsync(collection, cancellationToken);

        CreateIndexModel<BsonDocument>[] indexes =
        [
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys
                    .Ascending("parkId")
                    .Ascending("itemId"),
                new CreateIndexOptions
                {
                    Name = "ux_admin_field_mode_progress_park_item",
                    Unique = true,
                }),
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys
                    .Ascending("parkId")
                    .Ascending("isProcessed"),
                new CreateIndexOptions
                {
                    Name = "ix_admin_field_mode_progress_park_processed",
                }),
        ];

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private async Task CleanupAdminFieldModeItemProgressAsync(IMongoCollection<BsonDocument> collection, CancellationToken cancellationToken)
    {
        FilterDefinition<BsonDocument> invalidFilter = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.Type("parkId", BsonType.String)),
            Builders<BsonDocument>.Filter.Not(Builders<BsonDocument>.Filter.Type("itemId", BsonType.String)),
            Builders<BsonDocument>.Filter.Eq("parkId", string.Empty),
            Builders<BsonDocument>.Filter.Eq("itemId", string.Empty));

        await collection.DeleteManyAsync(invalidFilter, cancellationToken);

        List<BsonDocument> documents = await collection.Find(Builders<BsonDocument>.Filter.Empty).ToListAsync(cancellationToken);
        BsonValue[] duplicateIds = documents
            .Where(static document => document.GetValue("_id", BsonNull.Value) != BsonNull.Value)
            .GroupBy(static document => string.Concat(document.GetValue("parkId").AsString, "|", document.GetValue("itemId").AsString), StringComparer.Ordinal)
            .SelectMany(static group => group
                .OrderByDescending(static document => document.GetValue("updatedAtUtc", BsonNull.Value).IsValidDateTime
                    ? document.GetValue("updatedAtUtc").ToUniversalTime()
                    : DateTime.MinValue)
                .Skip(1)
                .Select(static document => document.GetValue("_id")))
            .ToArray();

        if (duplicateIds.Length == 0)
        {
            return;
        }

        await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.In("_id", duplicateIds), cancellationToken);
    }

    private async Task InitializeFactualChangeIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<FactualChangeOutboxDocument> outbox =
            this.database.GetCollection<FactualChangeOutboxDocument>(
                this.settings.FactualChangeOutboxCollectionName);
        IMongoCollection<FactualChangeEventDocument> events =
            this.database.GetCollection<FactualChangeEventDocument>(
                this.settings.FactualChangeEventsCollectionName);
        await outbox.Indexes.CreateManyAsync(BuildFactualChangeOutboxIndexes(), cancellationToken);
        await events.Indexes.CreateManyAsync(BuildFactualChangeEventIndexes(), cancellationToken);
    }

    internal static IReadOnlyCollection<CreateIndexModel<FactualChangeOutboxDocument>>
        BuildFactualChangeOutboxIndexes()
    {
        return new List<CreateIndexModel<FactualChangeOutboxDocument>>
        {
            new CreateIndexModel<FactualChangeOutboxDocument>(
                Builders<FactualChangeOutboxDocument>.IndexKeys
                    .Ascending(static value => value.DeduplicationKey)
                    .Ascending(static value => value.SourceRevision),
                new CreateIndexOptions
                {
                    Name = "idx_factual_outbox_logical_revision_unique",
                    Unique = true,
                }),
            new CreateIndexModel<FactualChangeOutboxDocument>(
                Builders<FactualChangeOutboxDocument>.IndexKeys
                    .Ascending(static value => value.MaterializedAtUtc)
                    .Ascending(static value => value.TerminalAtUtc)
                    .Ascending(static value => value.CreatedAt)
                    .Ascending(static value => value.Id),
                new CreateIndexOptions { Name = "idx_factual_outbox_pending" }),
        };
    }

    internal static IReadOnlyCollection<CreateIndexModel<FactualChangeEventDocument>>
        BuildFactualChangeEventIndexes()
    {
        return new List<CreateIndexModel<FactualChangeEventDocument>>
        {
            new CreateIndexModel<FactualChangeEventDocument>(
                Builders<FactualChangeEventDocument>.IndexKeys
                    .Ascending(static value => value.DeduplicationKey)
                    .Ascending(static value => value.Revision),
                new CreateIndexOptions
                {
                    Name = "idx_factual_events_logical_revision_unique",
                    Unique = true,
                }),
            new CreateIndexModel<FactualChangeEventDocument>(
                Builders<FactualChangeEventDocument>.IndexKeys
                    .Ascending(static value => value.Status)
                    .Descending(static value => value.CreatedAt),
                new CreateIndexOptions { Name = "idx_factual_events_status_created" }),
            new CreateIndexModel<FactualChangeEventDocument>(
                Builders<FactualChangeEventDocument>.IndexKeys
                    .Ascending("target.type")
                    .Ascending("target.targetId")
                    .Descending(static value => value.CreatedAt),
                new CreateIndexOptions { Name = "idx_factual_events_target_created" }),
            new CreateIndexModel<FactualChangeEventDocument>(
                Builders<FactualChangeEventDocument>.IndexKeys
                    .Ascending(static value => value.Status)
                    .Ascending(static value => value.PublishedAtUtc)
                    .Ascending(static value => value.Id),
                new CreateIndexOptions { Name = "idx_factual_events_published_distribution" }),
            new CreateIndexModel<FactualChangeEventDocument>(
                Builders<FactualChangeEventDocument>.IndexKeys
                    .Ascending(static value => value.Status)
                    .Ascending(static value => value.TerminalAtUtc)
                    .Ascending(static value => value.Id),
                new CreateIndexOptions { Name = "idx_factual_events_terminal_distribution" }),
            new CreateIndexModel<FactualChangeEventDocument>(
                Builders<FactualChangeEventDocument>.IndexKeys
                    .Ascending(static value => value.Status)
                    .Ascending(static value => value.SupersededByEventId),
                new CreateIndexOptions { Name = "idx_factual_events_correction_lookup" }),
            new CreateIndexModel<FactualChangeEventDocument>(
                Builders<FactualChangeEventDocument>.IndexKeys
                    .Ascending(static value => value.VerifiedAtUtc),
                new CreateIndexOptions { Name = "idx_factual_events_pilot_verified" }),
            new CreateIndexModel<FactualChangeEventDocument>(
                Builders<FactualChangeEventDocument>.IndexKeys
                    .Ascending(static value => value.PublishedAtUtc),
                new CreateIndexOptions { Name = "idx_factual_events_pilot_published" }),
        };
    }

private async Task InitializeDurableBackgroundJobIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<DurableBackgroundJobDocument> collection =
            this.database.GetCollection<DurableBackgroundJobDocument>(this.settings.DurableBackgroundJobsCollectionName);
        await collection.Indexes.CreateManyAsync(BuildDurableBackgroundJobIndexes(), cancellationToken);
    }

    internal static IReadOnlyCollection<CreateIndexModel<DurableBackgroundJobDocument>> BuildDurableBackgroundJobIndexes()
    {
        BsonDocument exactJobFilter = new BsonDocument(
            "idempotencyKey",
            new BsonDocument("$type", "string"));
        BsonDocument activeCoalescibleJobFilter = new BsonDocument
        {
            { "naturalKey", new BsonDocument("$type", "string") },
            {
                "status",
                new BsonDocument("$in", new BsonArray
                {
                    DurableBackgroundJobStatus.Pending.ToString(),
                    DurableBackgroundJobStatus.Leased.ToString(),
                    DurableBackgroundJobStatus.RetryScheduled.ToString(),
                })
            },
        };
        BsonDocument activeJobFilter = new BsonDocument(
            "status",
            new BsonDocument("$in", new BsonArray
            {
                DurableBackgroundJobStatus.Pending.ToString(),
                DurableBackgroundJobStatus.Leased.ToString(),
                DurableBackgroundJobStatus.RetryScheduled.ToString(),
            }));
        BsonDocument scheduledJobFilter = new BsonDocument(
            "status",
            new BsonDocument("$in", new BsonArray
            {
                DurableBackgroundJobStatus.Pending.ToString(),
                DurableBackgroundJobStatus.RetryScheduled.ToString(),
            }));
        BsonDocument leasedJobFilter = new BsonDocument(
            "status",
            DurableBackgroundJobStatus.Leased.ToString());

        return new List<CreateIndexModel<DurableBackgroundJobDocument>>
        {
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys
                    .Ascending(item => item.Kind)
                    .Ascending(item => item.IdempotencyKey),
                new CreateIndexOptions<DurableBackgroundJobDocument>
                {
                    Name = "idx_background_jobs_exact_unique",
                    Unique = true,
                    PartialFilterExpression = exactJobFilter,
                }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys
                    .Ascending(item => item.Kind)
                    .Ascending(item => item.NaturalKey),
                new CreateIndexOptions<DurableBackgroundJobDocument>
                {
                    Name = "idx_background_jobs_active_natural_key_unique",
                    Unique = true,
                    PartialFilterExpression = activeCoalescibleJobFilter,
                }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys.Ascending(item => item.Kind),
                new CreateIndexOptions<DurableBackgroundJobDocument>
                {
                    Name = "idx_background_jobs_active_kind_scan",
                    PartialFilterExpression = activeJobFilter,
                }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys
                    .Ascending(item => item.Kind)
                    .Descending(item => item.Priority)
                    .Ascending(item => item.NotBeforeUtc)
                    .Ascending(item => item.CreatedAt),
                new CreateIndexOptions<DurableBackgroundJobDocument>
                {
                    Name = "idx_background_jobs_runnable",
                    PartialFilterExpression = scheduledJobFilter,
                }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys
                    .Ascending(item => item.Kind)
                    .Descending(item => item.Priority)
                    .Ascending(item => item.LeaseExpiresAtUtc)
                    .Ascending(item => item.CreatedAt),
                new CreateIndexOptions<DurableBackgroundJobDocument>
                {
                    Name = "idx_background_jobs_expired_claim",
                    PartialFilterExpression = leasedJobFilter,
                }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys.Ascending(item => item.LeaseExpiresAtUtc),
                new CreateIndexOptions<DurableBackgroundJobDocument>
                {
                    Name = "idx_background_jobs_lease_expiry",
                    PartialFilterExpression = leasedJobFilter,
                }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys.Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_background_jobs_diagnostics_recent" }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys
                    .Ascending(item => item.Status)
                    .Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_background_jobs_diagnostics_status" }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys
                    .Ascending(item => item.Kind)
                    .Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_background_jobs_diagnostics_kind" }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys
                    .Ascending(item => item.Kind)
                    .Ascending(item => item.Status)
                    .Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_background_jobs_diagnostics" }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys
                    .Ascending(item => item.Kind)
                    .Ascending(item => item.NaturalKey)
                    .Ascending(item => item.Status)
                    .Ascending(item => item.RequestedRevision)
                    .Ascending(item => item.PayloadVersion),
                new CreateIndexOptions { Name = "idx_background_jobs_terminal_revision" }),
        };
    }

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

private async Task InitializeCommentsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CommentDocument> collection =
            this.database.GetCollection<CommentDocument>(this.settings.CommentsCollectionName);
        await collection.UpdateManyAsync(
            BuildLegacyCommentRevisionFilter(),
            BuildLegacyCommentRevisionUpdate(),
            cancellationToken: cancellationToken);

        List<CreateIndexModel<CommentDocument>> indexes = new List<CreateIndexModel<CommentDocument>>
        {
            new CreateIndexModel<CommentDocument>(
                Builders<CommentDocument>.IndexKeys
                    .Ascending(static document => document.TargetType)
                    .Ascending(static document => document.TargetId)
                    .Ascending(static document => document.ModerationStatus)
                    .Descending(static document => document.IsOfficial)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "idx_comments_public_target" }),
            new CreateIndexModel<CommentDocument>(
                Builders<CommentDocument>.IndexKeys
                    .Ascending(static document => document.AuthorUserId)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "idx_comments_author_created" }),
            new CreateIndexModel<CommentDocument>(
                Builders<CommentDocument>.IndexKeys
                    .Ascending(static document => document.ParkId)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "idx_comments_park_created" }),
            new CreateIndexModel<CommentDocument>(
                Builders<CommentDocument>.IndexKeys
                    .Ascending(static document => document.ImageIds),
                new CreateIndexOptions { Name = "idx_comments_image_ids" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    internal static FilterDefinition<CommentDocument> BuildLegacyCommentRevisionFilter()
    {
        return Builders<CommentDocument>.Filter.Exists(
            static document => document.Revision,
            false);
    }

    internal static UpdateDefinition<CommentDocument> BuildLegacyCommentRevisionUpdate()
    {
        return Builders<CommentDocument>.Update.Set(
            static document => document.Revision,
            0L);
    }

private async Task InitializeContactGrievanceIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ContactGrievanceDocument> collection = this.database.GetCollection<ContactGrievanceDocument>(this.settings.ContactGrievancesCollectionName);
        List<CreateIndexModel<ContactGrievanceDocument>> indexes = new List<CreateIndexModel<ContactGrievanceDocument>>
        {
            new CreateIndexModel<ContactGrievanceDocument>(
                Builders<ContactGrievanceDocument>.IndexKeys.Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "idx_contact_grievances_created_desc" }),
            new CreateIndexModel<ContactGrievanceDocument>(
                Builders<ContactGrievanceDocument>.IndexKeys
                    .Ascending(static document => document.IpAddress)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "idx_contact_grievances_ip_created_desc" }),
            new CreateIndexModel<ContactGrievanceDocument>(
                Builders<ContactGrievanceDocument>.IndexKeys
                    .Ascending(static document => document.LanguageCode)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "idx_contact_grievances_language_created_desc" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

private readonly IMongoDatabase database;
    private readonly MongoDbSettings settings;
    private readonly AdminSeedSettings adminSeedSettings;
    private readonly IHostEnvironment hostEnvironment;
    private readonly ILogger<MongoDatabaseInitializer> logger;
    private readonly PersonalRankingShareReplacementMigration personalRankingShareMigration;
    private readonly PersonalRankingShareAvatarPolicyMigration personalRankingShareAvatarPolicyMigration;

    public MongoDatabaseInitializer(
        IMongoDatabase database,
        MongoDbSettings settings,
        AdminSeedSettings adminSeedSettings,
        IHostEnvironment hostEnvironment,
        ILogger<MongoDatabaseInitializer> logger,
        PersonalRankingShareReplacementMigration personalRankingShareMigration,
        PersonalRankingShareAvatarPolicyMigration personalRankingShareAvatarPolicyMigration)
    {
        this.database = database;
        this.settings = settings;
        this.adminSeedSettings = adminSeedSettings;
        this.hostEnvironment = hostEnvironment;
        this.logger = logger;
        this.personalRankingShareMigration = personalRankingShareMigration;
        this.personalRankingShareAvatarPolicyMigration = personalRankingShareAvatarPolicyMigration;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await this.EnsureCollectionExistsAsync(this.settings.CountersCollectionName, cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.DurableBackgroundJobsCollectionName, cancellationToken);
        await this.InitializeDurableBackgroundJobIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.FactualChangeOutboxCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.FactualChangeEventsCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.FactualEventMigrationsCollectionName, cancellationToken);
        await this.InitializeFactualChangeIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.UsersCollectionName, cancellationToken);
        await this.InitializeUsersIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.RefreshTokensCollectionName, cancellationToken);
        await this.InitializeRefreshTokensIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkDataEditorAccessTokensCollectionName, cancellationToken);
        await this.InitializeParkDataEditorAccessTokensIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ImagesCollectionName, cancellationToken);
        await this.BackfillLegacyImageCategoriesAsync(cancellationToken);
        await this.InitializeImagesIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ImageTagsCollectionName, cancellationToken);
        await this.InitializeImageTagsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.VideosCollectionName, cancellationToken);
        await this.InitializeVideosIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.VideoTagsCollectionName, cancellationToken);
        await this.InitializeVideoTagsIndexesAsync(cancellationToken);
        await this.SeedSystemVideoTagsAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ContactGrievancesCollectionName, cancellationToken);
        await this.InitializeContactGrievanceIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.SocialShareEventsCollectionName, cancellationToken);
        await this.InitializeSocialShareEventIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.SocialPublicationsCollectionName, cancellationToken);
        await this.InitializeSocialPublicationIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.UserRatingsCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.RatingAggregatesCollectionName, cancellationToken);
        await this.InitializeRatingsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.SharePublicationsCollectionName, cancellationToken);
        IMongoCollection<SharePublicationDocument> sharePublicationsCollection =
            this.database.GetCollection<SharePublicationDocument>(
                this.settings.SharePublicationsCollectionName);
        await sharePublicationsCollection.UpdateManyAsync(
            Builders<SharePublicationDocument>.Filter.Exists(
                "moderationSuspensionReportIds",
                false),
            Builders<SharePublicationDocument>.Update.Set(
                static document => document.ModerationSuspensionReportIds,
                new List<string>()),
            cancellationToken: cancellationToken);
        SharePublicationModerationSourceLockMigration moderationSourceLockMigration =
            new SharePublicationModerationSourceLockMigration(sharePublicationsCollection);
        await moderationSourceLockMigration.MigrateAsync(cancellationToken);
        await sharePublicationsCollection.Indexes.CreateManyAsync(
            SharePublicationMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.ProfileComparisonInvitationsCollectionName,
            cancellationToken);
        IMongoCollection<ProfileComparisonInvitationDocument> comparisonInvitationsCollection =
            this.database.GetCollection<ProfileComparisonInvitationDocument>(
                this.settings.ProfileComparisonInvitationsCollectionName);
        await comparisonInvitationsCollection.Indexes.CreateManyAsync(
            ProfileComparisonInvitationMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.ProfileComparisonsCollectionName,
            cancellationToken);
        IMongoCollection<ProfileComparisonDocument> comparisonsCollection =
            this.database.GetCollection<ProfileComparisonDocument>(
                this.settings.ProfileComparisonsCollectionName);
        await comparisonsCollection.UpdateManyAsync(
            Builders<ProfileComparisonDocument>.Filter.Exists(
                "moderationSuspensionReportIds",
                false),
            Builders<ProfileComparisonDocument>.Update.Set(
                static document => document.ModerationSuspensionReportIds,
                new List<string>()),
            cancellationToken: cancellationToken);
        await this.DropIndexIfExistsAsync(
            comparisonsCollection,
            ProfileComparisonMongoDefinitions.LegacyCreatorIndexName,
            cancellationToken);
        await this.DropIndexIfExistsAsync(
            comparisonsCollection,
            ProfileComparisonMongoDefinitions.LegacyAcceptorIndexName,
            cancellationToken);
        await comparisonsCollection.Indexes.CreateManyAsync(
            ProfileComparisonMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.UserGroupProfilesCollectionName,
            cancellationToken);
        IMongoCollection<ParkFitGroupProfileDocument> groupProfilesCollection =
            this.database.GetCollection<ParkFitGroupProfileDocument>(
                this.settings.UserGroupProfilesCollectionName);
        await groupProfilesCollection.Indexes.CreateManyAsync(
            ParkFitGroupProfileMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.UserCollectionEntriesCollectionName,
            cancellationToken);
        IMongoCollection<UserCollectionEntryDocument> userCollectionEntries =
            this.database.GetCollection<UserCollectionEntryDocument>(
                this.settings.UserCollectionEntriesCollectionName);
        await userCollectionEntries.Indexes.CreateManyAsync(
            UserCollectionEntryMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.WatchSubscriptionsCollectionName,
            cancellationToken);
        IMongoCollection<WatchSubscriptionDocument> watchSubscriptions =
            this.database.GetCollection<WatchSubscriptionDocument>(
                this.settings.WatchSubscriptionsCollectionName);
        await watchSubscriptions.Indexes.CreateManyAsync(
            WatchNotificationMongoDefinitions.BuildSubscriptionIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.UserNotificationsCollectionName,
            cancellationToken);
        IMongoCollection<UserNotificationDocument> userNotifications =
            this.database.GetCollection<UserNotificationDocument>(
                this.settings.UserNotificationsCollectionName);
        await userNotifications.Indexes.CreateManyAsync(
            WatchNotificationMongoDefinitions.BuildNotificationIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.NotificationDigestsCollectionName,
            cancellationToken);
        IMongoCollection<NotificationDigestDocument> notificationDigests =
            this.database.GetCollection<NotificationDigestDocument>(
                this.settings.NotificationDigestsCollectionName);
        await notificationDigests.Indexes.CreateManyAsync(
            NotificationDigestMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.NotificationPreferencesCollectionName,
            cancellationToken);
        IMongoCollection<NotificationEmailPreferenceDocument> notificationPreferences =
            this.database.GetCollection<NotificationEmailPreferenceDocument>(
                this.settings.NotificationPreferencesCollectionName);
        await notificationPreferences.Indexes.CreateManyAsync(
            NotificationEmailPreferenceMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.NotificationDeliveryAttemptsCollectionName,
            cancellationToken);
        IMongoCollection<NotificationDeliveryAttemptDocument> notificationDeliveryAttempts =
            this.database.GetCollection<NotificationDeliveryAttemptDocument>(
                this.settings.NotificationDeliveryAttemptsCollectionName);
        await notificationDeliveryAttempts.Indexes.CreateManyAsync(
            NotificationDeliveryAttemptMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.WatchlistAccountDeletionFencesCollectionName,
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.WatchlistAccountDeletionLeasesCollectionName,
            cancellationToken);
        IMongoCollection<WatchlistAccountDeletionLeaseDocument> watchlistAccountDeletionLeases =
            this.database.GetCollection<WatchlistAccountDeletionLeaseDocument>(
                this.settings.WatchlistAccountDeletionLeasesCollectionName);
        await watchlistAccountDeletionLeases.Indexes.CreateManyAsync(
            WatchlistAccountDeletionLeaseMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.FactualNotificationDistributionsCollectionName,
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.WatchPilotDailyMetricsCollectionName,
            cancellationToken);
        IMongoCollection<WatchPilotDailyMetricsDocument> watchPilotDailyMetrics =
            this.database.GetCollection<WatchPilotDailyMetricsDocument>(
                this.settings.WatchPilotDailyMetricsCollectionName);
        await watchPilotDailyMetrics.Indexes.CreateManyAsync(
            WatchPilotMetricsMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.ParkFitSourceReportsCollectionName,
            cancellationToken);
        IMongoCollection<ParkFitSourceReportDocument> parkFitSourceReportsCollection =
            this.database.GetCollection<ParkFitSourceReportDocument>(
                this.settings.ParkFitSourceReportsCollectionName);
        await parkFitSourceReportsCollection.Indexes.CreateManyAsync(
            ParkFitSourceReportMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.ParkFitPilotDailyMetricsCollectionName,
            cancellationToken);
        IMongoCollection<ParkFitPilotDailyMetricsDocument> parkFitPilotMetricsCollection =
            this.database.GetCollection<ParkFitPilotDailyMetricsDocument>(
                this.settings.ParkFitPilotDailyMetricsCollectionName);
        await parkFitPilotMetricsCollection.Indexes.CreateManyAsync(
            ParkFitPilotMetricsMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.ParkFitOperationalStatusesCollectionName,
            cancellationToken);
        IMongoCollection<ParkFitOperationalStatusDocument> parkFitOperationalStatusesCollection =
            this.database.GetCollection<ParkFitOperationalStatusDocument>(
                this.settings.ParkFitOperationalStatusesCollectionName);
        await parkFitOperationalStatusesCollection.Indexes.CreateManyAsync(
            ParkFitOperationalStatusMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.ParkFitPortfolioMigrationsCollectionName,
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.ShareModerationReportsCollectionName,
            cancellationToken);
        IMongoCollection<ShareModerationReportDocument> moderationReportsCollection =
            this.database.GetCollection<ShareModerationReportDocument>(
                this.settings.ShareModerationReportsCollectionName);
        await moderationReportsCollection.Indexes.CreateManyAsync(
            ShareModerationReportMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.SharePublicationSnapshotsCollectionName,
            cancellationToken);
        IMongoCollection<VisitRecapShareSnapshotDocument> shareSnapshotsCollection =
            this.database.GetCollection<VisitRecapShareSnapshotDocument>(
                this.settings.SharePublicationSnapshotsCollectionName);
        IMongoCollection<PassportProfileShareSnapshotDocument> passportShareSnapshotsCollection =
            this.database.GetCollection<PassportProfileShareSnapshotDocument>(
                this.settings.SharePublicationSnapshotsCollectionName);
        IMongoCollection<ParkDocument> passportShareParksCollection =
            this.database.GetCollection<ParkDocument>(this.settings.ParksCollectionName);
        PassportProfileShareSelectedParksMigration passportSelectedParksMigration =
            new PassportProfileShareSelectedParksMigration(
                passportShareSnapshotsCollection,
                passportShareParksCollection);
        await passportSelectedParksMigration.MigrateAsync(cancellationToken);
        await shareSnapshotsCollection.Indexes.CreateManyAsync(
            VisitRecapShareSnapshotMongoDefinitions.BuildIndexes(),
            cancellationToken);

        await this.EnsureCollectionExistsAsync(
            this.settings.ShareSourceRevisionsCollectionName,
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.PassportProfileShareScopeRegistrationsCollectionName,
            cancellationToken);
        IMongoCollection<PassportProfileShareScopeRegistrationDocument>
            passportProfileShareScopeRegistrationsCollection =
                this.database.GetCollection<PassportProfileShareScopeRegistrationDocument>(
                    this.settings.PassportProfileShareScopeRegistrationsCollectionName);
        await passportProfileShareScopeRegistrationsCollection.Indexes.CreateManyAsync(
            PassportProfileShareScopeRegistrationMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.SharePublicationMigrationsCollectionName,
            cancellationToken);
        await this.personalRankingShareMigration.ExecuteAsync(cancellationToken);
        await this.personalRankingShareAvatarPolicyMigration.ExecuteAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.TripPlansCollectionName, cancellationToken);
        IMongoCollection<TripPlanDocument> tripPlans =
            this.database.GetCollection<TripPlanDocument>(this.settings.TripPlansCollectionName);
        await MigrateTripPlanProgramFoundationAsync(tripPlans, cancellationToken);
        await this.DropIndexIfExistsAsync(
            tripPlans,
            TripPlanMongoDefinitions.LegacyOwnerScopeOperationIndexName,
            cancellationToken);
        await tripPlans.Indexes.CreateManyAsync(
            TripPlanMongoDefinitions.BuildIndexes(),
            cancellationToken);
        await this.DropIndexIfExistsAsync(
            tripPlans,
            TripPlanMongoDefinitions.LegacyOwnerOperationIndexName,
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.TripParkCandidatesCollectionName,
            cancellationToken);
        IMongoCollection<TripParkCandidateDocument> tripParkCandidates =
            this.database.GetCollection<TripParkCandidateDocument>(
                this.settings.TripParkCandidatesCollectionName);
        await tripParkCandidates.Indexes.CreateManyAsync(
            TripParkCandidateRepository.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.TripDayPlansCollectionName,
            cancellationToken);
        IMongoCollection<TripDayPlanDocument> tripDayPlans =
            this.database.GetCollection<TripDayPlanDocument>(this.settings.TripDayPlansCollectionName);
        await tripDayPlans.Indexes.CreateManyAsync(
            TripDayPlanRepository.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.TripItemPreferencesCollectionName,
            cancellationToken);
        IMongoCollection<TripItemPreferenceDocument> tripItemPreferences =
            this.database.GetCollection<TripItemPreferenceDocument>(
                this.settings.TripItemPreferencesCollectionName);
        await tripItemPreferences.Indexes.CreateManyAsync(
            TripPreferenceRepository.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.TripItemDecisionsCollectionName,
            cancellationToken);
        IMongoCollection<TripItemDecisionDocument> tripItemDecisions =
            this.database.GetCollection<TripItemDecisionDocument>(
                this.settings.TripItemDecisionsCollectionName);
        await tripItemDecisions.Indexes.CreateManyAsync(
            TripItemDecisionRepository.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.TripInvitationsCollectionName,
            cancellationToken);
        IMongoCollection<TripInvitationDocument> tripInvitations =
            this.database.GetCollection<TripInvitationDocument>(
                this.settings.TripInvitationsCollectionName);
        await tripInvitations.Indexes.CreateManyAsync(
            TripInvitationRepository.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.TripAuditEventsCollectionName,
            cancellationToken);
        IMongoCollection<TripActivityEventDocument> tripAuditEvents =
            this.database.GetCollection<TripActivityEventDocument>(
                this.settings.TripAuditEventsCollectionName);
        await tripAuditEvents.Indexes.CreateManyAsync(
            TripAuditRepository.BuildIndexes(),
            cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.UserVisitsCollectionName, cancellationToken);
        await this.InitializeUserVisitIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(
            this.settings.UserRideOccurrencesCollectionName,
            cancellationToken);
        await this.InitializeUserRideOccurrenceIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(
            this.settings.UserRideOccurrenceOperationsCollectionName,
            cancellationToken);
        await this.InitializeUserRideOccurrenceOperationIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(
            this.settings.PassportAuditEventsCollectionName,
            cancellationToken);
        await this.InitializePassportAuditIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(
            this.settings.PassportExportsCollectionName,
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.PassportExportChunksCollectionName,
            cancellationToken);
        await this.InitializePassportExportIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(
            this.settings.GlobalRatingSuggestionStatesCollectionName,
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.GlobalRatingSuggestionPreferencesCollectionName,
            cancellationToken);
        await this.EnsureCollectionExistsAsync(
            this.settings.GlobalRatingSuggestionInteractionsCollectionName,
            cancellationToken);
        await this.InitializeGlobalRatingSuggestionIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.RatingRankingSnapshotHeadersCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.RatingRankingSnapshotChunksCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.RatingRankingPublicationPointersCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.RatingRankingSourceRevisionsCollectionName, cancellationToken);
        await this.InitializeRankingSnapshotIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CommentsCollectionName, cancellationToken);
        await this.InitializeCommentsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CountriesCollectionName, cancellationToken);
        await this.InitializeCountriesIndexesAsync(cancellationToken);
        await this.SeedCountriesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParksCollectionName, cancellationToken);
        await this.InitializeParksIndexesAsync(cancellationToken);
        ParkFitPortfolioStateMigration parkFitPortfolioStateMigration =
            new ParkFitPortfolioStateMigration(
                this.database.GetCollection<ParkDocument>(this.settings.ParksCollectionName),
                parkFitOperationalStatusesCollection,
                this.database.GetCollection<ParkFitPortfolioMigrationDocument>(
                    this.settings.ParkFitPortfolioMigrationsCollectionName));
        long initializedParkFitParkCount =
            await parkFitPortfolioStateMigration.MigrateAsync(cancellationToken);
        if (initializedParkFitParkCount > 0)
        {
            this.logger.LogInformation(
                "Park Fit portfolio migration initialized {ParkCount} parks as not activated.",
                initializedParkFitParkCount);
        }

        await this.EnsureCollectionExistsAsync(this.settings.ParkOpeningHoursCollectionName, cancellationToken);
        await this.InitializeParkOpeningHoursIndexesAsync(cancellationToken);
        OpeningCalendarFactualEvidenceMigration openingCalendarEvidenceMigration = new OpeningCalendarFactualEvidenceMigration(
            this.database.GetCollection<FactualChangeEventDocument>(
                this.settings.FactualChangeEventsCollectionName),
            this.database.GetCollection<FactualChangeOutboxDocument>(
                this.settings.FactualChangeOutboxCollectionName),
            this.database.GetCollection<ParkOpeningHoursScheduleDocument>(
                this.settings.ParkOpeningHoursCollectionName),
            this.database.GetCollection<FactualEventMigrationDocument>(
                this.settings.FactualEventMigrationsCollectionName),
            this.settings.CompleteFactualEventMigrationsOnStartup);
        long migratedOpeningCalendarEvidenceCount =
            await openingCalendarEvidenceMigration.MigrateAsync(cancellationToken);
        if (migratedOpeningCalendarEvidenceCount > 0)
        {
            this.logger.LogInformation(
                "Migrated {FactCount} persisted opening-calendar facts to contextual evidence schema 2.",
                migratedOpeningCalendarEvidenceCount);
        }

        await this.EnsureCollectionExistsAsync(this.settings.ParkPricingCollectionName, cancellationToken);
        await this.InitializeParkPricingIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.HistoryEventsCollectionName, cancellationToken);
        await this.InitializeHistoryEventsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkFoundersCollectionName, cancellationToken);
        await this.InitializeParkFoundersIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkOperatorsCollectionName, cancellationToken);
        await this.InitializeParkOperatorsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.AttractionManufacturersCollectionName, cancellationToken);
        await this.InitializeAttractionManufacturersIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.TechnicalPagesCollectionName, cancellationToken);
        await this.InitializeTechnicalPagesIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkZonesCollectionName, cancellationToken);
        await this.InitializeParkZonesIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkItemsCollectionName, cancellationToken);
        await this.InitializeParkItemsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.StandaloneAttractionsCollectionName, cancellationToken);
        await this.InitializeStandaloneAttractionsIndexesAsync(cancellationToken);

        AttractionAccessConditionProvenanceMigration parkItemAccessConditionMigration =
            new AttractionAccessConditionProvenanceMigration(
                this.database.GetCollection<BsonDocument>(this.settings.ParkItemsCollectionName));
        long migratedParkItemCount = await parkItemAccessConditionMigration.MigrateAsync(cancellationToken);
        AttractionAccessConditionProvenanceMigration standaloneAccessConditionMigration =
            new AttractionAccessConditionProvenanceMigration(
                this.database.GetCollection<BsonDocument>(this.settings.StandaloneAttractionsCollectionName));
        long migratedStandaloneCount = await standaloneAccessConditionMigration.MigrateAsync(cancellationToken);
        this.logger.LogInformation(
            "Migrated access-condition provenance on {ParkItemCount} park items and {StandaloneCount} standalone attractions.",
            migratedParkItemCount,
            migratedStandaloneCount);

        await this.EnsureCollectionExistsAsync(AdminFieldModeItemProgressCollectionName, cancellationToken);
        await this.InitializeAdminFieldModeItemProgressAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.AttractionAccessConditionTypesCollectionName, cancellationToken);
        await this.InitializeAttractionAccessConditionTypesIndexesAsync(cancellationToken);
        await this.SeedSystemAttractionAccessConditionTypesAsync(cancellationToken);

        await this.BackfillAdminReviewPrioritiesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.SearchItemCollectionName, cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.AdminAuditLogsCollectionName, cancellationToken);
        await this.InitializeAdminAuditIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.SeoSitemapSnapshotsCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.SeoSitemapSnapshotSectionsCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.SeoSitemapGenerationHistoryCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.SeoSitemapSettingsCollectionName, cancellationToken);
        await this.InitializeSeoSitemapIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkGraphUpsertHistoryCollectionName, cancellationToken);
        await this.InitializeParkGraphUpsertHistoryIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CaptainCoasterSettingsCollectionName, cancellationToken);
        await this.InitializeCaptainCoasterSettingsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CaptainCoasterParksCollectionName, cancellationToken);
        await this.InitializeCaptainCoasterParksIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CaptainCoasterCoastersCollectionName, cancellationToken);
        await this.InitializeCaptainCoasterCoastersIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CaptainCoasterDiscoveredUrlsCollectionName, cancellationToken);
        await this.InitializeCaptainCoasterDiscoveredUrlsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CaptainCoasterSyncSessionsCollectionName, cancellationToken);
        await this.InitializeCaptainCoasterSyncSessionsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.CaptainCoasterComparisonResultsCollectionName, cancellationToken);
        await this.InitializeCaptainCoasterComparisonResultsIndexesAsync(cancellationToken);

        await this.EnsureCollectionExistsAsync(this.settings.ParkWeatherDailySnapshotsCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.ParkWeatherRunsCollectionName, cancellationToken);
        await this.EnsureCollectionExistsAsync(this.settings.ParkWeatherRunItemsCollectionName, cancellationToken);
        await this.InitializeParkWeatherIndexesAsync(cancellationToken);

        await this.InitializeAdminUserAsync(cancellationToken);
        await this.BackfillPublicAccountIdentitiesAsync(cancellationToken);
        await this.InitializeUserPublicDisplayNameIndexAsync(cancellationToken);
        await this.BackfillLegacyCommentAuthorSnapshotsAsync(cancellationToken);
    }

    private async Task EnsureCollectionExistsAsync(string collectionName, CancellationToken cancellationToken)
    {
        BsonDocument filter = new BsonDocument("name", collectionName);
        ListCollectionsOptions options = new ListCollectionsOptions
        {
            Filter = filter,
        };

        using IAsyncCursor<BsonDocument> collections = await this.database.ListCollectionsAsync(options, cancellationToken);
        bool exists = await collections.AnyAsync(cancellationToken);

        if (!exists)
        {
            await this.database.CreateCollectionAsync(collectionName, cancellationToken: cancellationToken);
        }
    }

    private async Task DropIndexIfExistsAsync<TDocument>(IMongoCollection<TDocument> collection, string indexName, CancellationToken cancellationToken)
    {
        using IAsyncCursor<BsonDocument> cursor = await collection.Indexes.ListAsync(cancellationToken);
        List<BsonDocument> indexes = await cursor.ToListAsync(cancellationToken);
        bool exists = indexes.Any(item => item.TryGetValue("name", out BsonValue value) && value.AsString == indexName);
        if (exists)
        {
            await collection.Indexes.DropOneAsync(indexName, cancellationToken);
        }
    }

    private async Task InitializeStandaloneAttractionsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<StandaloneAttractionDocument> collection = this.database.GetCollection<StandaloneAttractionDocument>(this.settings.StandaloneAttractionsCollectionName);
        CreateIndexModel<StandaloneAttractionDocument>[] indexes =
        {
            new CreateIndexModel<StandaloneAttractionDocument>(
                Builders<StandaloneAttractionDocument>.IndexKeys.Ascending(document => document.Name),
                new CreateIndexOptions { Name = "standaloneAttractions_name" }),
            new CreateIndexModel<StandaloneAttractionDocument>(
                Builders<StandaloneAttractionDocument>.IndexKeys
                    .Ascending(document => document.IsVisible)
                    .Ascending(document => document.AdminReviewPriority)
                    .Ascending(document => document.Name),
                new CreateIndexOptions { Name = "standaloneAttractions_adminList" }),
            new CreateIndexModel<StandaloneAttractionDocument>(
                Builders<StandaloneAttractionDocument>.IndexKeys
                    .Ascending(document => document.LegacyParkId)
                    .Ascending(document => document.LegacyParkItemId),
                new CreateIndexOptions { Name = "standaloneAttractions_legacy" }),
            new CreateIndexModel<StandaloneAttractionDocument>(
                Builders<StandaloneAttractionDocument>.IndexKeys.Geo2DSphere(document => document.Location),
                new CreateIndexOptions { Name = "standaloneAttractions_location" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

private async Task InitializeHistoryEventsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<HistoryEventDocument> collection = this.database.GetCollection<HistoryEventDocument>(this.settings.HistoryEventsCollectionName);
        List<CreateIndexModel<HistoryEventDocument>> indexes = new List<CreateIndexModel<HistoryEventDocument>>
        {
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.EntityType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.Key),
                new CreateIndexOptions { Name = "idx_history_owner_key_unique", Unique = true }),
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.EntityType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.Year)
                    .Ascending(item => item.Month)
                    .Ascending(item => item.Day),
                new CreateIndexOptions { Name = "idx_history_owner_visible_date" }),
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.EntityType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.Year)
                    .Ascending(item => item.Month)
                    .Ascending(item => item.Day)
                    .Ascending(item => item.Key)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_history_owner_date" }),
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.ContextParkId)
                    .Ascending(item => item.EntityType)
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.Year)
                    .Ascending(item => item.Month)
                    .Ascending(item => item.Day),
                new CreateIndexOptions { Name = "idx_history_context_park_visible_date" }),
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.ContextParkId)
                    .Ascending(item => item.EntityType)
                    .Ascending(item => item.Year)
                    .Ascending(item => item.Month)
                    .Ascending(item => item.Day)
                    .Ascending(item => item.Key)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_history_context_park_date" }),
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.Year)
                    .Ascending(item => item.Month)
                    .Ascending(item => item.Day),
                new CreateIndexOptions { Name = "idx_history_park_visible_date" }),
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.ParkItemId)
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.Year)
                    .Ascending(item => item.Month)
                    .Ascending(item => item.Day),
                new CreateIndexOptions { Name = "idx_history_park_item_visible_date" }),
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.IsMajor)
                    .Ascending("article.isPublished")
                    .Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_history_public_articles" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

private async Task InitializeParkGraphUpsertHistoryIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkGraphUpsertHistoryDocument> collection = this.database.GetCollection<ParkGraphUpsertHistoryDocument>(this.settings.ParkGraphUpsertHistoryCollectionName);
        List<CreateIndexModel<ParkGraphUpsertHistoryDocument>> indexes = new List<CreateIndexModel<ParkGraphUpsertHistoryDocument>>
        {
            new CreateIndexModel<ParkGraphUpsertHistoryDocument>(
                Builders<ParkGraphUpsertHistoryDocument>.IndexKeys.Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_park_graph_upsert_history_created_at_desc" }),
            new CreateIndexModel<ParkGraphUpsertHistoryDocument>(
                Builders<ParkGraphUpsertHistoryDocument>.IndexKeys.Ascending(item => item.TargetParkId).Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_park_graph_upsert_history_park_created_at" }),
            new CreateIndexModel<ParkGraphUpsertHistoryDocument>(
                Builders<ParkGraphUpsertHistoryDocument>.IndexKeys.Ascending(item => item.OperationKind).Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_park_graph_upsert_history_kind_created_at" }),
            new CreateIndexModel<ParkGraphUpsertHistoryDocument>(
                Builders<ParkGraphUpsertHistoryDocument>.IndexKeys.Ascending(item => item.ExpiresAt),
                new CreateIndexOptions
                {
                    Name = "idx_park_graph_upsert_history_expires_at_ttl",
                    ExpireAfter = TimeSpan.Zero,
                }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);

        DateTime fallbackExpirationUtc = DateTime.UtcNow.AddDays(Math.Max(1, this.settings.ParkGraphUpsertHistoryRetentionDays));
        FilterDefinition<ParkGraphUpsertHistoryDocument> missingExpirationFilter =
            Builders<ParkGraphUpsertHistoryDocument>.Filter.Exists(item => item.ExpiresAt, false);
        UpdateDefinition<ParkGraphUpsertHistoryDocument> setExpirationUpdate =
            Builders<ParkGraphUpsertHistoryDocument>.Update.Set(item => item.ExpiresAt, fallbackExpirationUtc);

        await collection.UpdateManyAsync(missingExpirationFilter, setExpirationUpdate, cancellationToken: cancellationToken);
    }

private async Task InitializeParkOpeningHoursIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkOpeningHoursScheduleDocument> collection =
            this.database.GetCollection<ParkOpeningHoursScheduleDocument>(this.settings.ParkOpeningHoursCollectionName);

        IReadOnlyCollection<CreateIndexModel<ParkOpeningHoursScheduleDocument>> indexes =
            BuildParkOpeningHoursIndexes();

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    internal static IReadOnlyCollection<CreateIndexModel<ParkOpeningHoursScheduleDocument>> BuildParkOpeningHoursIndexes()
    {
        return new List<CreateIndexModel<ParkOpeningHoursScheduleDocument>>
        {
            new CreateIndexModel<ParkOpeningHoursScheduleDocument>(
                Builders<ParkOpeningHoursScheduleDocument>.IndexKeys.Ascending(item => item.ParkId),
                new CreateIndexOptions { Name = "idx_park_opening_hours_park_id_unique", Unique = true }),
            new CreateIndexModel<ParkOpeningHoursScheduleDocument>(
                Builders<ParkOpeningHoursScheduleDocument>.IndexKeys.Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_park_opening_hours_updated" }),
            new CreateIndexModel<ParkOpeningHoursScheduleDocument>(
                Builders<ParkOpeningHoursScheduleDocument>.IndexKeys
                    .Ascending(item => item.UpdatedAt)
                    .Ascending(item => item.ParkId),
                new CreateIndexOptions<ParkOpeningHoursScheduleDocument>
                {
                    Name = "idx_park_opening_hours_pending_factual_changes",
                    PartialFilterExpression = ParkOpeningHoursRepository.BuildPendingFactualChangeFilter(null),
                }),
        };
    }

private async Task InitializeParkPricingIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkPricingDocument> collection =
            this.database.GetCollection<ParkPricingDocument>(this.settings.ParkPricingCollectionName);

        IReadOnlyCollection<CreateIndexModel<ParkPricingDocument>> indexes = BuildParkPricingIndexes();

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    internal static IReadOnlyCollection<CreateIndexModel<ParkPricingDocument>> BuildParkPricingIndexes()
    {
        return new List<CreateIndexModel<ParkPricingDocument>>
        {
            new CreateIndexModel<ParkPricingDocument>(
                Builders<ParkPricingDocument>.IndexKeys.Ascending(item => item.ParkId),
                new CreateIndexOptions { Name = "idx_park_pricing_park_id_unique", Unique = true }),
            new CreateIndexModel<ParkPricingDocument>(
                Builders<ParkPricingDocument>.IndexKeys.Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_park_pricing_updated" }),
        };
    }

private async Task InitializeParksIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkDocument> parksCollection = this.database.GetCollection<ParkDocument>(this.settings.ParksCollectionName);
        await BackfillParkRandomSortKeysAsync(parksCollection, cancellationToken);

        List<CreateIndexModel<ParkDocument>> indexes = new List<CreateIndexModel<ParkDocument>>
        {
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_parks_name" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.CountryCode),
                new CreateIndexOptions { Name = "idx_parks_country_code" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.IsVisible).Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_parks_visibility_updated" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.IsVisible).Ascending(item => item.Name).Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_parks_visibility_name_id" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.IsVisible).Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_parks_visibility_id" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Ascending(item => item.IsVisible).Ascending(item => item.CountryCode),
                new CreateIndexOptions { Name = "idx_parks_visibility_country_code" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.AudienceClassification)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_parks_visibility_audience_classification_name_id" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.RandomSortKey)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_parks_visibility_random_sort_key" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.CountryCode)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_parks_public_country_name_id" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys
                    .Ascending(item => item.IsVisible)
                    .Descending(item => item.AdminReviewStatus)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_parks_public_review_status_name_id" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.IsFeaturedOnHome)
                    .Ascending(item => item.FeaturedHomeOrder)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_parks_home_featured" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys.Geo2DSphere(item => item.Location),
                new CreateIndexOptions { Name = "idx_parks_location" }),
            new CreateIndexModel<ParkDocument>(
                Builders<ParkDocument>.IndexKeys
                    .Ascending(item => item.AdminReviewPriority)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_parks_admin_review_priority_name" }),
        };

        await parksCollection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeParkFoundersIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkFounderDocument> collection = this.database.GetCollection<ParkFounderDocument>(this.settings.ParkFoundersCollectionName);
        List<CreateIndexModel<ParkFounderDocument>> indexes = new List<CreateIndexModel<ParkFounderDocument>>
        {
            new CreateIndexModel<ParkFounderDocument>(
                Builders<ParkFounderDocument>.IndexKeys.Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_founders_name_unique", Unique = true }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeParkOperatorsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkOperatorDocument> collection = this.database.GetCollection<ParkOperatorDocument>(this.settings.ParkOperatorsCollectionName);
        List<CreateIndexModel<ParkOperatorDocument>> indexes = new List<CreateIndexModel<ParkOperatorDocument>>
        {
            new CreateIndexModel<ParkOperatorDocument>(
                Builders<ParkOperatorDocument>.IndexKeys.Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_operators_name_unique", Unique = true }),
            new CreateIndexModel<ParkOperatorDocument>(
                Builders<ParkOperatorDocument>.IndexKeys
                    .Ascending(item => item.AdminReviewPriority)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_park_operators_admin_review_priority_name" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeAttractionManufacturersIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<AttractionManufacturerDocument> collection = this.database.GetCollection<AttractionManufacturerDocument>(this.settings.AttractionManufacturersCollectionName);
        List<CreateIndexModel<AttractionManufacturerDocument>> indexes = new List<CreateIndexModel<AttractionManufacturerDocument>>
        {
            new CreateIndexModel<AttractionManufacturerDocument>(
                Builders<AttractionManufacturerDocument>.IndexKeys.Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_attraction_manufacturers_name_unique", Unique = true }),
            new CreateIndexModel<AttractionManufacturerDocument>(
                Builders<AttractionManufacturerDocument>.IndexKeys
                    .Ascending(item => item.AdminReviewPriority)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_attraction_manufacturers_admin_review_priority_name" }),
            new CreateIndexModel<AttractionManufacturerDocument>(
                Builders<AttractionManufacturerDocument>.IndexKeys
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_attraction_manufacturers_visibility_name" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeParkZonesIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkZoneDocument> collection = this.database.GetCollection<ParkZoneDocument>(this.settings.ParkZonesCollectionName);
        List<CreateIndexModel<ParkZoneDocument>> indexes = new List<CreateIndexModel<ParkZoneDocument>>
        {
            new CreateIndexModel<ParkZoneDocument>(
                Builders<ParkZoneDocument>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_zones_park_name" }),
            new CreateIndexModel<ParkZoneDocument>(
                Builders<ParkZoneDocument>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_park_zones_park_slug" }),
            new CreateIndexModel<ParkZoneDocument>(
                Builders<ParkZoneDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.SortOrder)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_park_zones_park_sort_name" }),
            new CreateIndexModel<ParkZoneDocument>(
                Builders<ParkZoneDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.SortOrder)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_park_zones_public_park_sort_name" }),
            new CreateIndexModel<ParkZoneDocument>(
                Builders<ParkZoneDocument>.IndexKeys.Geo2DSphere(item => item.Location),
                new CreateIndexOptions { Name = "idx_park_zones_location" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeParkItemsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkItemDocument> collection = this.database.GetCollection<ParkItemDocument>(this.settings.ParkItemsCollectionName);
        List<CreateIndexModel<ParkItemDocument>> indexes = new List<CreateIndexModel<ParkItemDocument>>
        {
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_items_park_name" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.ParkId).Ascending(item => item.ZoneId),
                new CreateIndexOptions { Name = "idx_park_items_park_zone" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.Category)
                    .Ascending(item => item.Type)
                    .Ascending(item => item.Name),
                new CreateIndexOptions { Name = "idx_park_items_park_category_type_name" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.Category)
                    .Ascending(item => item.Type)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_park_items_public_park_category_type_name" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.ZoneId)
                    .Ascending(item => item.Category)
                    .Ascending(item => item.Type)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_park_items_public_park_zone_category_type_name" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.Latitude)
                    .Ascending(item => item.Longitude)
                    .Ascending(item => item.Category)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_park_items_public_park_coordinates_category_name" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.ZoneId),
                new CreateIndexOptions { Name = "idx_park_items_zone_id" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.Category).Ascending(item => item.IsVisible),
                new CreateIndexOptions { Name = "idx_park_items_category_visibility" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.Category).Ascending(item => item.IsVisible).Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_park_items_category_visibility_updated" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Ascending(item => item.Category).Ascending("attractionDetails.manufacturerId"),
                new CreateIndexOptions { Name = "idx_park_items_attraction_manufacturer" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys
                    .Ascending(item => item.Category)
                    .Ascending("attractionDetails.manufacturerId")
                    .Ascending(item => item.IsVisible),
                new CreateIndexOptions { Name = "idx_park_items_attraction_manufacturer_visibility" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys.Geo2DSphere(item => item.Location),
                new CreateIndexOptions { Name = "idx_park_items_location" }),
            new CreateIndexModel<ParkItemDocument>(
                Builders<ParkItemDocument>.IndexKeys
                    .Ascending(item => item.AdminReviewPriority)
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.Name)
                    .Ascending(item => item.Id),
                new CreateIndexOptions { Name = "idx_park_items_admin_review_priority_park_name" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeImagesIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ImageDocument> collection = this.database.GetCollection<ImageDocument>(this.settings.ImagesCollectionName);
        List<CreateIndexModel<ImageDocument>> indexes = new List<CreateIndexModel<ImageDocument>>
        {
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.OwnerType).Ascending(item => item.OwnerId).Ascending(item => item.Category).Ascending(item => item.IsCurrent),
                new CreateIndexOptions { Name = "idx_images_owner_category_current" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.OwnerType).Ascending(item => item.OwnerId),
                new CreateIndexOptions { Name = "idx_images_owner" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.OwnerType).Ascending(item => item.OwnerId).Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_images_owner_created_at_desc" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.OwnerType).Ascending(item => item.OwnerId).Ascending(item => item.IsPublished).Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_images_owner_published_created_at_desc" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys
                    .Ascending(item => item.OwnerType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.Category)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_images_owner_category_created_at_desc" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys
                    .Ascending(item => item.OwnerType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.Category)
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_images_owner_category_published_created_desc" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.Category),
                new CreateIndexOptions { Name = "idx_images_category" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_images_created_at" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending(item => item.Path),
                new CreateIndexOptions { Name = "idx_images_path" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys.Ascending("tagIds"),
                new CreateIndexOptions { Name = "idx_images_tag_ids" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys
                    .Ascending(item => item.Category)
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_images_category_published_created_desc" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys
                    .Ascending(item => item.OwnerType)
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_images_owner_type_published_created_desc" }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys
                    .Ascending(item => item.Category)
                    .Ascending(item => item.CleanupRequestedAt)
                    .Ascending(item => item.CreatedAt),
                new CreateIndexOptions<ImageDocument>
                {
                    Name = "idx_images_comment_cleanup_due",
                    PartialFilterExpression =
                        Builders<ImageDocument>.Filter.Exists(
                            static item => item.CleanupRequestedAt),
                }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys
                    .Ascending(item => item.Category)
                    .Ascending(item => item.ReservationReconcileAfter)
                    .Ascending(item => item.CreatedAt),
                new CreateIndexOptions<ImageDocument>
                {
                    Name = "idx_images_comment_reservation_reconcile_due",
                    PartialFilterExpression =
                        Builders<ImageDocument>.Filter.Exists(
                            static item => item.ReservationReconcileAfter),
                }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys
                    .Ascending(item => item.Category)
                    .Ascending(item => item.CommentReuseReconcileAfter),
                new CreateIndexOptions<ImageDocument>
                {
                    Name = "idx_images_comment_reuse_reconcile",
                    PartialFilterExpression =
                        Builders<ImageDocument>.Filter.Exists(
                            static item => item.CommentReuseReconcileAfter),
                }),
            new CreateIndexModel<ImageDocument>(
                Builders<ImageDocument>.IndexKeys
                    .Text(item => item.OriginalFileName)
                    .Text(item => item.Description)
                    .Text(item => item.Path)
                    .Text(item => item.OwnerId),
                new CreateIndexOptions { Name = "idx_images_admin_text" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task BackfillLegacyImageCategoriesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<BsonDocument> imagesCollection = this.database.GetCollection<BsonDocument>(this.settings.ImagesCollectionName);
        DateTime now = DateTime.UtcNow;

        UpdateResult parkLogoResult = await imagesCollection.UpdateManyAsync(
            Builders<BsonDocument>.Filter.Eq("category", "ParkLogo"),
            Builders<BsonDocument>.Update
                .Set("category", "Logo")
                .Set("updatedAt", now),
            cancellationToken: cancellationToken);

        if (parkLogoResult.ModifiedCount > 0)
        {
            this.logger.LogInformation("Migrated {Count} image category values from ParkLogo to Logo.", parkLogoResult.ModifiedCount);
        }

        IMongoCollection<BsonDocument> manufacturersCollection = this.database.GetCollection<BsonDocument>(this.settings.AttractionManufacturersCollectionName);
        List<BsonDocument> manufacturerLogoDocuments = await manufacturersCollection
            .Find(Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Exists("currentLogoImageId", true),
                Builders<BsonDocument>.Filter.Ne("currentLogoImageId", BsonNull.Value)))
            .Project(Builders<BsonDocument>.Projection.Include("currentLogoImageId").Exclude("_id"))
            .ToListAsync(cancellationToken);

        List<string> manufacturerLogoImageIds = manufacturerLogoDocuments
            .Select(static document => document.TryGetValue("currentLogoImageId", out BsonValue value) && value.IsString ? value.AsString : null)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (manufacturerLogoImageIds.Count == 0)
        {
            return;
        }

        UpdateResult manufacturerLogoResult = await imagesCollection.UpdateManyAsync(
            Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.In("_id", manufacturerLogoImageIds),
                Builders<BsonDocument>.Filter.Eq("ownerType", "AttractionManufacturer"),
                Builders<BsonDocument>.Filter.Ne("category", "Logo")),
            Builders<BsonDocument>.Update
                .Set("category", "Logo")
                .Set("updatedAt", now),
            cancellationToken: cancellationToken);

        if (manufacturerLogoResult.ModifiedCount > 0)
        {
            this.logger.LogInformation("Migrated {Count} current manufacturer logo images to Logo category.", manufacturerLogoResult.ModifiedCount);
        }
    }

    private async Task InitializeImageTagsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ImageTagDocument> collection = this.database.GetCollection<ImageTagDocument>(this.settings.ImageTagsCollectionName);
        List<CreateIndexModel<ImageTagDocument>> indexes = new List<CreateIndexModel<ImageTagDocument>>
        {
            new CreateIndexModel<ImageTagDocument>(
                Builders<ImageTagDocument>.IndexKeys.Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_image_tags_slug_unique", Unique = true }),
            new CreateIndexModel<ImageTagDocument>(
                Builders<ImageTagDocument>.IndexKeys.Ascending(item => item.IsActive),
                new CreateIndexOptions { Name = "idx_image_tags_is_active" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task BackfillAdminReviewPrioritiesAsync(CancellationToken cancellationToken)
    {
        await BackfillAdminReviewPriorityAsync(this.database.GetCollection<ParkDocument>(this.settings.ParksCollectionName), cancellationToken);
        await BackfillAdminReviewPriorityAsync(this.database.GetCollection<ParkItemDocument>(this.settings.ParkItemsCollectionName), cancellationToken);
        await BackfillAdminReviewPriorityAsync(this.database.GetCollection<ParkOperatorDocument>(this.settings.ParkOperatorsCollectionName), cancellationToken);
        await BackfillAdminReviewPriorityAsync(this.database.GetCollection<AttractionManufacturerDocument>(this.settings.AttractionManufacturersCollectionName), cancellationToken);
        await BackfillAdminReviewPriorityAsync(this.database.GetCollection<TechnicalPageDocument>(this.settings.TechnicalPagesCollectionName), cancellationToken);
    }

    private static async Task BackfillParkRandomSortKeysAsync(IMongoCollection<ParkDocument> collection, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> missingRandomSortKeyFilter = Builders<ParkDocument>.Filter.Or(
            Builders<ParkDocument>.Filter.Exists(document => document.RandomSortKey, false),
            Builders<ParkDocument>.Filter.Eq(document => document.RandomSortKey, null));

        List<string> parkIds = await collection.Find(missingRandomSortKeyFilter)
            .Project(document => document.Id)
            .ToListAsync(cancellationToken);

        List<WriteModel<ParkDocument>> writes = new List<WriteModel<ParkDocument>>(Math.Min(parkIds.Count, 500));
        foreach (string parkId in parkIds.Where(static id => !string.IsNullOrWhiteSpace(id)))
        {
            writes.Add(new UpdateOneModel<ParkDocument>(
                Builders<ParkDocument>.Filter.Eq(document => document.Id, parkId),
                Builders<ParkDocument>.Update.Set(document => document.RandomSortKey, Random.Shared.NextDouble())));

            if (writes.Count >= 500)
            {
                await collection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
                writes.Clear();
            }
        }

        if (writes.Count > 0)
        {
            await collection.BulkWriteAsync(writes, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
        }
    }

    private static async Task BackfillAdminReviewPriorityAsync<TDocument>(IMongoCollection<TDocument> collection, CancellationToken cancellationToken)
    {
        await collection.UpdateManyAsync(
            Builders<TDocument>.Filter.Or(
                Builders<TDocument>.Filter.Exists("adminReviewStatus", false),
                Builders<TDocument>.Filter.Eq("adminReviewStatus", BsonNull.Value)),
            Builders<TDocument>.Update
                .Set("adminReviewStatus", AdminReviewStatus.ToReview.ToString())
                .Set("adminReviewPriority", 0),
            cancellationToken: cancellationToken);

        await collection.UpdateManyAsync(
            Builders<TDocument>.Filter.Eq("adminReviewStatus", AdminReviewStatus.ToReview.ToString()),
            Builders<TDocument>.Update.Set("adminReviewPriority", 0),
            cancellationToken: cancellationToken);

        await collection.UpdateManyAsync(
            Builders<TDocument>.Filter.Or(
                Builders<TDocument>.Filter.Eq("adminReviewStatus", AdminReviewStatus.Validated.ToString()),
                Builders<TDocument>.Filter.Eq("adminReviewStatus", "Ready")),
            Builders<TDocument>.Update
                .Set("adminReviewStatus", AdminReviewStatus.Validated.ToString())
                .Set("adminReviewPriority", 10),
            cancellationToken: cancellationToken);

        await collection.UpdateManyAsync(
            Builders<TDocument>.Filter.Eq("adminReviewStatus", AdminReviewStatus.ToProcessLater.ToString()),
            Builders<TDocument>.Update.Set("adminReviewPriority", 90),
            cancellationToken: cancellationToken);

        await collection.UpdateManyAsync(
            Builders<TDocument>.Filter.Eq("adminReviewStatus", AdminReviewStatus.NotRelevant.ToString()),
            Builders<TDocument>.Update.Set("adminReviewPriority", 99),
            cancellationToken: cancellationToken);
    }

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

private async Task InitializeRankingSnapshotIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<RankingSnapshotHeaderDocument> headers = this.database.GetCollection<RankingSnapshotHeaderDocument>(
            this.settings.RatingRankingSnapshotHeadersCollectionName);
        IMongoCollection<RankingSnapshotChunkDocument> chunks = this.database.GetCollection<RankingSnapshotChunkDocument>(
            this.settings.RatingRankingSnapshotChunksCollectionName);
        IMongoCollection<RankingPublicationPointerDocument> pointers = this.database.GetCollection<RankingPublicationPointerDocument>(
            this.settings.RatingRankingPublicationPointersCollectionName);

        await headers.Indexes.CreateManyAsync(BuildRankingSnapshotHeaderIndexes(), cancellationToken);
        await chunks.Indexes.CreateManyAsync(BuildRankingSnapshotChunkIndexes(), cancellationToken);
        await pointers.Indexes.CreateManyAsync(BuildRankingPublicationPointerIndexes(), cancellationToken);
    }

    internal static IReadOnlyCollection<CreateIndexModel<RankingSnapshotHeaderDocument>> BuildRankingSnapshotHeaderIndexes()
    {
        return new List<CreateIndexModel<RankingSnapshotHeaderDocument>>
        {
            new CreateIndexModel<RankingSnapshotHeaderDocument>(
                Builders<RankingSnapshotHeaderDocument>.IndexKeys
                    .Ascending(document => document.ScopeKey)
                    .Ascending(document => document.MethodologyVersion)
                    .Ascending(document => document.SourceRevision),
                new CreateIndexOptions { Name = "idx_ranking_snapshot_header_source_unique", Unique = true }),
            new CreateIndexModel<RankingSnapshotHeaderDocument>(
                Builders<RankingSnapshotHeaderDocument>.IndexKeys
                    .Ascending(document => document.ScopeKey)
                    .Ascending(document => document.Status)
                    .Descending(document => document.GeneratedAtUtc),
                new CreateIndexOptions { Name = "idx_ranking_snapshot_header_scope_status" }),
        };
    }

    internal static IReadOnlyCollection<CreateIndexModel<RankingSnapshotChunkDocument>> BuildRankingSnapshotChunkIndexes()
    {
        return new List<CreateIndexModel<RankingSnapshotChunkDocument>>
        {
            new CreateIndexModel<RankingSnapshotChunkDocument>(
                Builders<RankingSnapshotChunkDocument>.IndexKeys
                    .Ascending(document => document.SnapshotId)
                    .Ascending(document => document.ChunkIndex),
                new CreateIndexOptions { Name = "idx_ranking_snapshot_chunk_index_unique", Unique = true }),
            new CreateIndexModel<RankingSnapshotChunkDocument>(
                Builders<RankingSnapshotChunkDocument>.IndexKeys
                    .Ascending(document => document.SnapshotId)
                    .Ascending(document => document.FirstRank)
                    .Ascending(document => document.LastRank),
                new CreateIndexOptions { Name = "idx_ranking_snapshot_chunk_rank_range" }),
            new CreateIndexModel<RankingSnapshotChunkDocument>(
                Builders<RankingSnapshotChunkDocument>.IndexKeys
                    .Ascending(document => document.ScopeKey)
                    .Ascending(document => document.UpdatedAt),
                new CreateIndexOptions { Name = "idx_ranking_snapshot_chunk_orphan_cleanup" }),
        };
    }

    internal static IReadOnlyCollection<CreateIndexModel<RankingPublicationPointerDocument>> BuildRankingPublicationPointerIndexes()
    {
        return new List<CreateIndexModel<RankingPublicationPointerDocument>>
        {
            new CreateIndexModel<RankingPublicationPointerDocument>(
                Builders<RankingPublicationPointerDocument>.IndexKeys.Ascending(document => document.ScopeKey),
                new CreateIndexOptions { Name = "idx_ranking_publication_pointer_scope_unique", Unique = true }),
        };
    }

private async Task InitializeRatingsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<UserRatingDocument> userRatingsCollection = this.database.GetCollection<UserRatingDocument>(this.settings.UserRatingsCollectionName);
        List<CreateIndexModel<UserRatingDocument>> userRatingIndexes = new List<CreateIndexModel<UserRatingDocument>>
        {
            new CreateIndexModel<UserRatingDocument>(
                Builders<UserRatingDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.TargetType)
                    .Ascending(static document => document.TargetId),
                new CreateIndexOptions { Name = "idx_user_ratings_user_target_unique", Unique = true }),
            new CreateIndexModel<UserRatingDocument>(
                Builders<UserRatingDocument>.IndexKeys
                    .Ascending(static document => document.TargetType)
                    .Ascending(static document => document.TargetId),
                new CreateIndexOptions { Name = "idx_user_ratings_target" }),
            new CreateIndexModel<UserRatingDocument>(
                Builders<UserRatingDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Descending(static document => document.UpdatedAt),
                new CreateIndexOptions { Name = "idx_user_ratings_user_updated" }),
            new CreateIndexModel<UserRatingDocument>(
                Builders<UserRatingDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.ParkId),
                new CreateIndexOptions { Name = "idx_user_ratings_user_park" }),
        };

        await userRatingsCollection.Indexes.CreateManyAsync(userRatingIndexes, cancellationToken: cancellationToken);

        IMongoCollection<RatingAggregateDocument> ratingAggregatesCollection = this.database.GetCollection<RatingAggregateDocument>(this.settings.RatingAggregatesCollectionName);
        List<CreateIndexModel<RatingAggregateDocument>> ratingAggregateIndexes = new List<CreateIndexModel<RatingAggregateDocument>>
        {
            new CreateIndexModel<RatingAggregateDocument>(
                Builders<RatingAggregateDocument>.IndexKeys
                    .Ascending(static document => document.TargetType)
                    .Ascending(static document => document.TargetId),
                new CreateIndexOptions { Name = "idx_rating_aggregates_target_unique", Unique = true }),
            new CreateIndexModel<RatingAggregateDocument>(
                Builders<RatingAggregateDocument>.IndexKeys
                    .Descending(static document => document.BayesianScore)
                    .Descending(static document => document.RatingCount)
                    .Descending(static document => document.AverageRating),
                new CreateIndexOptions { Name = "idx_rating_aggregates_ranking" }),
            new CreateIndexModel<RatingAggregateDocument>(
                Builders<RatingAggregateDocument>.IndexKeys
                    .Ascending(static document => document.TargetType)
                    .Descending(static document => document.BayesianScore)
                    .Descending(static document => document.RatingCount),
                new CreateIndexOptions { Name = "idx_rating_aggregates_type_ranking" }),
            new CreateIndexModel<RatingAggregateDocument>(
                Builders<RatingAggregateDocument>.IndexKeys
                    .Ascending(static document => document.ParkItemCategory)
                    .Descending(static document => document.BayesianScore)
                    .Descending(static document => document.RatingCount),
                new CreateIndexOptions { Name = "idx_rating_aggregates_category_ranking" }),
        };

        await ratingAggregatesCollection.Indexes.CreateManyAsync(ratingAggregateIndexes, cancellationToken: cancellationToken);

    }

private async Task InitializeSeoSitemapIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<SeoSitemapSnapshotSectionChunkDocument> sectionChunksCollection =
            this.database.GetCollection<SeoSitemapSnapshotSectionChunkDocument>(this.settings.SeoSitemapSnapshotSectionsCollectionName);

        List<CreateIndexModel<SeoSitemapSnapshotSectionChunkDocument>> sectionChunkIndexes =
            new List<CreateIndexModel<SeoSitemapSnapshotSectionChunkDocument>>
            {
                new CreateIndexModel<SeoSitemapSnapshotSectionChunkDocument>(
                    Builders<SeoSitemapSnapshotSectionChunkDocument>.IndexKeys
                        .Ascending(item => item.SnapshotId)
                        .Ascending(item => item.StorageId)
                        .Ascending(item => item.SectionKey)
                        .Ascending(item => item.ChunkIndex),
                    new CreateIndexOptions { Name = "idx_seo_sitemap_section_chunks_lookup", Unique = true }),
            };

        await sectionChunksCollection.Indexes.CreateManyAsync(sectionChunkIndexes, cancellationToken: cancellationToken);
    }

private async Task InitializeSocialPublicationIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<SocialPublicationDocument> collection = this.database
            .GetCollection<SocialPublicationDocument>(this.settings.SocialPublicationsCollectionName);
        List<CreateIndexModel<SocialPublicationDocument>> indexes = new List<CreateIndexModel<SocialPublicationDocument>>
        {
            new CreateIndexModel<SocialPublicationDocument>(
                Builders<SocialPublicationDocument>.IndexKeys.Descending(static document => document.RequestedAtUtc),
                new CreateIndexOptions { Name = "idx_social_publications_requested_desc" }),
            new CreateIndexModel<SocialPublicationDocument>(
                Builders<SocialPublicationDocument>.IndexKeys.Ascending(static document => document.DeduplicationKey),
                new CreateIndexOptions<SocialPublicationDocument>
                {
                    Name = "idx_social_publications_deduplication_unique",
                    Unique = true,
                    PartialFilterExpression = Builders<SocialPublicationDocument>.Filter.Type(
                        static document => document.DeduplicationKey,
                        MongoDB.Bson.BsonType.String),
                }),
            new CreateIndexModel<SocialPublicationDocument>(
                Builders<SocialPublicationDocument>.IndexKeys
                    .Ascending(static document => document.Network)
                    .Ascending(static document => document.Status)
                    .Descending(static document => document.RequestedAtUtc),
                new CreateIndexOptions { Name = "idx_social_publications_network_status" }),
            new CreateIndexModel<SocialPublicationDocument>(
                Builders<SocialPublicationDocument>.IndexKeys.Ascending(static document => document.ExternalPostId),
                new CreateIndexOptions<SocialPublicationDocument>
                {
                    Name = "idx_social_publications_external_post_id",
                    PartialFilterExpression = Builders<SocialPublicationDocument>.Filter.Type(
                        static document => document.ExternalPostId,
                        MongoDB.Bson.BsonType.String),
                }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

private async Task InitializeSocialShareEventIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<SocialShareEventDocument> collection = this.database.GetCollection<SocialShareEventDocument>(this.settings.SocialShareEventsCollectionName);
        List<CreateIndexModel<SocialShareEventDocument>> indexes = new List<CreateIndexModel<SocialShareEventDocument>>
        {
            new CreateIndexModel<SocialShareEventDocument>(
                Builders<SocialShareEventDocument>.IndexKeys.Descending(static document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_social_share_events_occurred_desc" }),
            new CreateIndexModel<SocialShareEventDocument>(
                Builders<SocialShareEventDocument>.IndexKeys
                    .Ascending(static document => document.Channel)
                    .Descending(static document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_social_share_events_channel_occurred" }),
            new CreateIndexModel<SocialShareEventDocument>(
                Builders<SocialShareEventDocument>.IndexKeys
                    .Ascending(static document => document.TargetType)
                    .Descending(static document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_social_share_events_target_occurred" }),
            new CreateIndexModel<SocialShareEventDocument>(
                Builders<SocialShareEventDocument>.IndexKeys
                    .Ascending(static document => document.VisitorKind)
                    .Descending(static document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_social_share_events_visitor_occurred" }),
            new CreateIndexModel<SocialShareEventDocument>(
                Builders<SocialShareEventDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Descending(static document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_social_share_events_user_occurred" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

private async Task InitializeTechnicalPagesIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<TechnicalPageDocument> collection = this.database.GetCollection<TechnicalPageDocument>(this.settings.TechnicalPagesCollectionName);
        List<CreateIndexModel<TechnicalPageDocument>> indexes = new List<CreateIndexModel<TechnicalPageDocument>>
        {
            new CreateIndexModel<TechnicalPageDocument>(
                Builders<TechnicalPageDocument>.IndexKeys.Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_technical_pages_slug_unique", Unique = true }),
            new CreateIndexModel<TechnicalPageDocument>(
                Builders<TechnicalPageDocument>.IndexKeys
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.CategoryKey)
                    .Ascending(item => item.SortOrder)
                    .Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_technical_pages_public_category_sort_slug" }),
            new CreateIndexModel<TechnicalPageDocument>(
                Builders<TechnicalPageDocument>.IndexKeys
                    .Ascending(item => item.AdminReviewPriority)
                    .Ascending(item => item.CategoryKey)
                    .Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_technical_pages_admin_review_category_slug" }),
            new CreateIndexModel<TechnicalPageDocument>(
                Builders<TechnicalPageDocument>.IndexKeys.Ascending("aliases.categoryKey").Ascending("aliases.labels.value"),
                new CreateIndexOptions { Name = "idx_technical_pages_aliases" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

private async Task InitializeParkDataEditorAccessTokensIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkDataEditorAccessTokenDocument> collection =
            this.database.GetCollection<ParkDataEditorAccessTokenDocument>(
                this.settings.ParkDataEditorAccessTokensCollectionName);
        List<CreateIndexModel<ParkDataEditorAccessTokenDocument>> indexes =
            new List<CreateIndexModel<ParkDataEditorAccessTokenDocument>>
            {
                new CreateIndexModel<ParkDataEditorAccessTokenDocument>(
                    Builders<ParkDataEditorAccessTokenDocument>.IndexKeys
                        .Ascending(item => item.UserId)
                        .Descending(item => item.CreatedAt),
                    new CreateIndexOptions { Name = "idx_park_data_editor_tokens_user_created" }),
                new CreateIndexModel<ParkDataEditorAccessTokenDocument>(
                    Builders<ParkDataEditorAccessTokenDocument>.IndexKeys
                        .Ascending(item => item.UserId)
                        .Ascending(item => item.RevokedAtUtc)
                        .Ascending(item => item.ExpiresAtUtc),
                    new CreateIndexOptions { Name = "idx_park_data_editor_tokens_active" }),
            };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private async Task InitializeRefreshTokensIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<RefreshTokenDocument> collection = this.database.GetCollection<RefreshTokenDocument>(this.settings.RefreshTokensCollectionName);

        List<CreateIndexModel<RefreshTokenDocument>> indexes = new List<CreateIndexModel<RefreshTokenDocument>>
        {
            new CreateIndexModel<RefreshTokenDocument>(
                Builders<RefreshTokenDocument>.IndexKeys.Ascending(item => item.TokenHash),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "ux_refresh_tokens_tokenHash",
                }),
            new CreateIndexModel<RefreshTokenDocument>(
                Builders<RefreshTokenDocument>.IndexKeys.Ascending(item => item.UserId).Descending(item => item.ExpiresAtUtc),
                new CreateIndexOptions
                {
                    Name = "ix_refresh_tokens_userId_expiresAtUtc",
                }),
            new CreateIndexModel<RefreshTokenDocument>(
                Builders<RefreshTokenDocument>.IndexKeys.Ascending(item => item.ExpiresAtUtc),
                new CreateIndexOptions
                {
                    Name = "ix_refresh_tokens_expiresAtUtc_ttl",
                    ExpireAfter = TimeSpan.Zero,
                }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private async Task InitializeAdminUserAsync(CancellationToken cancellationToken)
    {
        if (!this.adminSeedSettings.Enabled)
        {
            this.logger.LogDebug("Admin seed is disabled.");
            return;
        }

        if (!this.hostEnvironment.IsDevelopment())
        {
            this.logger.LogWarning("Admin seed is enabled but ignored because the current environment is {EnvironmentName}. Admin seed is reserved for local development.", this.hostEnvironment.EnvironmentName);
            return;
        }

        if (string.IsNullOrWhiteSpace(this.adminSeedSettings.Email))
        {
            this.logger.LogWarning("Admin seed ignored because Initialization:AdminUser:Email is empty.");
            return;
        }

        IMongoCollection<UserDocument> usersCollection = this.database.GetCollection<UserDocument>(this.settings.UsersCollectionName);
        string normalizedEmail = this.adminSeedSettings.Email.Trim().ToLowerInvariant();

        UserDocument? existingUser = await usersCollection
            .Find(user => user.Email == normalizedEmail)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUser is null)
        {
            if (string.IsNullOrWhiteSpace(this.adminSeedSettings.Password))
            {
                this.logger.LogWarning("Admin seed ignored because Initialization:AdminUser:Password is empty. Configure it through user-secrets or environment variables when local admin seeding is required.");
                return;
            }

            DateTime now = DateTime.UtcNow;
            UserDocument adminUser = new UserDocument
            {
                Email = normalizedEmail,
                FirstName = string.IsNullOrWhiteSpace(this.adminSeedSettings.FirstName) ? "Admin" : this.adminSeedSettings.FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(this.adminSeedSettings.LastName) ? "User" : this.adminSeedSettings.LastName.Trim(),
                PreferredLanguage = string.IsNullOrWhiteSpace(this.adminSeedSettings.PreferredLanguage)
                    ? "FR"
                    : this.adminSeedSettings.PreferredLanguage.Trim().ToUpperInvariant(),
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(this.adminSeedSettings.Password),
                IsActivated = true,
                IsBlocked = false,
                Roles = new List<Role>
                {
                    Role.User,
                    Role.Moderator,
                    Role.Admin,
                },
                CreatedAt = now,
                UpdatedAt = now,
                LastLoginUtc = now,
                LastActivityUtc = now,
            };

            await usersCollection.InsertOneAsync(adminUser, cancellationToken: cancellationToken);
            return;
        }

        bool needsUpdate = false;

        if (!existingUser.IsActivated)
        {
            existingUser.IsActivated = true;
            needsUpdate = true;
        }

        if (existingUser.IsBlocked)
        {
            existingUser.IsBlocked = false;
            needsUpdate = true;
        }

        foreach (Role role in new[] { Role.User, Role.Moderator, Role.Admin })
        {
            if (!existingUser.Roles.Contains(role))
            {
                existingUser.Roles.Add(role);
                needsUpdate = true;
            }
        }

        if (string.IsNullOrWhiteSpace(existingUser.HashedPassword) && !string.IsNullOrWhiteSpace(this.adminSeedSettings.Password))
        {
            existingUser.HashedPassword = BCrypt.Net.BCrypt.HashPassword(this.adminSeedSettings.Password);
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            existingUser.UpdatedAt = DateTime.UtcNow;
            await usersCollection.ReplaceOneAsync(user => user.Id == existingUser.Id, existingUser, cancellationToken: cancellationToken);
        }
    }

    private async Task SeedCountriesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CountryDocument> countriesCollection = this.database.GetCollection<CountryDocument>(this.settings.CountriesCollectionName);
        long documentCount = await countriesCollection.EstimatedDocumentCountAsync(cancellationToken: cancellationToken);

        if (documentCount > 0)
        {
            return;
        }

        string jsonFilePath = this.ResolveCountriesSeedPath();
        if (!File.Exists(jsonFilePath))
        {
            this.logger.LogWarning("Countries seed JSON file was not found at path {JsonFilePath}.", jsonFilePath);
            return;
        }

        string json = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
        };

        List<CountrySeedItem>? seedItems = JsonSerializer.Deserialize<List<CountrySeedItem>>(json, options);
        if (seedItems is null || seedItems.Count == 0)
        {
            this.logger.LogWarning("Countries seed JSON file did not contain any deserializable country.");
            return;
        }

        List<CountryDocument> countries = seedItems.Select(static item => new CountryDocument
        {
            Id = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("D") : item.Id,
            IsoCode = item.IsoCode?.Trim().ToUpperInvariant() ?? string.Empty,
            Names = item.Names.Select(static name => new LocalizedTextDocument
            {
                LanguageCode = name.LanguageCode?.Trim().ToLowerInvariant() ?? string.Empty,
                Value = name.Value,
            }).ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        }).ToList();

        if (countries.Count > 0)
        {
            await countriesCollection.InsertManyAsync(countries, cancellationToken: cancellationToken);
        }
    }

    private string ResolveCountriesSeedPath()
    {
        string contentRootPath = this.hostEnvironment.ContentRootPath;
        string primaryPath = Path.Combine(contentRootPath, "Resources", "InitializingDatas", "countries.seed.json");
        if (File.Exists(primaryPath))
        {
            return primaryPath;
        }

        string fallbackPath = Path.Combine(AppContext.BaseDirectory, "Resources", "InitializingDatas", "countries.seed.json");
        return fallbackPath;
    }

    private async Task InitializeUsersIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<UserDocument> usersCollection = this.database.GetCollection<UserDocument>(this.settings.UsersCollectionName);
        List<CreateIndexModel<UserDocument>> indexes = new List<CreateIndexModel<UserDocument>>
        {
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(item => item.Email),
                new CreateIndexOptions { Name = "idx_users_email_unique", Unique = true }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Descending(item => item.LastActivityUtc),
                new CreateIndexOptions { Name = "idx_users_last_activity_desc" }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys
                    .Ascending("externalLogins.provider")
                    .Ascending("externalLogins.providerUserId"),
                new CreateIndexOptions { Name = "idx_users_external_login" }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(item => item.EmailConfirmationTokenHash),
                new CreateIndexOptions<UserDocument>
                {
                    Name = "idx_users_email_confirmation_token_hash",
                    PartialFilterExpression = Builders<UserDocument>.Filter.Type(item => item.EmailConfirmationTokenHash, BsonType.String),
                }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(item => item.PasswordResetTokenHash),
                new CreateIndexOptions<UserDocument>
                {
                    Name = "idx_users_password_reset_token_hash",
                    PartialFilterExpression = Builders<UserDocument>.Filter.Type(item => item.PasswordResetTokenHash, BsonType.String),
                }),
        };

        await usersCollection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task BackfillPublicAccountIdentitiesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<UserDocument> usersCollection =
            this.database.GetCollection<UserDocument>(this.settings.UsersCollectionName);
        FilterDefinition<UserDocument> migrationFilter = BuildPublicIdentityMigrationFilter();
        ProjectionDefinition<UserDocument> migrationProjection = Builders<UserDocument>.Projection
            .Include(document => document.Id)
            .Include(document => document.PublicAccountNumber)
            .Include(document => document.PublicDisplayName)
            .Include(document => document.UsesAutomaticPublicDisplayName)
            .Include(document => document.Roles)
            .Include(document => document.CreatedAt);
        List<UserDocument> users = await usersCollection
            .Find(migrationFilter)
            .Project<UserDocument>(migrationProjection)
            .ToListAsync(cancellationToken);
        if (users.Count == 0)
        {
            long persistedMaximumNumber = await usersCollection
                .Find(Builders<UserDocument>.Filter.Gt(
                    document => document.PublicAccountNumber,
                    0))
                .SortByDescending(document => document.PublicAccountNumber)
                .Project(document => document.PublicAccountNumber)
                .FirstOrDefaultAsync(cancellationToken);
            await this.SynchronizePublicAccountCounterAsync(
                persistedMaximumNumber,
                cancellationToken);
            return;
        }

        List<long> existingNumbers = await usersCollection
            .Find(Builders<UserDocument>.Filter.Gt(document => document.PublicAccountNumber, 0))
            .Project(document => document.PublicAccountNumber)
            .ToListAsync(cancellationToken);
        HashSet<long> reservedNumbers = existingNumbers
            .ToHashSet();
        long nextNumber = 1;
        long modifiedCount = 0;

        foreach (UserDocument user in users
            .OrderBy(static user => ResolvePublicIdentityRoleOrder(user.Roles))
            .ThenBy(static user => user.CreatedAt)
            .ThenBy(static user => user.Id, StringComparer.Ordinal))
        {
            bool needsNumber = user.PublicAccountNumber <= 0;
            if (needsNumber)
            {
                while (!reservedNumbers.Add(nextNumber))
                {
                    nextNumber++;
                }

                user.PublicAccountNumber = nextNumber;
                nextNumber++;
            }

            bool needsAutomaticDisplayName = string.IsNullOrWhiteSpace(user.PublicDisplayName)
                || user.UsesAutomaticPublicDisplayName;
            string? publicDisplayName = needsAutomaticDisplayName
                ? PublicDisplayNameFactory.Create(user.Roles, user.PublicAccountNumber)
                : user.PublicDisplayName?.Trim();
            bool needsUpdate = needsNumber
                || !string.Equals(user.PublicDisplayName, publicDisplayName, StringComparison.Ordinal)
                || (needsAutomaticDisplayName && !user.UsesAutomaticPublicDisplayName);
            if (!needsUpdate)
            {
                continue;
            }

            UpdateResult result = await usersCollection.UpdateOneAsync(
                Builders<UserDocument>.Filter.Eq(document => document.Id, user.Id),
                Builders<UserDocument>.Update
                    .Set(document => document.PublicAccountNumber, user.PublicAccountNumber)
                    .Set(document => document.PublicDisplayName, publicDisplayName)
                    .Set(document => document.UsesAutomaticPublicDisplayName, needsAutomaticDisplayName)
                    .Set(document => document.UpdatedAt, DateTime.UtcNow),
                cancellationToken: cancellationToken);
            modifiedCount += result.ModifiedCount;
        }

        long maximumNumber = reservedNumbers.Count == 0 ? 0 : reservedNumbers.Max();
        await this.SynchronizePublicAccountCounterAsync(maximumNumber, cancellationToken);

        this.logger.LogInformation(
            "Backfilled a stable public account identity for {Count} existing user accounts.",
            modifiedCount);
    }

    private async Task SynchronizePublicAccountCounterAsync(
        long maximumNumber,
        CancellationToken cancellationToken)
    {
        IMongoCollection<PublicAccountCounterDocument> countersCollection =
            this.database.GetCollection<PublicAccountCounterDocument>(this.settings.CountersCollectionName);
        await countersCollection.UpdateOneAsync(
            Builders<PublicAccountCounterDocument>.Filter.Eq(
                document => document.Id,
                "public-account-number"),
            Builders<PublicAccountCounterDocument>.Update.Max(
                document => document.Sequence,
                maximumNumber),
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    internal static FilterDefinition<UserDocument> BuildPublicIdentityMigrationFilter()
    {
        FilterDefinitionBuilder<UserDocument> builder = Builders<UserDocument>.Filter;
        return builder.Or(
            builder.Exists(document => document.PublicAccountNumber, false),
            builder.Lte(document => document.PublicAccountNumber, 0),
            builder.Exists(document => document.PublicDisplayName, false),
            builder.Eq(document => document.PublicDisplayName, null),
            builder.Eq(document => document.PublicDisplayName, string.Empty),
            builder.Exists(document => document.UsesAutomaticPublicDisplayName, false));
    }

    private async Task InitializeUserPublicDisplayNameIndexAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<UserDocument> usersCollection =
            this.database.GetCollection<UserDocument>(this.settings.UsersCollectionName);
        CreateIndexModel<UserDocument>[] indexes =
        {
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(document => document.PublicDisplayName),
                new CreateIndexOptions<UserDocument>
                {
                    Name = "idx_users_public_display_name_unique",
                    Unique = true,
                    Collation = new Collation("en", strength: CollationStrength.Secondary),
                    PartialFilterExpression = Builders<UserDocument>.Filter.Type(
                        document => document.PublicDisplayName,
                        BsonType.String),
                }),
            new CreateIndexModel<UserDocument>(
                Builders<UserDocument>.IndexKeys.Ascending(document => document.PublicAccountNumber),
                new CreateIndexOptions
                {
                    Name = "idx_users_public_account_number_unique",
                    Unique = true,
                }),
        };

        await usersCollection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private async Task BackfillLegacyCommentAuthorSnapshotsAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CommentDocument> commentsCollection =
            this.database.GetCollection<CommentDocument>(this.settings.CommentsCollectionName);
        List<CommentDocument> legacyComments = await commentsCollection
            .Find(BuildLegacyCommentAuthorSnapshotFilter())
            .Project<CommentDocument>(
                Builders<CommentDocument>.Projection
                    .Include(document => document.Id)
                    .Include(document => document.AuthorUserId)
                    .Include(document => document.UpdatedAt))
            .ToListAsync(cancellationToken);
        if (legacyComments.Count == 0)
        {
            return;
        }

        string[] authorUserIds = legacyComments
            .Select(static comment => comment.AuthorUserId)
            .Where(static authorUserId => !string.IsNullOrWhiteSpace(authorUserId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IMongoCollection<UserDocument> usersCollection =
            this.database.GetCollection<UserDocument>(this.settings.UsersCollectionName);
        List<UserDocument> authors = await usersCollection
            .Find(Builders<UserDocument>.Filter.In(document => document.Id, authorUserIds))
            .Project<UserDocument>(
                Builders<UserDocument>.Projection
                    .Include(document => document.Id)
                    .Include(document => document.PublicDisplayName)
                    .Include(document => document.PublicAccountNumber)
                    .Include(document => document.Roles)
                    .Include(document => document.AvatarUrl))
            .ToListAsync(cancellationToken);
        Dictionary<string, UserDocument> authorsById = authors.ToDictionary(
            static author => author.Id,
            StringComparer.Ordinal);
        List<WriteModel<CommentDocument>> writes = new List<WriteModel<CommentDocument>>(legacyComments.Count);

        foreach (CommentDocument comment in legacyComments)
        {
            authorsById.TryGetValue(comment.AuthorUserId, out UserDocument? author);
            writes.Add(new UpdateOneModel<CommentDocument>(
                Builders<CommentDocument>.Filter.Eq(document => document.Id, comment.Id),
                BuildLegacyCommentAuthorSnapshotUpdate(comment, author)));
        }

        BulkWriteResult<CommentDocument> result = await commentsCollection.BulkWriteAsync(
            writes,
            new BulkWriteOptions { IsOrdered = false },
            cancellationToken);
        this.logger.LogInformation(
            "Migrated {Count} legacy comment author snapshots to public pseudonyms.",
            result.ModifiedCount);
    }

    internal static FilterDefinition<CommentDocument> BuildLegacyCommentAuthorSnapshotFilter()
    {
        FilterDefinitionBuilder<CommentDocument> builder = Builders<CommentDocument>.Filter;
        FilterDefinition<CommentDocument> missingPublicDisplayName = builder.Or(
            builder.Exists(document => document.AuthorDisplayName, false),
            builder.Eq(document => document.AuthorDisplayName, null),
            builder.Eq(document => document.AuthorDisplayName, string.Empty));
        return builder.Or(
            builder.Exists("authorDisplayName", true),
            missingPublicDisplayName);
    }

    internal static UpdateDefinition<CommentDocument> BuildLegacyCommentAuthorSnapshotUpdate(
        CommentDocument comment,
        UserDocument? author)
    {
        string publicDisplayName = ResolveLegacyCommentAuthorPublicDisplayName(author);
        return Builders<CommentDocument>.Update
            .Set(document => document.AuthorDisplayName, publicDisplayName)
            .Set(document => document.AuthorAvatarUrl, author?.AvatarUrl)
            .Set(document => document.UpdatedAt, comment.UpdatedAt)
            .Unset("authorDisplayName");
    }

    internal static string ResolveLegacyCommentAuthorPublicDisplayName(UserDocument? author)
    {
        string publicDisplayName = author?.PublicDisplayName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(publicDisplayName)
            && author is not null
            && author.PublicAccountNumber > 0)
        {
            publicDisplayName = PublicDisplayNameFactory.Create(
                author.Roles,
                author.PublicAccountNumber);
        }

        return string.IsNullOrWhiteSpace(publicDisplayName)
            ? "User"
            : publicDisplayName;
    }

    private static int ResolvePublicIdentityRoleOrder(IReadOnlyCollection<Role> roles)
    {
        if (roles.Contains(Role.Admin))
        {
            return 0;
        }

        return roles.Contains(Role.Moderator) ? 1 : 2;
    }

    private async Task InitializeCountriesIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CountryDocument> countriesCollection = this.database.GetCollection<CountryDocument>(this.settings.CountriesCollectionName);
        List<CreateIndexModel<CountryDocument>> indexes = new List<CreateIndexModel<CountryDocument>>
        {
            new CreateIndexModel<CountryDocument>(
                Builders<CountryDocument>.IndexKeys.Ascending(item => item.IsoCode),
                new CreateIndexOptions { Name = "idx_countries_iso_code_unique", Unique = true }),
        };

        await countriesCollection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }





private async Task InitializeVideosIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<VideoDocument> collection = this.database.GetCollection<VideoDocument>(this.settings.VideosCollectionName);
        List<CreateIndexModel<VideoDocument>> indexes = new List<CreateIndexModel<VideoDocument>>
        {
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.OwnerType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_videos_owner_published_created_desc" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.OwnerType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.Type)
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_videos_owner_type_published_created_desc" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.HostingProvider)
                    .Ascending(item => item.ExternalId),
                new CreateIndexOptions { Name = "idx_videos_provider_external_id" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys.Ascending("tagIds"),
                new CreateIndexOptions { Name = "idx_videos_tag_ids" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys.Ascending("languageCodes"),
                new CreateIndexOptions { Name = "idx_videos_language_codes" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.CreatorName)
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_videos_creator_published_created_desc" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_videos_published_created_desc" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Text(item => item.Title)
                    .Text(item => item.Description)
                    .Text(item => item.CreatorName)
                    .Text(item => item.CanonicalUrl)
                    .Text(item => item.ExternalId),
                new CreateIndexOptions { Name = "idx_videos_admin_text" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeVideoTagsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<VideoTagDocument> collection = this.database.GetCollection<VideoTagDocument>(this.settings.VideoTagsCollectionName);
        List<CreateIndexModel<VideoTagDocument>> indexes = new List<CreateIndexModel<VideoTagDocument>>
        {
            new CreateIndexModel<VideoTagDocument>(
                Builders<VideoTagDocument>.IndexKeys.Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_video_tags_slug_unique", Unique = true }),
            new CreateIndexModel<VideoTagDocument>(
                Builders<VideoTagDocument>.IndexKeys.Ascending(item => item.IsActive),
                new CreateIndexOptions { Name = "idx_video_tags_is_active" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task SeedSystemVideoTagsAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<VideoTagDocument> collection = this.database.GetCollection<VideoTagDocument>(this.settings.VideoTagsCollectionName);
        DateTime now = DateTime.UtcNow;
        IReadOnlyCollection<VideoTagDocument> tags = new List<VideoTagDocument>
        {
            CreateSystemVideoTag(
                "video-tag-official-amusementparks",
                "official-amusementparks",
                new Dictionary<string, string>
                {
                    ["fr"] = "Officiel AmusementParks.fun",
                    ["en"] = "Official AmusementParks.fun",
                    ["de"] = "Offiziell AmusementParks.fun",
                    ["nl"] = "Officieel AmusementParks.fun",
                    ["it"] = "Ufficiale AmusementParks.fun",
                    ["es"] = "Oficial AmusementParks.fun",
                    ["pl"] = "Oficjalne AmusementParks.fun",
                    ["pt"] = "Oficial AmusementParks.fun",
                },
                now),
            CreateSystemVideoTag(
                "video-tag-associated-creator",
                "associated-creator",
                new Dictionary<string, string>
                {
                    ["fr"] = "Createur contenu associe",
                    ["en"] = "Associated content creator",
                    ["de"] = "Verbundener Content Creator",
                    ["nl"] = "Geassocieerde contentmaker",
                    ["it"] = "Creatore di contenuti associato",
                    ["es"] = "Creador de contenido asociado",
                    ["pl"] = "Powiazany tworca tresci",
                    ["pt"] = "Criador de conteudo associado",
                },
                now),
            CreateSystemVideoTag(
                "video-tag-third-party-creator",
                "third-party-creator",
                new Dictionary<string, string>
                {
                    ["fr"] = "Createur contenu tiers",
                    ["en"] = "Third-party content creator",
                    ["de"] = "Externer Content Creator",
                    ["nl"] = "Externe contentmaker",
                    ["it"] = "Creatore di contenuti terzo",
                    ["es"] = "Creador de contenido externo",
                    ["pl"] = "Zewnetrzny tworca tresci",
                    ["pt"] = "Criador de conteudo terceiro",
                },
                now),
            CreateSystemVideoTag(
                "video-tag-official-park",
                "official-park",
                new Dictionary<string, string>
                {
                    ["fr"] = "Officiel parc ou exploitant",
                    ["en"] = "Official park or operator",
                    ["de"] = "Offizieller Park oder Betreiber",
                    ["nl"] = "Officieel park of exploitant",
                    ["it"] = "Parco o gestore ufficiale",
                    ["es"] = "Parque u operador oficial",
                    ["pl"] = "Oficjalny park lub operator",
                    ["pt"] = "Parque ou operador oficial",
                },
                now),
        };

        foreach (VideoTagDocument tag in tags)
        {
            FilterDefinition<VideoTagDocument> filter = Builders<VideoTagDocument>.Filter.Eq(static document => document.Slug, tag.Slug);
            UpdateDefinition<VideoTagDocument> update = Builders<VideoTagDocument>.Update
                .SetOnInsert(static document => document.Id, tag.Id)
                .Set(static document => document.Slug, tag.Slug)
                .Set(static document => document.Labels, tag.Labels)
                .Set(static document => document.Descriptions, tag.Descriptions)
                .Set(static document => document.IsActive, true)
                .SetOnInsert(static document => document.CreatedAt, tag.CreatedAt)
                .Set(static document => document.UpdatedAt, now);

            await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
        }
    }

    private static VideoTagDocument CreateSystemVideoTag(string id, string slug, IReadOnlyDictionary<string, string> labels, DateTime now)
    {
        return new VideoTagDocument
        {
            Id = id,
            Slug = slug,
            Labels = labels.Select(static label => new LocalizedTextDocument
            {
                LanguageCode = label.Key,
                Value = label.Value,
            }).ToList(),
            Descriptions = new List<LocalizedTextDocument>(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static async Task MigrateTripPlanProgramFoundationAsync(
        IMongoCollection<TripPlanDocument> collection,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        UpdateDefinitionBuilder<TripPlanDocument> updates = Builders<TripPlanDocument>.Update;
        await collection.UpdateManyAsync(
            filters.Exists(static document => document.ChildMutationEpoch, false),
            updates.Set(static document => document.ChildMutationEpoch, 1),
            cancellationToken: cancellationToken);
        await collection.UpdateManyAsync(
            filters.Exists(static document => document.ChildMutationLeaseSequence, false),
            updates.Set(static document => document.ChildMutationLeaseSequence, 0),
            cancellationToken: cancellationToken);
        await collection.UpdateManyAsync(
            filters.Exists(static document => document.ActiveChildMutationLeases, false),
            updates.Set(
                static document => document.ActiveChildMutationLeases,
                new List<TripChildMutationLeaseDocument>()),
            cancellationToken: cancellationToken);
        await collection.UpdateManyAsync(
            filters.Exists(static document => document.ParkCandidateOrderIds, false),
            updates.Set(
                static document => document.ParkCandidateOrderIds,
                new List<string>()),
            cancellationToken: cancellationToken);
        await collection.UpdateManyAsync(
            filters.Exists(static document => document.ParkCandidateOrderVersion, false),
            updates.Set(static document => document.ParkCandidateOrderVersion, 0),
            cancellationToken: cancellationToken);
        await collection.UpdateManyAsync(
            filters.Exists("creationSnapshot", true)
                & filters.Exists("creationSnapshot.childMutationEpoch", false),
            updates.Set("creationSnapshot.childMutationEpoch", 1),
            cancellationToken: cancellationToken);
        await collection.UpdateManyAsync(
            TripPlanMongoDefinitions.BuildMissingCreationSnapshotOwnerFilter(),
            TripPlanMongoDefinitions.BuildCreationSnapshotOwnerBackfill(),
            cancellationToken: cancellationToken);
    }

    private async Task InitializeUserVisitIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<UserVisitDocument> collection =
            this.database.GetCollection<UserVisitDocument>(this.settings.UserVisitsCollectionName);
        await collection.UpdateManyAsync(
            UserVisitMongoDefinitions.BuildMissingDateSortKeyFilter(),
            UserVisitMongoDefinitions.BuildDateSortKeyBackfillUpdate(),
            cancellationToken: cancellationToken);
        await collection.Indexes.CreateManyAsync(
            UserVisitMongoDefinitions.BuildIndexes(),
            cancellationToken);
    }

    private async Task InitializeUserRideOccurrenceIndexesAsync(
        CancellationToken cancellationToken)
    {
        IMongoCollection<UserRideOccurrenceDocument> collection =
            this.database.GetCollection<UserRideOccurrenceDocument>(
                this.settings.UserRideOccurrencesCollectionName);
        await collection.Indexes.CreateManyAsync(
            UserRideOccurrenceMongoDefinitions.BuildIndexes(),
            cancellationToken);
    }

    private async Task InitializeUserRideOccurrenceOperationIndexesAsync(
        CancellationToken cancellationToken)
    {
        IMongoCollection<UserRideOccurrenceCreationOperationDocument> collection =
            this.database.GetCollection<UserRideOccurrenceCreationOperationDocument>(
                this.settings.UserRideOccurrenceOperationsCollectionName);
        await collection.Indexes.CreateManyAsync(
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildIndexes(),
            cancellationToken);
    }

    private async Task InitializePassportAuditIndexesAsync(
        CancellationToken cancellationToken)
    {
        IMongoCollection<PassportAuditJournalDocument> collection =
            this.database.GetCollection<PassportAuditJournalDocument>(
                this.settings.PassportAuditEventsCollectionName);
        await collection.Indexes.CreateManyAsync(
            PassportAuditMongoDefinitions.BuildJournalIndexes(),
            cancellationToken);
    }

    private async Task InitializePassportExportIndexesAsync(
        CancellationToken cancellationToken)
    {
        IMongoCollection<PassportExportDocument> exports =
            this.database.GetCollection<PassportExportDocument>(
                this.settings.PassportExportsCollectionName);
        await exports.Indexes.CreateManyAsync(
            PassportExportMongoDefinitions.BuildExportIndexes(),
            cancellationToken);

        IMongoCollection<PassportExportChunkDocument> chunks =
            this.database.GetCollection<PassportExportChunkDocument>(
                this.settings.PassportExportChunksCollectionName);
        await chunks.Indexes.CreateManyAsync(
            PassportExportMongoDefinitions.BuildChunkIndexes(),
            cancellationToken);
    }

    private async Task InitializeGlobalRatingSuggestionIndexesAsync(
        CancellationToken cancellationToken)
    {
        IMongoCollection<GlobalRatingSuggestionStateDocument> states =
            this.database.GetCollection<GlobalRatingSuggestionStateDocument>(
                this.settings.GlobalRatingSuggestionStatesCollectionName);
        await states.Indexes.CreateManyAsync(
            GlobalRatingSuggestionMongoDefinitions.BuildStateIndexes(),
            cancellationToken);

        IMongoCollection<GlobalRatingSuggestionPreferenceDocument> preferences =
            this.database.GetCollection<GlobalRatingSuggestionPreferenceDocument>(
                this.settings.GlobalRatingSuggestionPreferencesCollectionName);
        await preferences.Indexes.CreateManyAsync(
            GlobalRatingSuggestionMongoDefinitions.BuildPreferenceIndexes(),
            cancellationToken);

        IMongoCollection<GlobalRatingSuggestionInteractionDocument> interactions =
            this.database.GetCollection<GlobalRatingSuggestionInteractionDocument>(
                this.settings.GlobalRatingSuggestionInteractionsCollectionName);
        await interactions.Indexes.CreateManyAsync(
            GlobalRatingSuggestionMongoDefinitions.BuildInteractionIndexes(),
            cancellationToken);
    }
}
