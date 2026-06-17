using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Videos;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeVideosIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<VideoDocument> collection = this.database.GetCollection<VideoDocument>(this.settings.VideosCollectionName);
        List<CreateIndexModel<VideoDocument>> indexes = new List<CreateIndexModel<VideoDocument>>
        {
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.OwnerType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_videos_owner_published_created_desc" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.OwnerType)
                    .Ascending(item => item.OwnerId)
                    .Ascending(item => item.Type)
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_videos_owner_type_published_created_desc" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.HostingProvider)
                    .Ascending(item => item.ExternalId),
                new CreateIndexOptions { Name = "idx_videos_provider_external_id" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys.Ascending("tagIds"),
                new CreateIndexOptions { Name = "idx_videos_tag_ids" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.CreatorName)
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_videos_creator_published_created_desc" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Ascending(item => item.IsPublished)
                    .Descending(item => item.CreatedAt),
                new CreateIndexOptions { Name = "idx_videos_published_created_desc" }),
            new CreateIndexModel<VideoDocument>(
                Builders<VideoDocument>.IndexKeys
                    .Text(item => item.Title)
                    .Text(item => item.Description)
                    .Text(item => item.CreatorName)
                    .Text(item => item.CanonicalUrl)
                    .Text(item => item.ExternalId),
                new CreateIndexOptions { Name = "idx_videos_admin_text" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task InitializeVideoTagsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<VideoTagDocument> collection = this.database.GetCollection<VideoTagDocument>(this.settings.VideoTagsCollectionName);
        List<CreateIndexModel<VideoTagDocument>> indexes = new List<CreateIndexModel<VideoTagDocument>>
        {
            new CreateIndexModel<VideoTagDocument>(
                Builders<VideoTagDocument>.IndexKeys.Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "idx_video_tags_slug_unique", Unique = true }),
            new CreateIndexModel<VideoTagDocument>(
                Builders<VideoTagDocument>.IndexKeys.Ascending(item => item.IsActive),
                new CreateIndexOptions { Name = "idx_video_tags_is_active" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }
}
