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

public sealed class CreateCommentCommandHandler : ICommandHandler<CreateCommentCommand, ApplicationResult<CommentResult>>
{
    private readonly ICommentRepository commentRepository;
    private readonly ICommentContentSanitizer contentSanitizer;
    private readonly IUserRepository userRepository;
    private readonly CommentTargetResolver targetResolver;
    private readonly CommentImageManager commentImageManager;

    public CreateCommentCommandHandler(
        ICommentRepository commentRepository,
        ICommentContentSanitizer contentSanitizer,
        IUserRepository userRepository,
        CommentTargetResolver targetResolver,
        CommentImageManager commentImageManager)
    {
        this.commentRepository = commentRepository;
        this.contentSanitizer = contentSanitizer;
        this.userRepository = userRepository;
        this.targetResolver = targetResolver;
        this.commentImageManager = commentImageManager;
    }

    public async Task<ApplicationResult<CommentResult>> HandleAsync(
        CreateCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.AuthorUserId))
        {
            return ApplicationResult<CommentResult>.Failure(ApplicationErrors.Required(nameof(command.AuthorUserId)));
        }

        if (string.IsNullOrWhiteSpace(command.Model.TargetId))
        {
            return ApplicationResult<CommentResult>.Failure(ApplicationErrors.Required(nameof(command.Model.TargetId)));
        }

        if (!Enum.IsDefined(command.Model.TargetType))
        {
            return ApplicationResult<CommentResult>.Failure(CommentApplicationErrors.InvalidTargetType());
        }

        User? author = await this.userRepository.GetByIdAsync(command.AuthorUserId.Trim(), cancellationToken);
        if (!IsAllowedAuthor(author))
        {
            return ApplicationResult<CommentResult>.Failure(CommentApplicationErrors.AuthorNotAllowed());
        }

        CommentTargetMetadataResult? target = await this.targetResolver.ResolveAsync(
            command.Model.TargetType,
            command.Model.TargetId.Trim(),
            true,
            cancellationToken);
        if (target is null)
        {
            return ApplicationResult<CommentResult>.Failure(CommentApplicationErrors.TargetNotFound());
        }

        ApplicationResult<IReadOnlyCollection<LocalizedText>> bodiesResult = CommentBodyNormalizer.Normalize(
            command.Model.Bodies,
            this.contentSanitizer);
        if (!bodiesResult.IsSuccess || bodiesResult.Value is null)
        {
            return ApplicationResult<CommentResult>.Failure(bodiesResult.Errors);
        }

        DateTime nowUtc = DateTime.UtcNow;
        string commentId = Guid.NewGuid().ToString("N");
        List<string> imageIds = ExtractImageIds(bodiesResult.Value, this.contentSanitizer);
        ApplicationResult<CommentImageReservationBatch> imageResult =
            await this.commentImageManager.PublishForCommentAsync(
                author!.Id,
                commentId,
                imageIds,
                cancellationToken,
                0);
        if (!imageResult.IsSuccess || imageResult.Value is null)
        {
            return ApplicationResult<CommentResult>.Failure(imageResult.Errors);
        }

        Comment comment = new Comment
        {
            Id = commentId,
            TargetType = target.TargetType,
            TargetId = target.TargetId,
            ParkId = target.ParkId,
            AuthorUserId = author!.Id,
            AuthorDisplayName = BuildAuthorDisplayName(author),
            AuthorAvatarUrl = NormalizeAvatarUrl(author.AvatarUrl),
            AuthorRole = ResolveAuthorRole(author),
            Bodies = bodiesResult.Value.ToList(),
            ImageIds = imageIds,
            IsOfficial = command.Model.IsOfficial,
            ModerationStatus = CommentModerationStatus.Published,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };
        Comment created;
        try
        {
            created = await this.commentRepository.CreateAsync(comment, cancellationToken);
        }
        catch
        {
            Comment? committed =
                await CommentPersistenceRecovery.TryResolveCreateAsync(
                    this.commentRepository,
                    comment);
            if (committed is null)
            {
                // L'écriture Mongo peut encore être committée malgré l'exception ou l'annulation.
                // La réservation reste privée jusqu'à ce que le reconciler vérifie la référence.
                _ = await this.commentImageManager.RestorePreparedCleanupForCommentAsync(commentId, imageResult.Value);
                throw;
            }

            created = committed;
        }

        _ = await this.commentImageManager.FinalizeForCommentAsync(
            author.Id,
            commentId,
            imageResult.Value);

        return ApplicationResult<CommentResult>.Success(CommentResultFactory.Create(created, author));
    }

    private static List<string> ExtractImageIds(
        IReadOnlyCollection<LocalizedText> bodies,
        ICommentContentSanitizer contentSanitizer)
    {
        return bodies
            .SelectMany(body => ContainsImage(body.Value)
                ? contentSanitizer.ExtractImageIds(body.Value ?? string.Empty)
                : Array.Empty<string>())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static bool ContainsImage(string? value)
    {
        return value?.Contains("<img", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsAllowedAuthor(User? author)
    {
        return author is not null
            && author.IsActivated
            && !author.IsBlocked
            && (author.HasRole(Role.Admin) || author.HasRole(Role.Moderator));
    }

    private static Role ResolveAuthorRole(User author)
    {
        return author.HasRole(Role.Admin) ? Role.Admin : Role.Moderator;
    }

    private static string BuildAuthorDisplayName(User author)
    {
        return author.ResolvePublicDisplayName() ?? "Amusement Parks";
    }

    private static string? NormalizeAvatarUrl(string? avatarUrl)
    {
        return string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
    }
}
