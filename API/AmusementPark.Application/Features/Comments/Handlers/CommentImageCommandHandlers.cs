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

public sealed class UploadCommentImageCommandHandler
    : ICommandHandler<UploadCommentImageCommand, ApplicationResult<UploadedImageResult>>
{
    private const long MaximumFileSizeInBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new HashSet<string>(
        new[] { "image/jpeg", "image/png", "image/webp" },
        StringComparer.OrdinalIgnoreCase);
    private readonly IUserRepository userRepository;
    private readonly IImageRepository imageRepository;
    private readonly ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>> uploadImageHandler;

    public UploadCommentImageCommandHandler(
        IUserRepository userRepository,
        IImageRepository imageRepository,
        ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>> uploadImageHandler)
    {
        this.userRepository = userRepository;
        this.imageRepository = imageRepository;
        this.uploadImageHandler = uploadImageHandler;
    }

    public async Task<ApplicationResult<UploadedImageResult>> HandleAsync(
        UploadCommentImageCommand command,
        CancellationToken cancellationToken = default)
    {
        User? actor = await CommentManagementAuthorization.GetActorAsync(
            command.ActorUserId,
            this.userRepository,
            cancellationToken);
        if (!CanManageCommentImages(actor))
        {
            return ApplicationResult<UploadedImageResult>.Failure(CommentApplicationErrors.AuthorNotAllowed());
        }

        if (command.File is null
            || command.File.Content == Stream.Null
            || command.File.Length <= 0
            || command.File.Length > MaximumFileSizeInBytes
            || string.IsNullOrWhiteSpace(command.File.FileName)
            || !AllowedContentTypes.Contains(command.File.ContentType))
        {
            return ApplicationResult<UploadedImageResult>.Failure(CommentApplicationErrors.ImageUploadInvalid());
        }

        IReadOnlyCollection<Image> existingDrafts = await this.imageRepository.GetByOwnerAsync(
            ImageOwnerType.CommentDraft,
            actor!.Id,
            ImageCategory.Comment,
            cancellationToken);
        if (existingDrafts.Count >= CommentImageManager.MaximumDraftImagesPerAuthor)
        {
            return ApplicationResult<UploadedImageResult>.Failure(CommentApplicationErrors.TooManyImages());
        }

        ImageUploadRequest request = new ImageUploadRequest
        {
            Category = ImageCategory.Comment,
            File = command.File,
            WithWatermark = true,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = actor.Id,
            IsPublished = false,
        };
        return await this.uploadImageHandler.HandleAsync(new UploadImageCommand(request), cancellationToken);
    }

    private static bool CanManageCommentImages(User? actor)
    {
        return actor is not null
            && (actor.HasRole(Role.Admin) || actor.HasRole(Role.Moderator));
    }
}

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
