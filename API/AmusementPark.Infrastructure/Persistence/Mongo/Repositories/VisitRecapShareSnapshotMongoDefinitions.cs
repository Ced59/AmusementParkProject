using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class VisitRecapShareSnapshotMongoDefinitions
{
    public const string PublicationVersionIndexName =
        "idx_share_snapshot_publication_version";

    public static IReadOnlyCollection<CreateIndexModel<VisitRecapShareSnapshotDocument>>
        BuildIndexes()
    {
        CreateIndexModel<VisitRecapShareSnapshotDocument> publicationVersion =
            new CreateIndexModel<VisitRecapShareSnapshotDocument>(
                Builders<VisitRecapShareSnapshotDocument>.IndexKeys
                    .Ascending(static document => document.PublicationId)
                    .Ascending(static document => document.PublicationVersion),
                new CreateIndexOptions { Name = PublicationVersionIndexName });
        return new[] { publicationVersion };
    }
}
