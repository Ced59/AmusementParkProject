using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class WatchNotificationMongoDefinitions
{
    internal static FilterDefinition<WatchSubscriptionDocument> BuildSubscriptionPublicationCutoff(
        DateTime publishedAtUtc)
    {
        if (publishedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The publication timestamp must use UTC.", nameof(publishedAtUtc));
        }

        FilterDefinitionBuilder<WatchSubscriptionDocument> filters =
            Builders<WatchSubscriptionDocument>.Filter;
        return filters.Lte(static document => document.CreatedAt, publishedAtUtc)
            & filters.Lte(static document => document.UpdatedAt, publishedAtUtc);
    }

    internal static UpdateDefinition<WatchSubscriptionDocument> BuildSubscriptionMutation(
        WatchSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        return Builders<WatchSubscriptionDocument>.Update
            .Set(
                static document => document.EventTypes,
                subscription.EventTypes.OrderBy(static eventType => eventType).ToList())
            .Set(static document => document.Frequency, subscription.Frequency)
            .Set(
                static document => document.Channels,
                subscription.Channels.OrderBy(static channel => channel).ToList())
            .Set(static document => document.IsPaused, subscription.IsPaused)
            .Set(static document => document.UpdatedAt, subscription.UpdatedAtUtc)
            .Set(static document => document.Version, subscription.Version);
    }

    internal static FilterDefinition<UserNotificationDocument> BuildDigestNotificationFilter(
        string userId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        IReadOnlyCollection<NotificationDigestSubscriptionFilter> subscriptionFilters)
    {
        FilterDefinitionBuilder<UserNotificationDocument> filters =
            Builders<UserNotificationDocument>.Filter;
        FilterDefinition<UserNotificationDocument>[] subscriptionClauses = subscriptionFilters
            .Select(filter => filters.Eq(
                    static document => document.SubscriptionId,
                    filter.SubscriptionId.Value)
                & filters.In(static document => document.EventType, filter.EventTypes))
            .ToArray();
        return filters.Eq(static document => document.UserId, userId)
            & filters.Gte(static document => document.DeliveredAt, periodStartUtc)
            & filters.Lt(static document => document.DeliveredAt, periodEndUtc)
            & filters.Or(subscriptionClauses);
    }

    internal static IReadOnlyCollection<CreateIndexModel<WatchSubscriptionDocument>> BuildSubscriptionIndexes()
    {
        return new List<CreateIndexModel<WatchSubscriptionDocument>>
        {
            new CreateIndexModel<WatchSubscriptionDocument>(
                Builders<WatchSubscriptionDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.TargetType)
                    .Ascending(static document => document.TargetId),
                new CreateIndexOptions { Unique = true, Name = "uq_watch_subscription_owner_target" }),
            new CreateIndexModel<WatchSubscriptionDocument>(
                Builders<WatchSubscriptionDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.OwnerSlot),
                new CreateIndexOptions { Unique = true, Name = "uq_watch_subscription_owner_slot" }),
            new CreateIndexModel<WatchSubscriptionDocument>(
                Builders<WatchSubscriptionDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Descending(static document => document.UpdatedAt)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions { Name = "ix_watch_subscription_owner_updated" }),
            new CreateIndexModel<WatchSubscriptionDocument>(
                Builders<WatchSubscriptionDocument>.IndexKeys
                    .Ascending(static document => document.TargetType)
                    .Ascending(static document => document.TargetId)
                    .Ascending(static document => document.IsPaused)
                    .Ascending(static document => document.EventTypes)
                    .Ascending(static document => document.CreatedAt)
                    .Ascending(static document => document.UpdatedAt)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions { Name = "ix_watch_subscription_distribution" }),
        };
    }

    internal static IReadOnlyCollection<CreateIndexModel<UserNotificationDocument>> BuildNotificationIndexes()
    {
        return new List<CreateIndexModel<UserNotificationDocument>>
        {
            new CreateIndexModel<UserNotificationDocument>(
                Builders<UserNotificationDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.FactualEventId),
                new CreateIndexOptions { Unique = true, Name = "uq_user_notification_event" }),
            new CreateIndexModel<UserNotificationDocument>(
                Builders<UserNotificationDocument>.IndexKeys
                    .Ascending(static document => document.FactualEventId)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions { Name = "ix_user_notification_correction_distribution" }),
            new CreateIndexModel<UserNotificationDocument>(
                Builders<UserNotificationDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.Status)
                    .Descending(static document => document.DeliveredAt)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions { Name = "ix_user_notification_inbox" }),
            new CreateIndexModel<UserNotificationDocument>(
                Builders<UserNotificationDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.ParkId)
                    .Ascending(static document => document.EventType)
                    .Ascending(static document => document.Status)
                    .Descending(static document => document.DeliveredAt),
                new CreateIndexOptions { Name = "ix_user_notification_filters" }),
            new CreateIndexModel<UserNotificationDocument>(
                Builders<UserNotificationDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.SubscriptionId)
                    .Ascending(static document => document.EventType)
                    .Ascending(static document => document.DeliveredAt)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions { Name = "ix_user_notification_digest" }),
            new CreateIndexModel<UserNotificationDocument>(
                Builders<UserNotificationDocument>.IndexKeys
                    .Ascending(static document => document.ExpiresAt),
                new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.Zero,
                    Name = "ttl_user_notification_retention",
                }),
        };
    }
}
