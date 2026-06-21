using AmusementPark.Infrastructure.Persistence.Mongo.Documents.TechnicalPages;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeTechnicalPagesIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<TechnicalPageDocument> collection = this.database.GetCollection<TechnicalPageDocument>(this.settings.TechnicalPagesCollectionName);
        List<CreateIndexModel<TechnicalPageDocument>> indexes = new List<CreateIndexModel<TechnicalPageDocument>>
        {
            new CreateIndexModel<TechnicalPageDocument>(
                Builders<TechnicalPageDocument>.IndexKeys.Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_technical_pages_slug_unique", Unique = true }),
            new CreateIndexModel<TechnicalPageDocument>(
                Builders<TechnicalPageDocument>.IndexKeys
                    .Ascending(item => item.IsVisible)
                    .Ascending(item => item.CategoryKey)
                    .Ascending(item => item.SortOrder)
                    .Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_technical_pages_public_category_sort_slug" }),
            new CreateIndexModel<TechnicalPageDocument>(
                Builders<TechnicalPageDocument>.IndexKeys
                    .Ascending(item => item.AdminReviewPriority)
                    .Ascending(item => item.CategoryKey)
                    .Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_technical_pages_admin_review_category_slug" }),
            new CreateIndexModel<TechnicalPageDocument>(
                Builders<TechnicalPageDocument>.IndexKeys.Ascending("aliases.categoryKey").Ascending("aliases.labels.value"),
                new CreateIndexOptions { Name = "idx_technical_pages_aliases" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }
}
