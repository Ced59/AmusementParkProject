using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeHistoryEventsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<HistoryEventDocument> collection = this.database.GetCollection<HistoryEventDocument>(this.settings.HistoryEventsCollectionName);
        List<CreateIndexModel<HistoryEventDocument>> indexes = new List<CreateIndexModel<HistoryEventDocument>>
        {
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.EntityType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.Key),
                new CreateIndexOptions { Name = "idx_history_owner_key_unique", Unique = true }),
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.EntityType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.Year)
                    .Ascending(item => item.Month)
                    .Ascending(item => item.Day),
                new CreateIndexOptions { Name = "idx_history_owner_visible_date" }),
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.ContextParkId)
                    .Ascending(item => item.EntityType)
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.Year)
                    .Ascending(item => item.Month)
                    .Ascending(item => item.Day),
                new CreateIndexOptions { Name = "idx_history_context_park_visible_date" }),
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.ParkId)
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.Year)
                    .Ascending(item => item.Month)
                    .Ascending(item => item.Day),
                new CreateIndexOptions { Name = "idx_history_park_visible_date" }),
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.ParkItemId)
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.Year)
                    .Ascending(item => item.Month)
                    .Ascending(item => item.Day),
                new CreateIndexOptions { Name = "idx_history_park_item_visible_date" }),
            new CreateIndexModel<HistoryEventDocument>(
                Builders<HistoryEventDocument>.IndexKeys
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.IsMajor)
                    .Ascending("article.isPublished")
                    .Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_history_public_articles" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }
}
