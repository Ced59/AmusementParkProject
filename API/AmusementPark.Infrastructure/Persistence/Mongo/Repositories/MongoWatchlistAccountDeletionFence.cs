using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class MongoWatchlistAccountDeletionFence : IWatchlistAccountDeletionFence
{
    private readonly IMongoCollection<WatchlistAccountDeletionFenceDocument> collection;

    public MongoWatchlistAccountDeletionFence(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<WatchlistAccountDeletionFenceDocument>(
            settings.WatchlistAccountDeletionFencesCollectionName);
    }

    public async Task BlockAsync(string userId, CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string deletionKey = BuildDeletionKey(normalizedUserId);
        DateTime nowUtc = DateTime.UtcNow;
        UpdateDefinition<WatchlistAccountDeletionFenceDocument> update =
            Builders<WatchlistAccountDeletionFenceDocument>.Update
                .SetOnInsert(static document => document.Id, deletionKey)
                .SetOnInsert(static document => document.CreatedAt, nowUtc)
                .Set(static document => document.UpdatedAt, nowUtc);
        await this.collection.UpdateOneAsync(
            document => document.Id == deletionKey,
            update,
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    public async Task<bool> IsBlockedAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string deletionKey = BuildDeletionKey(normalizedUserId);
        long count = await this.collection.CountDocumentsAsync(
            document => document.Id == deletionKey,
            new CountOptions { Limit = 1 },
            cancellationToken);
        return count > 0;
    }

    public async Task<IReadOnlySet<string>> ListBlockedAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        Dictionary<string, string> userIdsByDeletionKey = userIds
            .Where(static userId => !string.IsNullOrWhiteSpace(userId))
            .Select(userId => IdentifierRules.NormalizeRequired(userId, nameof(userIds)))
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(BuildDeletionKey, static userId => userId, StringComparer.Ordinal);
        if (userIdsByDeletionKey.Count == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        List<string> blocked = await this.collection
            .Find(Builders<WatchlistAccountDeletionFenceDocument>.Filter.In(
                static document => document.Id,
                userIdsByDeletionKey.Keys))
            .Project(static document => document.Id)
            .ToListAsync(cancellationToken);
        return blocked
            .Where(userIdsByDeletionKey.ContainsKey)
            .Select(deletionKey => userIdsByDeletionKey[deletionKey])
            .ToHashSet(StringComparer.Ordinal);
    }

    internal static string BuildDeletionKey(string userId)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedUserId));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
