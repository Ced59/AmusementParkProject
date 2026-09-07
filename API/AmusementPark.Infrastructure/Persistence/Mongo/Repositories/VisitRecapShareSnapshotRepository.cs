using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class VisitRecapShareSnapshotRepository : IVisitRecapShareSnapshotRepository
{
    private readonly IMongoCollection<VisitRecapShareSnapshotDocument> collection;

    public VisitRecapShareSnapshotRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(database.GetCollection<VisitRecapShareSnapshotDocument>(
            settings.SharePublicationSnapshotsCollectionName))
    {
    }

    internal VisitRecapShareSnapshotRepository(
        IMongoCollection<VisitRecapShareSnapshotDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<bool> UpsertAsync(
        VisitRecapShareSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        VisitRecapShareSnapshotDocument document = snapshot.ToDocument();
        try
        {
            FilterDefinitionBuilder<VisitRecapShareSnapshotDocument> filters =
                Builders<VisitRecapShareSnapshotDocument>.Filter;
            FilterDefinition<VisitRecapShareSnapshotDocument> replaceableSnapshot =
                filters.Eq(static value => value.Id, document.Id)
                & filters.Lte(
                    static value => value.PublicationStateVersion,
                    snapshot.PublicationStateVersion);
            ReplaceOneResult result = await this.collection.ReplaceOneAsync(
                replaceableSnapshot,
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
            return result.IsAcknowledged
                && (result.MatchedCount == 1 || result.UpsertedId is not null);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            VisitRecapShareSnapshotDocument? existing = await this.collection
                .Find(BuildFilter(snapshot.PublicationId, snapshot.PublicationVersion))
                .FirstOrDefaultAsync(cancellationToken);
            return existing is not null
                && existing.SourceVersion == snapshot.SourceVersion
                && existing.PublicationStateVersion == snapshot.PublicationStateVersion
                && string.Equals(
                    existing.ContentFingerprint,
                    snapshot.ContentFingerprint,
                    StringComparison.Ordinal);
        }
    }

    public async Task<VisitRecapShareSnapshot?> GetAsync(
        SharePublicationId publicationId,
        long publicationVersion,
        CancellationToken cancellationToken)
    {
        VisitRecapShareSnapshotDocument? document = await this.collection
            .Find(BuildFilter(publicationId, publicationVersion))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<bool> DeleteSupersededAsync(
        SharePublicationId publicationId,
        long publishedVersion,
        CancellationToken cancellationToken)
    {
        DeleteResult deletion = await this.collection.DeleteManyAsync(
            BuildSupersededFilter(publicationId, publishedVersion),
            cancellationToken);
        return deletion.IsAcknowledged;
    }

    internal static FilterDefinition<VisitRecapShareSnapshotDocument> BuildSupersededFilter(
        SharePublicationId publicationId,
        long publishedVersion)
    {
        if (publishedVersion < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(publishedVersion));
        }

        FilterDefinitionBuilder<VisitRecapShareSnapshotDocument> filters =
            Builders<VisitRecapShareSnapshotDocument>.Filter;
        return filters.Eq(static value => value.PublicationId, publicationId.Value)
            & filters.Lt(static value => value.PublicationVersion, publishedVersion);
    }

    private static FilterDefinition<VisitRecapShareSnapshotDocument> BuildFilter(
        SharePublicationId publicationId,
        long publicationVersion)
    {
        string id = VisitRecapShareSnapshotMongoMapper.CreateDocumentId(
            publicationId.Value,
            publicationVersion);
        return Builders<VisitRecapShareSnapshotDocument>.Filter.Eq(
            static value => value.Id,
            id);
    }
}
