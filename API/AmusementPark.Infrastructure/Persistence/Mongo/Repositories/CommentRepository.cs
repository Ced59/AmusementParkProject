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
