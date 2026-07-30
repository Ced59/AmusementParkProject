using AmusementPark.Core.Domain.Comments;

namespace AmusementPark.Application.Features.Comments.Ports;

public interface ICommentRepository
{
    Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken);

    Task<Comment?> GetByIdAsync(string commentId, CancellationToken cancellationToken);

    Task<Comment?> UpdateAsync(Comment comment, long expectedRevision, CancellationToken cancellationToken);

    Task<bool> TryAdvanceRevisionFenceAsync(
        string commentId,
        long expectedRevision,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string commentId, long expectedRevision, CancellationToken cancellationToken);

    Task<bool> IsImageReferencedAsync(string imageId, CancellationToken cancellationToken);

    Task<string?> GetReferencingCommentIdAsync(
        string imageId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Comment>> GetPublishedByTargetAsync(
        CommentTargetType targetType,
        string targetId,
        CancellationToken cancellationToken);

    Task<long> CountPublishedByTargetAsync(
        CommentTargetType targetType,
        string targetId,
        CancellationToken cancellationToken);

    Task<long> CountPublishedByTargetAndLanguageAsync(
        CommentTargetType targetType,
        string targetId,
        string languageCode,
        CancellationToken cancellationToken);

    Task<Comment?> GetFirstOfficialPublishedByTargetAsync(
        CommentTargetType targetType,
        string targetId,
        CancellationToken cancellationToken);
}
