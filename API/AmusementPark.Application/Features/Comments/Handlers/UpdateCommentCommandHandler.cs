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

public sealed class UpdateCommentCommandHandler
    : ICommandHandler<UpdateCommentCommand, ApplicationResult<CommentResult>>
{
    private readonly ICommentRepository commentRepository;
    private readonly ICommentContentSanitizer contentSanitizer;
    private readonly IUserRepository userRepository;
    private readonly CommentImageManager commentImageManager;

    public UpdateCommentCommandHandler(
        ICommentRepository commentRepository,
        ICommentContentSanitizer contentSanitizer,
        IUserRepository userRepository,
        CommentImageManager commentImageManager)
    {
        this.commentRepository = commentRepository;
        this.contentSanitizer = contentSanitizer;
        this.userRepository = userRepository;
        this.commentImageManager = commentImageManager;
    }

    public async Task<ApplicationResult<CommentResult>> HandleAsync(
        UpdateCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        User? actor = await CommentManagementAuthorization.GetActorAsync(
            command.ActorUserId,
            this.userRepository,
            cancellationToken);
        if (actor is null)
        {
            return ApplicationResult<CommentResult>.Failure(CommentApplicationErrors.ManagerNotAllowed());
        }

        if (string.IsNullOrWhiteSpace(command.CommentId))
        {
            return ApplicationResult<CommentResult>.Failure(CommentApplicationErrors.CommentNotFound());
        }

        string commentId = command.CommentId.Trim();
        Comment? comment = await this.commentRepository.GetByIdAsync(commentId, cancellationToken);
        if (comment is null)
        {
            return ApplicationResult<CommentResult>.Failure(CommentApplicationErrors.CommentNotFound());
        }

        if (!comment.CanBeManagedBy(actor))
        {
            return ApplicationResult<CommentResult>.Failure(CommentApplicationErrors.ManagerNotAllowed());
        }

        if (command.Model.ExpectedRevision.HasValue
            && command.Model.ExpectedRevision.Value != comment.Revision)
        {
            return ApplicationResult<CommentResult>.Failure(
                CommentApplicationErrors.ConcurrentModification());
        }

        ApplicationResult<IReadOnlyCollection<LocalizedText>> bodiesResult = CommentBodyNormalizer.Normalize(
            command.Model.Bodies,
            this.contentSanitizer);
        if (!bodiesResult.IsSuccess || bodiesResult.Value is null)
        {
            return ApplicationResult<CommentResult>.Failure(bodiesResult.Errors);
        }

        bool isOfficial = Comment.CanManageOfficialStatus(actor)
            ? command.Model.IsOfficial
            : comment.IsOfficial;
        List<string> imageIds = bodiesResult.Value
            .SelectMany(body => body.Value?.Contains("<img", StringComparison.OrdinalIgnoreCase) == true
                ? this.contentSanitizer.ExtractImageIds(body.Value)
                : Array.Empty<string>())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        ApplicationResult<CommentImageReservationBatch> imageResult =
            await this.commentImageManager.PublishForCommentAsync(
                actor.Id,
                comment.Id,
                imageIds,
                cancellationToken,
                checked(comment.Revision + 1));
        if (!imageResult.IsSuccess || imageResult.Value is null)
        {
            return ApplicationResult<CommentResult>.Failure(imageResult.Errors);
        }

        try
        {
            List<string> removedImageIds = comment.ImageIds
                .Except(imageIds, StringComparer.Ordinal)
                .ToList();
            await this.commentImageManager.RequestRemovedCleanupAsync(
                comment.Id, checked(comment.Revision + 1),
                removedImageIds,
                cancellationToken);
        }
        catch
        {
            _ = await this.commentImageManager.ReleaseReservationsForCommentAsync(
                actor.Id,
                comment.Id,
                imageResult.Value);
            throw;
        }

        long expectedRevision = comment.Revision;
        comment.UpdateContent(bodiesResult.Value, imageIds, isOfficial);
        Comment? updated = await CommentPersistenceRecovery.UpdateAsync(
            this.commentRepository,
            comment,
            expectedRevision,
            cancellationToken);
        if (updated is null)
        {
            _ = await this.commentImageManager.ReleaseReservationsForCommentAsync(
                actor.Id,
                comment.Id,
                imageResult.Value);
            return ApplicationResult<CommentResult>.Failure(
                CommentApplicationErrors.ConcurrentModification());
        }

        _ = await this.commentImageManager.FinalizeForCommentAsync(
            actor.Id,
            comment.Id,
            imageResult.Value);

        User? author = string.Equals(actor.Id, comment.AuthorUserId, StringComparison.Ordinal)
            ? actor
            : await this.userRepository.GetByIdAsync(comment.AuthorUserId, cancellationToken);
        return ApplicationResult<CommentResult>.Success(CommentResultFactory.Create(updated, author));
    }
}
