using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class PassportProfileShareSnapshotRepository
    : IPassportProfileShareSnapshotRepository
{
    private readonly IMongoCollection<PassportProfileShareSnapshotDocument> collection;

    public PassportProfileShareSnapshotRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(database.GetCollection<PassportProfileShareSnapshotDocument>(
            settings.SharePublicationSnapshotsCollectionName))
    {
    }

    internal PassportProfileShareSnapshotRepository(
        IMongoCollection<PassportProfileShareSnapshotDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<bool> UpsertAsync(
        PassportProfileShareSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        PassportProfileShareSnapshotDocument document = snapshot.ToDocument();
        try
        {
            FilterDefinitionBuilder<PassportProfileShareSnapshotDocument> filters =
                Builders<PassportProfileShareSnapshotDocument>.Filter;
            FilterDefinition<PassportProfileShareSnapshotDocument> replaceable = filters.Eq(
                    static value => value.Id,
                    document.Id)
                & filters.Lte(
                    static value => value.PublicationStateVersion,
                    snapshot.PublicationStateVersion);
            ReplaceOneResult result = await this.collection.ReplaceOneAsync(
                replaceable,
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
            return result.IsAcknowledged
                && (result.MatchedCount == 1 || result.UpsertedId is not null);
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            PassportProfileShareSnapshotDocument? existing = await this.collection
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

    public async Task<PassportProfileShareSnapshot?> GetAsync(
        SharePublicationId publicationId,
        long publicationVersion,
        CancellationToken cancellationToken)
    {
        PassportProfileShareSnapshotDocument? document = await this.collection
            .Find(BuildFilter(publicationId, publicationVersion))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<bool> DeleteSupersededAsync(
        SharePublicationId publicationId,
        long publishedVersion,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<PassportProfileShareSnapshotDocument> filters =
            Builders<PassportProfileShareSnapshotDocument>.Filter;
        DeleteResult result = await this.collection.DeleteManyAsync(
            filters.Eq(static value => value.PublicationId, publicationId.Value)
                & filters.Lt(static value => value.PublicationVersion, publishedVersion),
            cancellationToken);
        return result.IsAcknowledged;
    }

    private static FilterDefinition<PassportProfileShareSnapshotDocument> BuildFilter(
        SharePublicationId publicationId,
        long publicationVersion)
    {
        string id = PassportProfileShareSnapshotMongoMapper.CreateDocumentId(
            publicationId.Value,
            publicationVersion);
        return Builders<PassportProfileShareSnapshotDocument>.Filter.Eq(
            static value => value.Id,
            id);
    }
}
