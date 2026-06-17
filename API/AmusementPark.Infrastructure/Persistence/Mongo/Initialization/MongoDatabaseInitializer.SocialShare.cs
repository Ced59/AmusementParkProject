using AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialShare;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeSocialShareEventIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<SocialShareEventDocument> collection = this.database.GetCollection<SocialShareEventDocument>(this.settings.SocialShareEventsCollectionName);
        List<CreateIndexModel<SocialShareEventDocument>> indexes = new List<CreateIndexModel<SocialShareEventDocument>>
        {
            new CreateIndexModel<SocialShareEventDocument>(
                Builders<SocialShareEventDocument>.IndexKeys.Descending(static document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_social_share_events_occurred_desc" }),
            new CreateIndexModel<SocialShareEventDocument>(
                Builders<SocialShareEventDocument>.IndexKeys
                    .Ascending(static document => document.Channel)
                    .Descending(static document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_social_share_events_channel_occurred" }),
            new CreateIndexModel<SocialShareEventDocument>(
                Builders<SocialShareEventDocument>.IndexKeys
                    .Ascending(static document => document.TargetType)
                    .Descending(static document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_social_share_events_target_occurred" }),
            new CreateIndexModel<SocialShareEventDocument>(
                Builders<SocialShareEventDocument>.IndexKeys
                    .Ascending(static document => document.VisitorKind)
                    .Descending(static document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_social_share_events_visitor_occurred" }),
            new CreateIndexModel<SocialShareEventDocument>(
                Builders<SocialShareEventDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Descending(static document => document.OccurredAtUtc),
                new CreateIndexOptions { Name = "idx_social_share_events_user_occurred" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }
}
