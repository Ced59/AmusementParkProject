using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class PassportAuditMongoDefinitions
{
    public const string PendingEventIdPath = "pendingAuditEvents.eventId";

    public static IReadOnlyCollection<CreateIndexModel<PassportAuditJournalDocument>>
        BuildJournalIndexes()
    {
        return new[]
        {
            new CreateIndexModel<PassportAuditJournalDocument>(
                Builders<PassportAuditJournalDocument>.IndexKeys
                    .Ascending(static document => document.Event.UserId)
                    .Descending(static document => document.Event.OccurredAtUtc)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions { Name = "idx_passport_audit_user_occurred" }),
            new CreateIndexModel<PassportAuditJournalDocument>(
                Builders<PassportAuditJournalDocument>.IndexKeys
                    .Ascending(static document => document.Event.EntityType)
                    .Ascending(static document => document.Event.EntityId)
                    .Ascending(static document => document.Event.EntityVersion),
                new CreateIndexOptions { Name = "idx_passport_audit_entity_revision" }),
        };
    }

    public static CreateIndexModel<TDocument> BuildPendingMarkerIndex<TDocument>(
        string name)
    {
        return new CreateIndexModel<TDocument>(
            Builders<TDocument>.IndexKeys.Ascending(PendingEventIdPath),
            new CreateIndexOptions<TDocument>
            {
                Name = name,
                PartialFilterExpression = Builders<TDocument>.Filter.Exists(
                    PendingEventIdPath,
                    true),
            });
    }
}
