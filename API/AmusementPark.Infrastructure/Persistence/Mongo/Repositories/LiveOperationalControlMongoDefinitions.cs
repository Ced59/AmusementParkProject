using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public static class LiveOperationalControlMongoDefinitions
{
    public static IEnumerable<CreateIndexModel<LiveOperationalControlDocument>> BuildIndexes()
    {
        IndexKeysDefinitionBuilder<LiveOperationalControlDocument> keys =
            Builders<LiveOperationalControlDocument>.IndexKeys;
        yield return new CreateIndexModel<LiveOperationalControlDocument>(
            keys.Ascending(static document => document.ControlId)
                .Ascending(static document => document.Revision),
            new CreateIndexOptions
            {
                Name = "ux_live_operational_control_revision",
                Unique = true,
            });
        yield return new CreateIndexModel<LiveOperationalControlDocument>(
            keys.Ascending(static document => document.SourceId)
                .Ascending(static document => document.ScopeType)
                .Ascending(static document => document.ExternalEntityId)
                .Ascending(static document => document.InternalParkId)
                .Ascending(static document => document.TargetType)
                .Ascending(static document => document.InternalTargetId)
                .Ascending(static document => document.Revision),
            new CreateIndexOptions
            {
                Name = "ux_live_operational_scope_revision",
                Unique = true,
            });
    }
}
