using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class SharePublicationRepository : ISharePublicationRepository
{
    private readonly IMongoCollection<SharePublicationDocument> collection;

    public SharePublicationRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal SharePublicationRepository(IMongoCollection<SharePublicationDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<SharePublication?> GetOwnedAsync(
        SharePublicationId publicationId,
        string ownerUserId,
        CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = IdentifierRules.NormalizeRequired(
            ownerUserId,
            nameof(ownerUserId));
        SharePublicationDocument? document = await this.collection
            .Find(SharePublicationMongoDefinitions.BuildOwnedFilter(
                publicationId.Value,
                normalizedOwnerUserId))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<SharePublication?> GetOwnedBySourceAsync(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        CancellationToken cancellationToken)
    {
        string normalizedOwnerUserId = IdentifierRules.NormalizeRequired(
            ownerUserId,
            nameof(ownerUserId));
        string normalizedSourceScopeKey = IdentifierRules.NormalizeRequired(
            sourceScopeKey,
            nameof(sourceScopeKey));
        SharePublicationDocument? document = await this.collection
            .Find(SharePublicationMongoDefinitions.BuildOwnedSourceFilter(
                normalizedOwnerUserId,
                publicationType,
                normalizedSourceScopeKey))
            .SortByDescending(static item => item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<SharePublication?> GetResolvableByTokenAsync(
        ShareToken shareToken,
        CancellationToken cancellationToken)
    {
        SharePublicationDocument? document = await this.collection
            .Find(SharePublicationMongoDefinitions.BuildResolvableTokenFilter(
                shareToken.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<SharePublicationWriteOutcome> CreateAsync(
        SharePublication publication,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publication);
        try
        {
            await this.collection.InsertOneAsync(
                publication.ToDocument(),
                cancellationToken: cancellationToken);
            return SharePublicationWriteOutcome.Success;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ClassifyDuplicateKey(
                exception.WriteError.Message,
                exception.WriteError.Details);
        }
    }

    public async Task<SharePublicationWriteOutcome> ReplaceAsync(
        SharePublication publication,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(publication);
        if (expectedVersion < 0
            || expectedVersion == long.MaxValue
            || publication.Version != expectedVersion + 1)
        {
            throw new ArgumentException(
                "The persisted share publication must be exactly one version ahead of the expected version.",
                nameof(publication));
        }

        try
        {
            ReplaceOneResult result = await this.collection.ReplaceOneAsync(
                SharePublicationMongoDefinitions.BuildOwnedVersionFilter(
                    publication.Id.Value,
                    publication.OwnerUserId,
                    expectedVersion),
                publication.ToDocument(),
                new ReplaceOptions { IsUpsert = false },
                cancellationToken);
            return result.MatchedCount == 1
                ? SharePublicationWriteOutcome.Success
                : SharePublicationWriteOutcome.Conflict;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return ClassifyDuplicateKey(
                exception.WriteError.Message,
                exception.WriteError.Details);
        }
    }

    internal static SharePublicationWriteOutcome ClassifyDuplicateKey(
        string? message,
        BsonDocument? details = null)
    {
        bool namedTokenIndex = message?.Contains(
            SharePublicationMongoDefinitions.ShareTokenUniqueIndexName,
            StringComparison.Ordinal) == true;
        bool tokenKeyPattern = details is not null
            && details.TryGetValue("keyPattern", out BsonValue? keyPattern)
            && keyPattern.IsBsonDocument
            && keyPattern.AsBsonDocument.Contains("shareToken");
        return namedTokenIndex || tokenKeyPattern
            ? SharePublicationWriteOutcome.TokenCollision
            : SharePublicationWriteOutcome.Conflict;
    }

    private static IMongoCollection<SharePublicationDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<SharePublicationDocument>(
            settings.SharePublicationsCollectionName);
    }
}
