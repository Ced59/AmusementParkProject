using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public static class LiveTargetMappingMongoDefinitions
{
    public static IReadOnlyCollection<CreateIndexModel<ExternalLiveTargetMappingDocument>>
        BuildIndexes()
    {
        return new CreateIndexModel<ExternalLiveTargetMappingDocument>[]
        {
            new(
                Builders<ExternalLiveTargetMappingDocument>.IndexKeys
                    .Ascending(static document => document.MappingId)
                    .Ascending(static document => document.Revision),
                new CreateIndexOptions
                {
                    Name = "idx_live_target_mapping_revision_unique",
                    Unique = true,
                }),
            new(
                Builders<ExternalLiveTargetMappingDocument>.IndexKeys
                    .Ascending(static document => document.MappingId)
                    .Descending(static document => document.Revision),
                new CreateIndexOptions { Name = "idx_live_target_mapping_latest_revision" }),
            new(
                Builders<ExternalLiveTargetMappingDocument>.IndexKeys
                    .Ascending(static document => document.SourceId)
                    .Ascending("externalTarget.id")
                    .Ascending(static document => document.Revision),
                new CreateIndexOptions
                {
                    Name = "idx_live_target_source_external_revision_unique",
                    Unique = true,
                }),
            new(
                Builders<ExternalLiveTargetMappingDocument>.IndexKeys
                    .Ascending(static document => document.SourceId)
                    .Ascending(static document => document.Status)
                    .Descending(static document => document.Revision),
                new CreateIndexOptions { Name = "idx_live_target_source_status_revision" }),
            new(
                Builders<ExternalLiveTargetMappingDocument>.IndexKeys
                    .Ascending("externalTarget.type")
                    .Ascending(static document => document.Status)
                    .Descending(static document => document.RecordedAtUtc),
                new CreateIndexOptions { Name = "idx_live_target_type_status_recorded" }),
        };
    }
}
