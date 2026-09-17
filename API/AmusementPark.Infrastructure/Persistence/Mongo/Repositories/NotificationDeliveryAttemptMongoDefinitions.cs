using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class NotificationDeliveryAttemptMongoDefinitions
{
    internal static IReadOnlyCollection<CreateIndexModel<NotificationDeliveryAttemptDocument>> BuildIndexes()
    {
        return new[]
        {
            new CreateIndexModel<NotificationDeliveryAttemptDocument>(
                Builders<NotificationDeliveryAttemptDocument>.IndexKeys
                    .Ascending(static item => item.DigestId),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "uq_notification_delivery_digest",
                }),
            new CreateIndexModel<NotificationDeliveryAttemptDocument>(
                Builders<NotificationDeliveryAttemptDocument>.IndexKeys
                    .Ascending(static item => item.Status)
                    .Descending(static item => item.UpdatedAt),
                new CreateIndexOptions { Name = "ix_notification_delivery_status_updated" }),
            new CreateIndexModel<NotificationDeliveryAttemptDocument>(
                Builders<NotificationDeliveryAttemptDocument>.IndexKeys
                    .Ascending(static item => item.ExpiresAt),
                new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.Zero,
                    Name = "ttl_notification_delivery_expiry",
                }),
            new CreateIndexModel<NotificationDeliveryAttemptDocument>(
                Builders<NotificationDeliveryAttemptDocument>.IndexKeys
                    .Ascending(static item => item.CreatedAt)
                    .Ascending(static item => item.Status),
                new CreateIndexOptions { Name = "ix_notification_delivery_pilot_created_status" }),
        };
    }
}
