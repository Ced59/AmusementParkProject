using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class LiveAlertSubscriptionRepository : ILiveAlertSubscriptionRepository
{
    private static readonly TimeSpan ActivityLeaseDuration = TimeSpan.FromMinutes(3);
    private readonly IMongoCollection<LiveAlertSubscriptionDocument> collection;
    private readonly IWatchlistAccountDeletionFence deletionFence;

    public LiveAlertSubscriptionRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        IWatchlistAccountDeletionFence deletionFence)
    {
        this.collection = database.GetCollection<LiveAlertSubscriptionDocument>(
            settings.LiveAlertSubscriptionsCollectionName);
        this.deletionFence = deletionFence;
    }

    public async Task<IReadOnlyCollection<LiveAlertSubscription>> ListOwnedAsync(
        string userId,
        string? targetId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        EnsureUtc(nowUtc);
        FilterDefinitionBuilder<LiveAlertSubscriptionDocument> filters =
            Builders<LiveAlertSubscriptionDocument>.Filter;
        FilterDefinition<LiveAlertSubscriptionDocument> filter =
            filters.Eq(static document => document.UserId, normalizedUserId)
            & filters.Gt(static document => document.ExpiresAt, nowUtc);
        if (!string.IsNullOrWhiteSpace(targetId))
        {
            filter &= filters.Eq(static document => document.TargetId, targetId.Trim());
        }

        List<LiveAlertSubscriptionDocument> documents = await this.collection.Find(filter)
            .SortBy(static document => document.ExpiresAt)
            .ThenBy(static document => document.Id)
            .Limit(LiveAlertSubscription.MaximumSubscriptionsPerUser)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyCollection<LiveAlertSubscription>> ListActiveMatchingAsync(
        IReadOnlyCollection<string> targetIds,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targetIds);
        EnsureUtc(nowUtc);
        string[] normalizedIds = targetIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedIds.Length == 0)
        {
            return Array.Empty<LiveAlertSubscription>();
        }

        List<LiveAlertSubscriptionDocument> documents = await this.collection.Find(
            Builders<LiveAlertSubscriptionDocument>.Filter.In(
                static document => document.TargetId,
                normalizedIds)
            & Builders<LiveAlertSubscriptionDocument>.Filter.Gt(
                static document => document.ExpiresAt,
                nowUtc))
            .SortBy(static document => document.TargetId)
            .ThenBy(static document => document.Id)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<WatchSubscriptionWriteOutcome> CreateAsync(
        LiveAlertSubscription subscription,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        EnsureUtc(nowUtc);
        string? leaseId = await this.deletionFence.TryAcquireActivityLeaseAsync(
            subscription.UserId,
            ActivityLeaseDuration,
            cancellationToken);
        if (leaseId is null)
        {
            return WatchSubscriptionWriteOutcome.Conflict;
        }

        try
        {
            long activeCount = await this.collection.CountDocumentsAsync(
                document => document.UserId == subscription.UserId && document.ExpiresAt > nowUtc,
                new CountOptions { Limit = LiveAlertSubscription.MaximumSubscriptionsPerUser },
                cancellationToken);
            if (activeCount >= LiveAlertSubscription.MaximumSubscriptionsPerUser)
            {
                return WatchSubscriptionWriteOutcome.LimitReached;
            }

            await this.collection.DeleteManyAsync(
                document => document.UserId == subscription.UserId
                    && document.TargetId == subscription.TargetId
                    && document.Type == subscription.Type
                    && document.ThresholdMinutes == subscription.ThresholdMinutes
                    && document.ExpiresAt <= nowUtc,
                cancellationToken);
            try
            {
                await this.collection.InsertOneAsync(
                    subscription.ToDocument(),
                    cancellationToken: cancellationToken);
                return WatchSubscriptionWriteOutcome.Success;
            }
            catch (MongoWriteException exception)
                when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                return WatchSubscriptionWriteOutcome.AlreadyExists;
            }
        }
        finally
        {
            await this.deletionFence.ReleaseActivityLeaseAsync(leaseId, CancellationToken.None);
        }
    }

    public async Task<WatchSubscriptionWriteOutcome> ReplaceAsync(
        LiveAlertSubscription subscription,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        LiveAlertSubscriptionDocument document = subscription.ToDocument();
        UpdateDefinition<LiveAlertSubscriptionDocument> update =
            Builders<LiveAlertSubscriptionDocument>.Update
                .Set(static item => item.LastObservedAt, document.LastObservedAt)
                .Set(static item => item.LastStatus, document.LastStatus)
                .Set(static item => item.LastWaitMinutes, document.LastWaitMinutes)
                .Set(static item => item.IsArmed, document.IsArmed)
                .Set(static item => item.LastTriggeredAt, document.LastTriggeredAt)
                .Set(static item => item.UpdatedAt, document.UpdatedAt)
                .Set(static item => item.Version, document.Version);
        UpdateResult result = await this.collection.UpdateOneAsync(
            item => item.Id == document.Id && item.Version == expectedVersion,
            update,
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1
            ? WatchSubscriptionWriteOutcome.Success
            : WatchSubscriptionWriteOutcome.Conflict;
    }

    public async Task<WatchSubscriptionWriteOutcome> DeleteAsync(
        string userId,
        LiveAlertSubscriptionId subscriptionId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        DeleteResult result = await this.collection.DeleteOneAsync(
            item => item.Id == subscriptionId.Value
                && item.UserId == normalizedUserId
                && item.Version == expectedVersion,
            cancellationToken);
        if (result.DeletedCount == 1)
        {
            return WatchSubscriptionWriteOutcome.Success;
        }

        long existing = await this.collection.CountDocumentsAsync(
            item => item.Id == subscriptionId.Value && item.UserId == normalizedUserId,
            new CountOptions { Limit = 1 },
            cancellationToken);
        return existing == 0
            ? WatchSubscriptionWriteOutcome.NotFound
            : WatchSubscriptionWriteOutcome.Conflict;
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The live alert repository clock must use UTC.", nameof(value));
        }
    }
}
