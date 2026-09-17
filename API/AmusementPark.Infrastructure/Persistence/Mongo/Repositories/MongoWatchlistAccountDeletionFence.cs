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
    private static readonly TimeSpan LeasePollInterval = TimeSpan.FromMilliseconds(100);
    private readonly IMongoCollection<WatchlistAccountDeletionFenceDocument> collection;
    private readonly IMongoCollection<WatchlistAccountDeletionLeaseDocument> leases;

    public MongoWatchlistAccountDeletionFence(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<WatchlistAccountDeletionFenceDocument>(
            settings.WatchlistAccountDeletionFencesCollectionName);
        this.leases = database.GetCollection<WatchlistAccountDeletionLeaseDocument>(
            settings.WatchlistAccountDeletionLeasesCollectionName);
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
                .Set(static document => document.IsBlocked, true)
                .Set(static document => document.UpdatedAt, nowUtc);
        await this.collection.UpdateOneAsync(
            document => document.Id == deletionKey,
            update,
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
        await this.WaitForDeliveryLeasesAsync(deletionKey, cancellationToken);
    }

    public async Task<bool> IsBlockedAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string deletionKey = BuildDeletionKey(normalizedUserId);
        long count = await this.collection.CountDocumentsAsync(
            document => document.Id == deletionKey && document.IsBlocked,
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
                userIdsByDeletionKey.Keys)
                & Builders<WatchlistAccountDeletionFenceDocument>.Filter.Eq(
                    static document => document.IsBlocked,
                    true))
            .Project(static document => document.Id)
            .ToListAsync(cancellationToken);
        return blocked
            .Where(userIdsByDeletionKey.ContainsKey)
            .Select(deletionKey => userIdsByDeletionKey[deletionKey])
            .ToHashSet(StringComparer.Ordinal);
    }

    public async Task<string?> TryAcquireDeliveryLeaseAsync(
        string userId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        if (leaseDuration <= TimeSpan.Zero || leaseDuration > TimeSpan.FromMinutes(10))
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        if (await this.IsBlockedAsync(normalizedUserId, cancellationToken))
        {
            return null;
        }

        DateTime nowUtc = DateTime.UtcNow;
        WatchlistAccountDeletionLeaseDocument lease = new WatchlistAccountDeletionLeaseDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            UserKey = BuildDeletionKey(normalizedUserId),
            ExpiresAt = nowUtc.Add(leaseDuration),
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
        };
        await this.leases.InsertOneAsync(lease, cancellationToken: cancellationToken);
        if (!await this.IsBlockedAsync(normalizedUserId, cancellationToken))
        {
            return lease.Id;
        }

        await this.ReleaseDeliveryLeaseAsync(lease.Id, cancellationToken);
        return null;
    }

    public async Task ReleaseDeliveryLeaseAsync(
        string leaseId,
        CancellationToken cancellationToken)
    {
        string normalizedLeaseId = IdentifierRules.NormalizeRequired(leaseId, nameof(leaseId));
        await this.leases.DeleteOneAsync(
            document => document.Id == normalizedLeaseId,
            cancellationToken);
    }

    private async Task WaitForDeliveryLeasesAsync(
        string deletionKey,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            DateTime nowUtc = DateTime.UtcNow;
            await this.leases.DeleteManyAsync(
                document => document.UserKey == deletionKey && document.ExpiresAt <= nowUtc,
                cancellationToken);
            long activeLeaseCount = await this.leases.CountDocumentsAsync(
                document => document.UserKey == deletionKey && document.ExpiresAt > nowUtc,
                new CountOptions { Limit = 1 },
                cancellationToken);
            if (activeLeaseCount == 0)
            {
                return;
            }

            await Task.Delay(LeasePollInterval, cancellationToken);
        }
    }

    internal static string BuildDeletionKey(string userId)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedUserId));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
