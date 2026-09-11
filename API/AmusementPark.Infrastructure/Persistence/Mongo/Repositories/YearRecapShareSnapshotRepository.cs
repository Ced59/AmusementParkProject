using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class YearRecapShareSnapshotRepository : IYearRecapShareSnapshotRepository
{
    private readonly IMongoCollection<YearRecapShareSnapshotDocument> collection;

    public YearRecapShareSnapshotRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(database.GetCollection<YearRecapShareSnapshotDocument>(
            settings.SharePublicationSnapshotsCollectionName))
    {
    }

    internal YearRecapShareSnapshotRepository(
        IMongoCollection<YearRecapShareSnapshotDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<bool> UpsertAsync(
        YearRecapShareSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        YearRecapShareSnapshotDocument document = snapshot.ToDocument();
        try
        {
            FilterDefinitionBuilder<YearRecapShareSnapshotDocument> filters =
                Builders<YearRecapShareSnapshotDocument>.Filter;
            FilterDefinition<YearRecapShareSnapshotDocument> replaceable = filters.Eq(
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
            YearRecapShareSnapshotDocument? existing = await this.collection
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

    public async Task<YearRecapShareSnapshot?> GetAsync(
        SharePublicationId publicationId,
        long publicationVersion,
        CancellationToken cancellationToken)
    {
        YearRecapShareSnapshotDocument? document = await this.collection
            .Find(BuildFilter(publicationId, publicationVersion))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<bool> DeleteSupersededAsync(
        SharePublicationId publicationId,
        long publishedVersion,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<YearRecapShareSnapshotDocument> filters =
            Builders<YearRecapShareSnapshotDocument>.Filter;
        DeleteResult result = await this.collection.DeleteManyAsync(
            filters.Eq(static value => value.PublicationId, publicationId.Value)
                & filters.Lt(static value => value.PublicationVersion, publishedVersion),
            cancellationToken);
        return result.IsAcknowledged;
    }

    private static FilterDefinition<YearRecapShareSnapshotDocument> BuildFilter(
        SharePublicationId publicationId,
        long publicationVersion)
    {
        string id = YearRecapShareSnapshotMongoMapper.CreateDocumentId(
            publicationId.Value,
            publicationVersion);
        return Builders<YearRecapShareSnapshotDocument>.Filter.Eq(
            static value => value.Id,
            id);
    }
}
