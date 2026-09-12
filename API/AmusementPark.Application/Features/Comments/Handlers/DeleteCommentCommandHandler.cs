using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Commands;
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Comments.Queries;
using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Comments.Handlers;

public sealed class DeleteCommentCommandHandler : ICommandHandler<DeleteCommentCommand, ApplicationResult>
{
    private readonly ICommentRepository commentRepository;
    private readonly IUserRepository userRepository;
    private readonly CommentImageManager commentImageManager;

    public DeleteCommentCommandHandler(
        ICommentRepository commentRepository,
        IUserRepository userRepository,
        CommentImageManager commentImageManager)
    {
        this.commentRepository = commentRepository;
        this.userRepository = userRepository;
        this.commentImageManager = commentImageManager;
    }

    public async Task<ApplicationResult> HandleAsync(
        DeleteCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        User? actor = await CommentManagementAuthorization.GetActorAsync(
            command.ActorUserId,
            this.userRepository,
            cancellationToken);
        if (actor is null)
        {
            return ApplicationResult.Failure(CommentApplicationErrors.ManagerNotAllowed());
        }

        if (string.IsNullOrWhiteSpace(command.CommentId))
        {
            return ApplicationResult.Failure(CommentApplicationErrors.CommentNotFound());
        }

        string commentId = command.CommentId.Trim();
        Comment? comment = await this.commentRepository.GetByIdAsync(commentId, cancellationToken);
        if (comment is null)
        {
            return ApplicationResult.Failure(CommentApplicationErrors.CommentNotFound());
        }

        if (!comment.CanBeManagedBy(actor))
        {
            return ApplicationResult.Failure(CommentApplicationErrors.ManagerNotAllowed());
        }

        if (command.ExpectedRevision.HasValue
            && command.ExpectedRevision.Value != comment.Revision)
        {
            return ApplicationResult.Failure(CommentApplicationErrors.ConcurrentModification());
        }

        await this.commentImageManager.RequestRemovedCleanupAsync(
            comment.Id, checked(comment.Revision + 1),
            comment.ImageIds,
            cancellationToken);
        bool deleted = await this.commentRepository.DeleteAsync(
            commentId,
            comment.Revision,
            cancellationToken);

        return deleted
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(CommentApplicationErrors.ConcurrentModification());
    }
}
