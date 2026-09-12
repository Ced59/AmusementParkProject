using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Commands;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Comments.Handlers;

public sealed class DeleteCommentDraftImageCommandHandler
    : ICommandHandler<DeleteCommentDraftImageCommand, ApplicationResult>
{
    private readonly IUserRepository userRepository;
    private readonly CommentImageManager commentImageManager;

    public DeleteCommentDraftImageCommandHandler(
        IUserRepository userRepository,
        CommentImageManager commentImageManager)
    {
        this.userRepository = userRepository;
        this.commentImageManager = commentImageManager;
    }

    public async Task<ApplicationResult> HandleAsync(
        DeleteCommentDraftImageCommand command,
        CancellationToken cancellationToken = default)
    {
        User? actor = await CommentManagementAuthorization.GetActorAsync(
            command.ActorUserId,
            this.userRepository,
            cancellationToken);
        if (!CanManageCommentImages(actor))
        {
            return ApplicationResult.Failure(CommentApplicationErrors.AuthorNotAllowed());
        }

        if (string.IsNullOrWhiteSpace(command.ImageId))
        {
            return ApplicationResult.Failure(CommentApplicationErrors.ImageNotAllowed());
        }

        return await this.commentImageManager.DeleteOwnedDraftAsync(
            actor!.Id,
            command.ImageId,
            cancellationToken);
    }

    private static bool CanManageCommentImages(User? actor)
    {
        return actor is not null
            && (actor.HasRole(Role.Admin) || actor.HasRole(Role.Moderator));
    }
}
