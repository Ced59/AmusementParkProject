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
            cancellationToken);
        if (!imageResult.IsSuccess || imageResult.Value is null)
        {
            return ApplicationResult<CommentResult>.Failure(imageResult.Errors);
        }

        Comment created;
        try
        {
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

            created = await this.commentRepository.CreateAsync(comment, cancellationToken);
        }
        catch
        {
            _ = await this.commentImageManager.ReleaseReservationsForCommentAsync(
                author!.Id,
                commentId,
                imageResult.Value);
            throw;
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
            cancellationToken);
        if (!imageResult.IsSuccess || imageResult.Value is null)
        {
            return ApplicationResult<CommentResult>.Failure(imageResult.Errors);
        }

        Comment? updated;
        try
        {
            List<string> removedImageIds = comment.ImageIds
                .Except(imageIds, StringComparer.Ordinal)
                .ToList();
            await this.commentImageManager.RequestRemovedCleanupAsync(
                comment.Id,
                removedImageIds,
                cancellationToken);
            long expectedRevision = comment.Revision;
            comment.UpdateContent(bodiesResult.Value, imageIds, isOfficial);
            updated = await this.commentRepository.UpdateAsync(
                comment,
                expectedRevision,
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
            comment.Id,
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

internal static class CommentQueryValidation
{
    public static async Task<ApplicationResult<CommentTargetMetadataResult>> ResolveTargetAsync(
        CommentTargetType targetType,
        string targetId,
        bool includeHidden,
        CommentTargetResolver targetResolver,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(targetType))
        {
            return ApplicationResult<CommentTargetMetadataResult>.Failure(CommentApplicationErrors.InvalidTargetType());
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            return ApplicationResult<CommentTargetMetadataResult>.Failure(ApplicationErrors.Required(nameof(targetId)));
        }

        CommentTargetMetadataResult? target = await targetResolver.ResolveAsync(
            targetType,
            targetId.Trim(),
            includeHidden,
            cancellationToken);
        return target is null
            ? ApplicationResult<CommentTargetMetadataResult>.Failure(CommentApplicationErrors.TargetNotFound())
            : ApplicationResult<CommentTargetMetadataResult>.Success(target);
    }
}

internal static class CommentBodyNormalizer
{
    private const int MaximumBodyLength = 12000;
    private static readonly HashSet<string> SupportedLanguages = new HashSet<string>(
        new[] { "fr", "en", "de", "nl", "it", "es", "pl", "pt" },
        StringComparer.OrdinalIgnoreCase);

    public static ApplicationResult<IReadOnlyCollection<LocalizedText>> Normalize(
        IReadOnlyCollection<LocalizedTextValue>? values,
        ICommentContentSanitizer contentSanitizer)
    {
        Dictionary<string, LocalizedText> normalized =
            new Dictionary<string, LocalizedText>(StringComparer.OrdinalIgnoreCase);
        bool hasPlainText = false;

        foreach (LocalizedTextValue value in values ?? Array.Empty<LocalizedTextValue>())
        {
            string languageCode = value.LanguageCode?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!SupportedLanguages.Contains(languageCode))
            {
                return ApplicationResult<IReadOnlyCollection<LocalizedText>>.Failure(
                    CommentApplicationErrors.InvalidLanguage());
            }

            if (value.Value.Length > MaximumBodyLength)
            {
                return ApplicationResult<IReadOnlyCollection<LocalizedText>>.Failure(
                    CommentApplicationErrors.BodyTooLong());
            }

            string sanitizedValue = contentSanitizer.SanitizeRichHtml(value.Value);
            string plainText = contentSanitizer.ExtractPlainText(sanitizedValue);
            bool hasImage = string.IsNullOrWhiteSpace(plainText)
                && contentSanitizer.ExtractImageIds(sanitizedValue).Count > 0;
            if (string.IsNullOrWhiteSpace(plainText) && !hasImage)
            {
                continue;
            }

            hasPlainText |= !string.IsNullOrWhiteSpace(plainText);

            if (sanitizedValue.Length > MaximumBodyLength)
            {
                return ApplicationResult<IReadOnlyCollection<LocalizedText>>.Failure(
                    CommentApplicationErrors.BodyTooLong());
            }

            normalized[languageCode] = new LocalizedText(languageCode, sanitizedValue);
        }

        if (normalized.Count == 0 || !hasPlainText)
        {
            return ApplicationResult<IReadOnlyCollection<LocalizedText>>.Failure(
                CommentApplicationErrors.EmptyBody());
        }

        return ApplicationResult<IReadOnlyCollection<LocalizedText>>.Success(
            normalized.Values.OrderBy(static value => value.LanguageCode, StringComparer.Ordinal).ToList());
    }
}

internal static class CommentManagementAuthorization
{
    public static async Task<User?> GetActorAsync(
        string actorUserId,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return null;
        }

        User? actor = await userRepository.GetByIdAsync(actorUserId.Trim(), cancellationToken);
        bool isAllowed = actor is not null
            && actor.IsActivated
            && !actor.IsBlocked;
        return isAllowed ? actor : null;
    }

}

internal static class CommentResultFactory
{
    public static CommentResult Create(Comment comment, User? currentAuthor = null)
    {
        bool hasCurrentAuthor = currentAuthor is not null
            && string.Equals(currentAuthor.Id, comment.AuthorUserId, StringComparison.Ordinal);
        string authorDisplayName = hasCurrentAuthor
            ? currentAuthor!.ResolvePublicDisplayName() ?? comment.AuthorDisplayName
            : comment.AuthorDisplayName;
        string? authorAvatarUrl = hasCurrentAuthor
            ? NormalizeAvatarUrl(currentAuthor!.AvatarUrl)
            : comment.AuthorAvatarUrl;
        Role authorRole = hasCurrentAuthor
            ? ResolveAuthorRole(currentAuthor!)
            : comment.AuthorRole;

        return new CommentResult(
            comment.Id,
            comment.TargetType,
            comment.TargetId,
            comment.AuthorUserId,
            authorDisplayName,
            authorAvatarUrl,
            authorRole,
            comment.Bodies,
            comment.IsOfficial,
            comment.CreatedAtUtc,
            comment.UpdatedAtUtc,
            comment.Revision);
    }

    private static Role ResolveAuthorRole(User author)
    {
        if (author.HasRole(Role.Admin))
        {
            return Role.Admin;
        }

        return author.HasRole(Role.Moderator) ? Role.Moderator : Role.User;
    }

    private static string? NormalizeAvatarUrl(string? avatarUrl)
    {
        return string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl.Trim();
    }
}
