using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Users;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo des refresh tokens opaques.
/// </summary>
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IMongoCollection<RefreshTokenDocument> collection;

    public RefreshTokenRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<RefreshTokenDocument>(settings.RefreshTokensCollectionName);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        RefreshTokenDocument? document = await this.collection
            .Find(document => document.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        RefreshTokenDocument document = refreshToken.ToDocument();
        document.CreatedAt = refreshToken.CreatedAtUtc == default ? DateTime.UtcNow : refreshToken.CreatedAtUtc;
        document.UpdatedAt = refreshToken.UpdatedAtUtc == default ? document.CreatedAt : refreshToken.UpdatedAtUtc;

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task<bool> RotateAsync(string currentTokenHash, RefreshToken replacementToken, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        FilterDefinition<RefreshTokenDocument> filter = Builders<RefreshTokenDocument>.Filter.And(
            Builders<RefreshTokenDocument>.Filter.Eq(document => document.TokenHash, currentTokenHash),
            Builders<RefreshTokenDocument>.Filter.Eq(document => document.RevokedAtUtc, null),
            Builders<RefreshTokenDocument>.Filter.Gt(document => document.ExpiresAtUtc, now));

        UpdateDefinition<RefreshTokenDocument> update = Builders<RefreshTokenDocument>.Update
            .Set(document => document.LastUsedAtUtc, now)
            .Set(document => document.RevokedAtUtc, now)
            .Set(document => document.ReplacedByTokenHash, replacementToken.TokenHash)
            .Set(document => document.RevocationReason, "Rotated")
            .Set(document => document.UpdatedAt, now);

        UpdateResult result = await this.collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0)
        {
            return false;
        }

        await this.CreateAsync(replacementToken, cancellationToken);
        return true;
    }

    public async Task<bool> RevokeAsync(string tokenHash, string reason, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        FilterDefinition<RefreshTokenDocument> filter = Builders<RefreshTokenDocument>.Filter.And(
            Builders<RefreshTokenDocument>.Filter.Eq(document => document.TokenHash, tokenHash),
            Builders<RefreshTokenDocument>.Filter.Eq(document => document.RevokedAtUtc, null));

        UpdateDefinition<RefreshTokenDocument> update = Builders<RefreshTokenDocument>.Update
            .Set(document => document.RevokedAtUtc, now)
            .Set(document => document.RevocationReason, reason)
            .Set(document => document.UpdatedAt, now);

        UpdateResult result = await this.collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }
}
