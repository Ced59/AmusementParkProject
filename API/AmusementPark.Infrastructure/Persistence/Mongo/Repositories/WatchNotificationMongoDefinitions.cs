using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class WatchNotificationMongoDefinitions
{
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
                    .Ascending(static document => document.ExpiresAt),
                new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.Zero,
                    Name = "ttl_user_notification_retention",
                }),
        };
    }
}
