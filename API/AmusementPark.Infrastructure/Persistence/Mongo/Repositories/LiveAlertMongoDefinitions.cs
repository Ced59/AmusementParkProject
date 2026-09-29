using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class LiveAlertMongoDefinitions
{
    public static FilterDefinition<LiveAlertSubscriptionDocument> BuildPendingDeliveryFilter(
        DateTime nowUtc)
    {
        return Builders<LiveAlertSubscriptionDocument>.Filter.Ne(
                static document => document.PendingTrigger,
                null)
            & Builders<LiveAlertSubscriptionDocument>.Filter.Gt(
                static document => document.RetentionExpiresAt,
                nowUtc);
    }

    public static IReadOnlyCollection<CreateIndexModel<LiveAlertSubscriptionDocument>> BuildSubscriptionIndexes()
    {
        return new List<CreateIndexModel<LiveAlertSubscriptionDocument>>
        {
            new(
                Builders<LiveAlertSubscriptionDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.TargetId)
                    .Ascending(static document => document.Type)
                    .Ascending(static document => document.ThresholdMinutes),
                new CreateIndexOptions { Unique = true, Name = "uq_live_alert_subscription_identity" }),
            new(
                Builders<LiveAlertSubscriptionDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.QuotaSlot),
                new CreateIndexOptions { Unique = true, Name = "uq_live_alert_subscription_quota_slot" }),
            new(
                Builders<LiveAlertSubscriptionDocument>.IndexKeys
                    .Ascending(static document => document.TargetId)
                    .Ascending(static document => document.ExpiresAt),
                new CreateIndexOptions { Name = "ix_live_alert_subscription_evaluation" }),
            new(
                Builders<LiveAlertSubscriptionDocument>.IndexKeys
                    .Ascending(static document => document.PendingTrigger!.TriggeredAt)
                    .Ascending(static document => document.RetentionExpiresAt),
                new CreateIndexOptions { Name = "ix_live_alert_subscription_pending_delivery" }),
            new(
                Builders<LiveAlertSubscriptionDocument>.IndexKeys.Ascending(
                    static document => document.RetentionExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ttl_live_alert_subscription" }),
        };
    }

    public static IReadOnlyCollection<CreateIndexModel<LiveAlertNotificationDocument>> BuildNotificationIndexes()
    {
        return new List<CreateIndexModel<LiveAlertNotificationDocument>>
        {
            new(
                Builders<LiveAlertNotificationDocument>.IndexKeys.Ascending(static document => document.TriggerKey),
                new CreateIndexOptions { Unique = true, Name = "uq_live_alert_notification_trigger" }),
            new(
                Builders<LiveAlertNotificationDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.Status)
                    .Descending(static document => document.DeliveredAt),
                new CreateIndexOptions { Name = "ix_live_alert_notification_inbox" }),
            new(
                Builders<LiveAlertNotificationDocument>.IndexKeys.Ascending(static document => document.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ttl_live_alert_notification" }),
        };
    }
}
