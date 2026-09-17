using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class NotificationDigestMongoDefinitions
{
    internal static IReadOnlyCollection<CreateIndexModel<NotificationDigestDocument>> BuildIndexes()
    {
        return new List<CreateIndexModel<NotificationDigestDocument>>
        {
            new CreateIndexModel<NotificationDigestDocument>(
                Builders<NotificationDigestDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.Channel)
                    .Ascending(static document => document.Frequency)
                    .Ascending(static document => document.PeriodStart),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "uq_notification_digest_group",
                }),
        };
    }
}
