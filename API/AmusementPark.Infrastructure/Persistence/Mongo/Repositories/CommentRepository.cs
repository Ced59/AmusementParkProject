using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Comments;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class CommentRepository : ICommentRepository
{
    private readonly IMongoCollection<CommentDocument> commentsCollection;

    public CommentRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.commentsCollection = database.GetCollection<CommentDocument>(settings.CommentsCollectionName);
    }

    public async Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken)
    {
        CommentDocument document = comment.ToDocument();
        await this.commentsCollection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<Comment?> GetByIdAsync(string commentId, CancellationToken cancellationToken)
    {
        CommentDocument? document = await this.commentsCollection
            .Find(value => value.Id == commentId.Trim())
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<Comment?> UpdateAsync(
        Comment comment,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        comment.Revision = checked(expectedRevision + 1);
        CommentDocument document = comment.ToDocument();
        FilterDefinitionBuilder<CommentDocument> builder = Builders<CommentDocument>.Filter;
        FilterDefinition<CommentDocument> filter =
            builder.Eq(static value => value.Id, document.Id)
            & builder.Eq(static value => value.Revision, expectedRevision);
        ReplaceOneResult result = await this.commentsCollection.ReplaceOneAsync(
            filter,
            document,
            cancellationToken: cancellationToken);

        return result.MatchedCount == 0 ? null : document.ToDomain();
    }

    public async Task<bool> TryAdvanceRevisionFenceAsync(
        string commentId,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        UpdateResult result = await this.commentsCollection.UpdateOneAsync(
            BuildRevisionFenceFilter(commentId, expectedRevision),
            BuildRevisionFenceUpdate(expectedRevision),
            cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    internal static FilterDefinition<CommentDocument> BuildRevisionFenceFilter(
        string commentId,
        long expectedRevision)
    {
        FilterDefinitionBuilder<CommentDocument> builder =
            Builders<CommentDocument>.Filter;
        return builder.Eq(
                static document => document.Id,
                commentId.Trim())
            & builder.Eq(
                static document => document.Revision,
                expectedRevision);
    }

    internal static UpdateDefinition<CommentDocument> BuildRevisionFenceUpdate(
        long expectedRevision)
    {
        return Builders<CommentDocument>.Update.Set(
            static document => document.Revision,
            checked(expectedRevision + 1));
    }

    public async Task<bool> DeleteAsync(
        string commentId,
        long expectedRevision,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<CommentDocument> builder = Builders<CommentDocument>.Filter;
        FilterDefinition<CommentDocument> filter =
            builder.Eq(static value => value.Id, commentId.Trim())
            & builder.Eq(static value => value.Revision, expectedRevision);
        DeleteResult result = await this.commentsCollection.DeleteOneAsync(
            filter,
            cancellationToken);

        return result.DeletedCount > 0;
    }

    public Task<bool> IsImageReferencedAsync(string imageId, CancellationToken cancellationToken)
    {
        FilterDefinition<CommentDocument> filter =
            Builders<CommentDocument>.Filter.AnyEq(static value => value.ImageIds, imageId.Trim());
        return this.commentsCollection.Find(filter).AnyAsync(cancellationToken);
    }

    public async Task<string?> GetReferencingCommentIdAsync(
        string imageId,
        CancellationToken cancellationToken)
    {
        FilterDefinition<CommentDocument> filter =
            Builders<CommentDocument>.Filter.AnyEq(
                static value => value.ImageIds,
                imageId.Trim());
        return await this.commentsCollection
            .Find(filter)
            .Project(static value => value.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Comment>> GetPublishedByTargetAsync(
        CommentTargetType targetType,
        string targetId,
        CancellationToken cancellationToken)
    {
        List<CommentDocument> documents = await this.commentsCollection
            .Find(BuildPublishedTargetFilter(targetType, targetId))
            .Sort(BuildPublicSort())
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public Task<long> CountPublishedByTargetAsync(
        CommentTargetType targetType,
        string targetId,
        CancellationToken cancellationToken)
    {
        return this.commentsCollection.CountDocumentsAsync(
            BuildPublishedTargetFilter(targetType, targetId),
            cancellationToken: cancellationToken);
    }

    public async Task<Comment?> GetFirstOfficialPublishedByTargetAsync(
        CommentTargetType targetType,
        string targetId,
        CancellationToken cancellationToken)
    {
        FilterDefinition<CommentDocument> filter = BuildPublishedTargetFilter(targetType, targetId)
            & Builders<CommentDocument>.Filter.Eq(static document => document.IsOfficial, true);
        CommentDocument? document = await this.commentsCollection
            .Find(filter)
            .SortByDescending(static value => value.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    private static FilterDefinition<CommentDocument> BuildPublishedTargetFilter(
        CommentTargetType targetType,
        string targetId)
    {
        return Builders<CommentDocument>.Filter.Eq(static document => document.TargetType, targetType)
            & Builders<CommentDocument>.Filter.Eq(static document => document.TargetId, targetId.Trim())
            & Builders<CommentDocument>.Filter.Eq(
                static document => document.ModerationStatus,
                CommentModerationStatus.Published);
    }

    private static SortDefinition<CommentDocument> BuildPublicSort()
    {
        return Builders<CommentDocument>.Sort
            .Descending(static document => document.IsOfficial)
            .Descending(static document => document.CreatedAt);
    }
}
