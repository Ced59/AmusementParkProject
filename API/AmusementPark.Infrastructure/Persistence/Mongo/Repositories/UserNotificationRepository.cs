using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class UserNotificationRepository : IUserNotificationRepository
{
    private readonly IMongoCollection<UserNotificationDocument> collection;
    private readonly IWatchlistAccountDeletionFence? deletionFence;

    public UserNotificationRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        IWatchlistAccountDeletionFence deletionFence)
        : this(GetCollection(database, settings), deletionFence)
    {
    }

    internal UserNotificationRepository(IMongoCollection<UserNotificationDocument> collection)
        : this(collection, null)
    {
    }

    internal UserNotificationRepository(
        IMongoCollection<UserNotificationDocument> collection,
        IWatchlistAccountDeletionFence? deletionFence)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
        this.deletionFence = deletionFence;
    }

    public async Task<long> CreateManyAsync(
        IReadOnlyCollection<UserNotification> notifications,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notifications);
        UserNotification[] distinct = notifications
            .GroupBy(
                static notification => $"{notification.UserId}\n{notification.FactualEventId.Value}",
                StringComparer.Ordinal)
            .Select(static group => group.First())
            .ToArray();
        if (this.deletionFence is not null)
        {
            IReadOnlySet<string> blockedUserIds = await this.deletionFence.ListBlockedAsync(
                distinct.Select(static notification => notification.UserId).ToArray(),
                cancellationToken);
            distinct = distinct
                .Where(notification => !blockedUserIds.Contains(notification.UserId))
                .ToArray();
        }
        if (distinct.Length == 0)
        {
            return 0;
        }

        List<WriteModel<UserNotificationDocument>> writes = distinct
            .Select(notification => BuildInsert(notification.ToDocument()))
            .Cast<WriteModel<UserNotificationDocument>>()
            .ToList();
        long createdCount;
        try
        {
            BulkWriteResult<UserNotificationDocument> result = await this.collection.BulkWriteAsync(
                writes,
                new BulkWriteOptions { IsOrdered = false },
                cancellationToken);
            createdCount = result.Upserts.Count;
        }
        catch (MongoBulkWriteException<UserNotificationDocument> exception)
            when (IsDuplicateOnlyFailure(
                exception.WriteErrors.Select(static error => error.Category).ToArray(),
                exception.WriteConcernError is not null))
        {
            createdCount = exception.Result?.Upserts.Count ?? 0;
        }

        await this.DeleteNewlyBlockedAsync(distinct, cancellationToken);
        return createdCount;
    }

    private async Task DeleteNewlyBlockedAsync(
        IReadOnlyCollection<UserNotification> notifications,
        CancellationToken cancellationToken)
    {
        if (this.deletionFence is null || notifications.Count == 0)
        {
            return;
        }

        IReadOnlySet<string> blockedUserIds = await this.deletionFence.ListBlockedAsync(
            notifications.Select(static notification => notification.UserId).ToArray(),
            cancellationToken);
        string[] blockedNotificationIds = notifications
            .Where(notification => blockedUserIds.Contains(notification.UserId))
            .Select(static notification => notification.Id.Value)
            .ToArray();
        if (blockedNotificationIds.Length == 0)
        {
            return;
        }

        await this.collection.DeleteManyAsync(
            Builders<UserNotificationDocument>.Filter.In(
                static document => document.Id,
                blockedNotificationIds),
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserNotification>> ListByFactualEventAsync(
        FactualChangeEventId eventId,
        string? afterNotificationId,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        FilterDefinitionBuilder<UserNotificationDocument> filters = Builders<UserNotificationDocument>.Filter;
        FilterDefinition<UserNotificationDocument> filter = filters.Eq(
            static document => document.FactualEventId,
            eventId.Value);
        if (!string.IsNullOrWhiteSpace(afterNotificationId))
        {
            filter &= filters.Gt(static document => document.Id, afterNotificationId.Trim());
        }

        List<UserNotificationDocument> documents = await this.collection.Find(filter)
            .SortBy(static document => document.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyCollection<UserNotification>> ListByFactualEventAndUsersAsync(
        FactualChangeEventId eventId,
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        string[] normalizedUserIds = userIds
            .Select(userId => IdentifierRules.NormalizeRequired(userId, nameof(userIds)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedUserIds.Length == 0)
        {
            return Array.Empty<UserNotification>();
        }

        FilterDefinition<UserNotificationDocument> filter =
            Builders<UserNotificationDocument>.Filter.Eq(
                static document => document.FactualEventId,
                eventId.Value)
            & Builders<UserNotificationDocument>.Filter.In(
                static document => document.UserId,
                normalizedUserIds);
        List<UserNotificationDocument> documents = await this.collection.Find(filter)
            .SortBy(static document => document.UserId)
            .ThenBy(static document => document.Id)
            .Limit(normalizedUserIds.Length)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyCollection<UserNotification>> ListOwnedForDigestAsync(
        string userId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        IReadOnlyCollection<NotificationDigestSubscriptionFilter> subscriptionFilters,
        int limit,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        ArgumentNullException.ThrowIfNull(subscriptionFilters);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        if (periodStartUtc.Kind != DateTimeKind.Utc
            || periodEndUtc.Kind != DateTimeKind.Utc
            || periodEndUtc <= periodStartUtc)
        {
            throw new ArgumentException("The digest period must be a chronological UTC range.");
        }

        if (subscriptionFilters.Count == 0)
        {
            return Array.Empty<UserNotification>();
        }

        FilterDefinition<UserNotificationDocument> filter =
            WatchNotificationMongoDefinitions.BuildDigestNotificationFilter(
                normalizedUserId,
                periodStartUtc,
                periodEndUtc,
                subscriptionFilters);
        List<UserNotificationDocument> documents = await this.collection.Find(filter)
            .SortBy(static document => document.DeliveredAt)
            .ThenBy(static document => document.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyCollection<FactualChangeEventId>> ListDeliveredFactualEventIdsOwnedAsync(
        string userId,
        IReadOnlyCollection<FactualChangeEventId> eventIds,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        ArgumentNullException.ThrowIfNull(eventIds);
        string[] ids = eventIds
            .Select(static eventId => eventId.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<FactualChangeEventId>();
        }

        FilterDefinition<UserNotificationDocument> filter =
            Builders<UserNotificationDocument>.Filter.Eq(
                static document => document.UserId,
                normalizedUserId)
            & Builders<UserNotificationDocument>.Filter.In(
                static document => document.FactualEventId,
                ids);
        List<string> deliveredIds = await this.collection.Find(filter)
            .Project(static document => document.FactualEventId)
            .ToListAsync(cancellationToken);
        return deliveredIds
            .Distinct(StringComparer.Ordinal)
            .Select(FactualChangeEventId.Parse)
            .ToArray();
    }

    public async Task<long> RedeliverRetractionAsync(
        FactualChangeEventId eventId,
        IReadOnlyCollection<UserNotificationId> notificationIds,
        DateTime terminalAtUtc,
        DateTime deliveredAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notificationIds);
        if (terminalAtUtc.Kind != DateTimeKind.Utc || deliveredAtUtc.Kind != DateTimeKind.Utc
            || deliveredAtUtc < terminalAtUtc)
        {
            throw new ArgumentException("Retraction notification timestamps must be chronological UTC values.");
        }

        string[] ids = notificationIds
            .Select(static notificationId => notificationId.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (ids.Length == 0)
        {
            return 0;
        }

        FilterDefinition<UserNotificationDocument> filter =
            Builders<UserNotificationDocument>.Filter.Eq(
                static document => document.FactualEventId,
                eventId.Value)
            & Builders<UserNotificationDocument>.Filter.In(static document => document.Id, ids)
            & Builders<UserNotificationDocument>.Filter.Lt(
                static document => document.DeliveredAt,
                terminalAtUtc);
        UpdateResult result = await this.collection.UpdateManyAsync(
            filter,
            Builders<UserNotificationDocument>.Update
                .Set(static document => document.Status, UserNotificationStatus.Delivered)
                .Set(static document => document.DeliveredAt, deliveredAtUtc)
                .Set(static document => document.ReadAt, null)
                .Set(static document => document.DismissedAt, null)
                .Set(static document => document.ExpiresAt, deliveredAtUtc.AddDays(UserNotification.RetentionDays))
                .Set(static document => document.UpdatedAt, deliveredAtUtc)
                .Inc(static document => document.Version, 1),
            cancellationToken: cancellationToken);
        return result.ModifiedCount;
    }

    public async Task<PagedResult<UserNotification>> SearchOwnedAsync(
        string userId,
        UserNotificationSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        FilterDefinitionBuilder<UserNotificationDocument> filters = Builders<UserNotificationDocument>.Filter;
        FilterDefinition<UserNotificationDocument> filter = filters.Eq(
            static document => document.UserId,
            normalizedUserId)
            & filters.In(
                static document => document.Status,
                new[] { UserNotificationStatus.Delivered, UserNotificationStatus.Read });
        if (criteria.UnreadOnly)
        {
            filter &= filters.Eq(static document => document.Status, UserNotificationStatus.Delivered);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ParkId))
        {
            filter &= filters.Eq(static document => document.ParkId, criteria.ParkId.Trim());
        }

        if (criteria.EventType.HasValue)
        {
            filter &= filters.Eq(static document => document.EventType, criteria.EventType.Value);
        }

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        int skip = checked((criteria.Paging.Page - 1) * criteria.Paging.PageSize);
        List<UserNotificationDocument> documents = await this.collection.Find(filter)
            .SortByDescending(static document => document.DeliveredAt)
            .ThenBy(static document => document.Id)
            .Skip(skip)
            .Limit(criteria.Paging.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<UserNotification>(
            documents.Select(static document => document.ToDomain()).ToArray(),
            criteria.Paging.Page,
            criteria.Paging.PageSize,
            totalItems);
    }

    public Task<long> CountUnreadAsync(string userId, CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        return this.collection.CountDocumentsAsync(
            Builders<UserNotificationDocument>.Filter.Eq(static document => document.UserId, normalizedUserId)
            & Builders<UserNotificationDocument>.Filter.Eq(
                static document => document.Status,
                UserNotificationStatus.Delivered),
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> ListParkIdsOwnedAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        FilterDefinition<UserNotificationDocument> filter =
            Builders<UserNotificationDocument>.Filter.Eq(static document => document.UserId, normalizedUserId)
            & Builders<UserNotificationDocument>.Filter.In(
                static document => document.Status,
                new[] { UserNotificationStatus.Delivered, UserNotificationStatus.Read });
        IAsyncCursor<string> cursor = await this.collection.DistinctAsync(
            static document => document.ParkId,
            filter,
            cancellationToken: cancellationToken);
        return await cursor.ToListAsync(cancellationToken);
    }

    public async Task<UserNotification?> GetOwnedAsync(
        string userId,
        UserNotificationId notificationId,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        UserNotificationDocument? document = await this.collection.Find(
            Builders<UserNotificationDocument>.Filter.Eq(static item => item.UserId, normalizedUserId)
            & Builders<UserNotificationDocument>.Filter.Eq(static item => item.Id, notificationId.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<UserNotificationWriteOutcome> ReplaceAsync(
        UserNotification notification,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            Builders<UserNotificationDocument>.Filter.Eq(static item => item.Id, notification.Id.Value)
            & Builders<UserNotificationDocument>.Filter.Eq(static item => item.UserId, notification.UserId)
            & Builders<UserNotificationDocument>.Filter.Eq(static item => item.Version, expectedVersion),
            notification.ToDocument(),
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1
            ? UserNotificationWriteOutcome.Success
            : UserNotificationWriteOutcome.Conflict;
    }

    public async Task<long> MarkAllReadAsync(
        string userId,
        DateTime readAtUtc,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        if (readAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The read timestamp must use UTC.", nameof(readAtUtc));
        }

        UpdateResult result = await this.collection.UpdateManyAsync(
            Builders<UserNotificationDocument>.Filter.Eq(static item => item.UserId, normalizedUserId)
            & Builders<UserNotificationDocument>.Filter.Eq(
                static item => item.Status,
                UserNotificationStatus.Delivered),
            Builders<UserNotificationDocument>.Update
                .Set(static item => item.Status, UserNotificationStatus.Read)
                .Set(static item => item.ReadAt, readAtUtc)
                .Set(static item => item.UpdatedAt, readAtUtc)
                .Inc(static item => item.Version, 1),
            cancellationToken: cancellationToken);
        return result.ModifiedCount;
    }

    private static UpdateOneModel<UserNotificationDocument> BuildInsert(
        UserNotificationDocument document)
    {
        FilterDefinition<UserNotificationDocument> filter = Builders<UserNotificationDocument>.Filter.Eq(
            static item => item.UserId,
            document.UserId)
            & Builders<UserNotificationDocument>.Filter.Eq(
                static item => item.FactualEventId,
                document.FactualEventId);
        UpdateDefinition<UserNotificationDocument> update = Builders<UserNotificationDocument>.Update
            .SetOnInsert(static item => item.Id, document.Id)
            .SetOnInsert(static item => item.UserId, document.UserId)
            .SetOnInsert(static item => item.FactualEventId, document.FactualEventId)
            .SetOnInsert(static item => item.SubscriptionId, document.SubscriptionId)
            .SetOnInsert(static item => item.EventType, document.EventType)
            .SetOnInsert(static item => item.TargetType, document.TargetType)
            .SetOnInsert(static item => item.TargetId, document.TargetId)
            .SetOnInsert(static item => item.ParkId, document.ParkId)
            .SetOnInsert(static item => item.SourceRevision, document.SourceRevision)
            .SetOnInsert(static item => item.TemplateVersion, document.TemplateVersion)
            .SetOnInsert(static item => item.Language, document.Language)
            .SetOnInsert(static item => item.Status, document.Status)
            .SetOnInsert(static item => item.DeliveredAt, document.DeliveredAt)
            .SetOnInsert(static item => item.ReadAt, document.ReadAt)
            .SetOnInsert(static item => item.DismissedAt, document.DismissedAt)
            .SetOnInsert(static item => item.ExpiresAt, document.ExpiresAt)
            .SetOnInsert(static item => item.CreatedAt, document.CreatedAt)
            .SetOnInsert(static item => item.UpdatedAt, document.UpdatedAt)
            .SetOnInsert(static item => item.Version, document.Version);
        return new UpdateOneModel<UserNotificationDocument>(filter, update) { IsUpsert = true };
    }

    internal static bool IsDuplicateOnlyFailure(
        IReadOnlyCollection<ServerErrorCategory> errorCategories,
        bool hasWriteConcernError)
    {
        ArgumentNullException.ThrowIfNull(errorCategories);
        return !hasWriteConcernError
            && errorCategories.Count > 0
            && errorCategories.All(static category => category == ServerErrorCategory.DuplicateKey);
    }

    private static IMongoCollection<UserNotificationDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<UserNotificationDocument>(settings.UserNotificationsCollectionName);
    }
}
