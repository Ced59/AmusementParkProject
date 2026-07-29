using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Comments;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeCommentsIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<CommentDocument> collection =
            this.database.GetCollection<CommentDocument>(this.settings.CommentsCollectionName);
        await collection.UpdateManyAsync(
            BuildLegacyCommentRevisionFilter(),
            BuildLegacyCommentRevisionUpdate(),
            cancellationToken: cancellationToken);

        List<CreateIndexModel<CommentDocument>> indexes = new List<CreateIndexModel<CommentDocument>>
        {
            new CreateIndexModel<CommentDocument>(
                Builders<CommentDocument>.IndexKeys
                    .Ascending(static document => document.TargetType)
                    .Ascending(static document => document.TargetId)
                    .Ascending(static document => document.ModerationStatus)
                    .Descending(static document => document.IsOfficial)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "idx_comments_public_target" }),
            new CreateIndexModel<CommentDocument>(
                Builders<CommentDocument>.IndexKeys
                    .Ascending(static document => document.AuthorUserId)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "idx_comments_author_created" }),
            new CreateIndexModel<CommentDocument>(
                Builders<CommentDocument>.IndexKeys
                    .Ascending(static document => document.ParkId)
                    .Descending(static document => document.CreatedAt),
                new CreateIndexOptions { Name = "idx_comments_park_created" }),
            new CreateIndexModel<CommentDocument>(
                Builders<CommentDocument>.IndexKeys
                    .Ascending(static document => document.ImageIds),
                new CreateIndexOptions { Name = "idx_comments_image_ids" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    internal static FilterDefinition<CommentDocument> BuildLegacyCommentRevisionFilter()
    {
        return Builders<CommentDocument>.Filter.Exists(
            static document => document.Revision,
            false);
    }

    internal static UpdateDefinition<CommentDocument> BuildLegacyCommentRevisionUpdate()
    {
        return Builders<CommentDocument>.Update.Set(
            static document => document.Revision,
            0L);
    }
}
