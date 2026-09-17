using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class NotificationEmailPreferenceMongoDefinitions
{
    internal static IReadOnlyCollection<CreateIndexModel<NotificationEmailPreferenceDocument>> BuildIndexes()
    {
        return new[]
        {
            new CreateIndexModel<NotificationEmailPreferenceDocument>(
                Builders<NotificationEmailPreferenceDocument>.IndexKeys
                    .Ascending(static item => item.UserId),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "uq_notification_email_preference_user",
                }),
        };
    }
}
