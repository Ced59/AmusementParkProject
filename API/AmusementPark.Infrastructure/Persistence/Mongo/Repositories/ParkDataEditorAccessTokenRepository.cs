using AmusementPark.Application.Features.ParkDataEditorTokens.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ParkDataEditorAccessTokenRepository : IParkDataEditorAccessTokenRepository
{
    private const int MaximumListedTokens = 100;
    private readonly IMongoCollection<ParkDataEditorAccessTokenDocument> collection;

    public ParkDataEditorAccessTokenRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ParkDataEditorAccessTokenDocument>(
            settings.ParkDataEditorAccessTokensCollectionName);
    }

    public async Task<ParkDataEditorAccessToken?> GetByIdAsync(
        string tokenId,
        CancellationToken cancellationToken)
    {
        ParkDataEditorAccessTokenDocument? document = await this.collection
            .Find(item => item.Id == tokenId)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<ParkDataEditorAccessToken>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        List<ParkDataEditorAccessTokenDocument> documents = await this.collection
            .Find(item => item.UserId == userId)
            .SortByDescending(item => item.CreatedAt)
            .Limit(MaximumListedTokens)
            .ToListAsync(cancellationToken);
        return documents.Select(static item => item.ToDomain()).ToList();
    }

    public Task<long> CountActiveByUserIdAsync(
        string userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDataEditorAccessTokenDocument> filter = Builders<ParkDataEditorAccessTokenDocument>.Filter.And(
            Builders<ParkDataEditorAccessTokenDocument>.Filter.Eq(item => item.UserId, userId),
            Builders<ParkDataEditorAccessTokenDocument>.Filter.Eq(item => item.RevokedAtUtc, null),
            Builders<ParkDataEditorAccessTokenDocument>.Filter.Gt(item => item.ExpiresAtUtc, utcNow));
        return this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task CreateAsync(ParkDataEditorAccessToken token, CancellationToken cancellationToken)
    {
        ParkDataEditorAccessTokenDocument document = token.ToDocument();
        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task<bool> MarkUsedAsync(
        string tokenId,
        DateTime usedAtUtc,
        DateTime updateOnlyIfLastUsedBeforeUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDataEditorAccessTokenDocument> filter = Builders<ParkDataEditorAccessTokenDocument>.Filter.And(
            Builders<ParkDataEditorAccessTokenDocument>.Filter.Eq(item => item.Id, tokenId),
            Builders<ParkDataEditorAccessTokenDocument>.Filter.Or(
                Builders<ParkDataEditorAccessTokenDocument>.Filter.Eq(item => item.LastUsedAtUtc, null),
                Builders<ParkDataEditorAccessTokenDocument>.Filter.Lt(item => item.LastUsedAtUtc, updateOnlyIfLastUsedBeforeUtc)));
        UpdateDefinition<ParkDataEditorAccessTokenDocument> update = Builders<ParkDataEditorAccessTokenDocument>.Update
            .Set(item => item.LastUsedAtUtc, usedAtUtc)
            .Set(item => item.UpdatedAt, usedAtUtc);
        UpdateResult result = await this.collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<long> RevokeAsync(
        string userId,
        string? tokenId,
        string revokedByUserId,
        string reason,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<ParkDataEditorAccessTokenDocument> builder = Builders<ParkDataEditorAccessTokenDocument>.Filter;
        FilterDefinition<ParkDataEditorAccessTokenDocument> filter = builder.And(
            builder.Eq(item => item.UserId, userId),
            builder.Eq(item => item.RevokedAtUtc, null));
        if (!string.IsNullOrWhiteSpace(tokenId))
        {
            filter = builder.And(filter, builder.Eq(item => item.Id, tokenId));
        }

        UpdateDefinition<ParkDataEditorAccessTokenDocument> update = Builders<ParkDataEditorAccessTokenDocument>.Update
            .Set(item => item.RevokedAtUtc, revokedAtUtc)
            .Set(item => item.RevokedByUserId, revokedByUserId)
            .Set(item => item.RevocationReason, reason)
            .Set(item => item.UpdatedAt, revokedAtUtc);

        UpdateResult result = string.IsNullOrWhiteSpace(tokenId)
            ? await this.collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken)
            : await this.collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount;
    }
}
