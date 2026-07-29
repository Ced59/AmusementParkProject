using AmusementPark.Core.Domain.Comments;

namespace AmusementPark.Application.Features.Comments.Ports;

public interface ICommentRepository
{
    Task<Comment> CreateAsync(Comment comment, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Comment>> GetPublishedByTargetAsync(
        CommentTargetType targetType,
        string targetId,
        CancellationToken cancellationToken);

    Task<long> CountPublishedByTargetAsync(
        CommentTargetType targetType,
        string targetId,
        CancellationToken cancellationToken);

    Task<Comment?> GetFirstOfficialPublishedByTargetAsync(
        CommentTargetType targetType,
        string targetId,
        CancellationToken cancellationToken);
}
