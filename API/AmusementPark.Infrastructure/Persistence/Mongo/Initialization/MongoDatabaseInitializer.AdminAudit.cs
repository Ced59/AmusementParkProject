using AmusementPark.Infrastructure.Persistence.Mongo.Documents.AdminAudit;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
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
}
