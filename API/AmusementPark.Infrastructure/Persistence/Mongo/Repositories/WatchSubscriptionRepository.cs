using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class WatchSubscriptionRepository : IWatchSubscriptionRepository
{
    private readonly IMongoCollection<WatchSubscriptionDocument> collection;

    public WatchSubscriptionRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal WatchSubscriptionRepository(IMongoCollection<WatchSubscriptionDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<IReadOnlyCollection<WatchSubscription>> ListOwnedAsync(
        string userId,
        CollectionTargetType? targetType,
        string? targetId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        FilterDefinitionBuilder<WatchSubscriptionDocument> filters = Builders<WatchSubscriptionDocument>.Filter;
        FilterDefinition<WatchSubscriptionDocument> filter = filters.Eq(
            static document => document.UserId,
            normalizedUserId);
        if (targetType.HasValue)
        {
            filter &= filters.Eq(static document => document.TargetType, targetType.Value);
        }

        if (!string.IsNullOrWhiteSpace(targetId))
        {
            filter &= filters.Eq(static document => document.TargetId, targetId.Trim());
        }

        List<WatchSubscriptionDocument> documents = await this.collection.Find(filter)
            .SortByDescending(static document => document.UpdatedAt)
            .ThenBy(static document => document.Id)
            .Limit(WatchSubscription.MaximumSubscriptionsPerUser)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyCollection<WatchSubscription>> ListOwnedByIdsAsync(
        string userId,
        IReadOnlyCollection<WatchSubscriptionId> subscriptionIds,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string[] ids = subscriptionIds.Select(static id => id.Value).Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<WatchSubscription>();
        }

        FilterDefinition<WatchSubscriptionDocument> filter = Builders<WatchSubscriptionDocument>.Filter.Eq(
            static document => document.UserId,
            normalizedUserId)
            & Builders<WatchSubscriptionDocument>.Filter.In(static document => document.Id, ids);
        List<WatchSubscriptionDocument> documents = await this.collection.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<WatchSubscription?> GetOwnedAsync(
        string userId,
        WatchSubscriptionId subscriptionId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        WatchSubscriptionDocument? document = await this.collection.Find(
            Builders<WatchSubscriptionDocument>.Filter.Eq(static item => item.UserId, normalizedUserId)
            & Builders<WatchSubscriptionDocument>.Filter.Eq(static item => item.Id, subscriptionId.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<WatchSubscription?> GetOwnedByTargetAsync(
        string userId,
        CollectionTargetType targetType,
        string targetId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        string normalizedTargetId = IdentifierRules.NormalizeRequired(targetId, nameof(targetId));
        WatchSubscriptionDocument? document = await this.collection.Find(
            Builders<WatchSubscriptionDocument>.Filter.Eq(static item => item.UserId, normalizedUserId)
            & Builders<WatchSubscriptionDocument>.Filter.Eq(static item => item.TargetType, targetType)
            & Builders<WatchSubscriptionDocument>.Filter.Eq(static item => item.TargetId, normalizedTargetId))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<WatchSubscription>> ListMatchingAsync(
        FactualChangeEvent factualEvent,
        string? afterSubscriptionId,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(factualEvent);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        FilterDefinitionBuilder<WatchSubscriptionDocument> filters = Builders<WatchSubscriptionDocument>.Filter;
        FilterDefinition<WatchSubscriptionDocument> targetFilter = factualEvent.Target.Type == FactualTargetType.Park
            ? filters.Eq(static item => item.TargetType, CollectionTargetType.Park)
                & filters.Eq(static item => item.TargetId, factualEvent.Target.TargetId)
            : filters.Or(
                filters.Eq(static item => item.TargetType, CollectionTargetType.ParkItem)
                    & filters.Eq(static item => item.TargetId, factualEvent.Target.TargetId),
                filters.Eq(static item => item.TargetType, CollectionTargetType.Park)
                    & filters.Eq(static item => item.TargetId, factualEvent.Target.ParentParkId));
        FilterDefinition<WatchSubscriptionDocument> filter = targetFilter
            & filters.Eq(static item => item.IsPaused, false)
            & filters.AnyEq(static item => item.EventTypes, factualEvent.Type);
        if (!string.IsNullOrWhiteSpace(afterSubscriptionId))
        {
            filter &= filters.Gt(static item => item.Id, afterSubscriptionId.Trim());
        }

        List<WatchSubscriptionDocument> documents = await this.collection.Find(filter)
            .SortBy(static item => item.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<WatchSubscriptionWriteOutcome> CreateAsync(
        WatchSubscription subscription,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        List<WatchSubscriptionDocument> owned = await this.collection.Find(
            Builders<WatchSubscriptionDocument>.Filter.Eq(
                static document => document.UserId,
                subscription.UserId))
            .Limit(WatchSubscription.MaximumSubscriptionsPerUser)
            .ToListAsync(cancellationToken);
        if (owned.Any(document => document.TargetType == subscription.TargetType
            && string.Equals(document.TargetId, subscription.TargetId, StringComparison.Ordinal)))
        {
            return WatchSubscriptionWriteOutcome.AlreadyExists;
        }

        HashSet<int> occupiedSlots = owned.Select(static document => document.OwnerSlot).ToHashSet();
        for (int ownerSlot = 0; ownerSlot < WatchSubscription.MaximumSubscriptionsPerUser; ownerSlot++)
        {
            if (occupiedSlots.Contains(ownerSlot))
            {
                continue;
            }

            WatchSubscriptionDocument document = subscription.ToDocument();
            document.OwnerSlot = ownerSlot;
            try
            {
                await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
                return WatchSubscriptionWriteOutcome.Success;
            }
            catch (MongoWriteException exception)
                when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                WatchSubscription? existing = await this.GetOwnedByTargetAsync(
                    subscription.UserId,
                    subscription.TargetType,
                    subscription.TargetId,
                    cancellationToken);
                if (existing is not null)
                {
                    return WatchSubscriptionWriteOutcome.AlreadyExists;
                }
            }
        }

        return WatchSubscriptionWriteOutcome.LimitReached;
    }

    public async Task<WatchSubscriptionWriteOutcome> ReplaceAsync(
        WatchSubscription subscription,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            Builders<WatchSubscriptionDocument>.Filter.Eq(static item => item.Id, subscription.Id.Value)
            & Builders<WatchSubscriptionDocument>.Filter.Eq(static item => item.UserId, subscription.UserId)
            & Builders<WatchSubscriptionDocument>.Filter.Eq(static item => item.Version, expectedVersion),
            subscription.ToDocument(),
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1
            ? WatchSubscriptionWriteOutcome.Success
            : WatchSubscriptionWriteOutcome.Conflict;
    }

    public async Task<WatchSubscriptionWriteOutcome> DeleteAsync(
        string userId,
        WatchSubscriptionId subscriptionId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        DeleteResult result = await this.collection.DeleteOneAsync(
            Builders<WatchSubscriptionDocument>.Filter.Eq(static item => item.Id, subscriptionId.Value)
            & Builders<WatchSubscriptionDocument>.Filter.Eq(static item => item.UserId, normalizedUserId)
            & Builders<WatchSubscriptionDocument>.Filter.Eq(static item => item.Version, expectedVersion),
            cancellationToken);
        if (result.DeletedCount == 1)
        {
            return WatchSubscriptionWriteOutcome.Success;
        }

        WatchSubscription? existing = await this.GetOwnedAsync(normalizedUserId, subscriptionId, cancellationToken);
        return existing is null
            ? WatchSubscriptionWriteOutcome.NotFound
            : WatchSubscriptionWriteOutcome.Conflict;
    }

    private static IMongoCollection<WatchSubscriptionDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<WatchSubscriptionDocument>(settings.WatchSubscriptionsCollectionName);
    }
}
