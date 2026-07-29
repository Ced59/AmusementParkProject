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

    public CreateCommentCommandHandler(
        ICommentRepository commentRepository,
        ICommentContentSanitizer contentSanitizer,
        IUserRepository userRepository,
        CommentTargetResolver targetResolver)
    {
        this.commentRepository = commentRepository;
        this.contentSanitizer = contentSanitizer;
        this.userRepository = userRepository;
        this.targetResolver = targetResolver;
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
        Comment comment = new Comment
        {
            TargetType = target.TargetType,
            TargetId = target.TargetId,
            ParkId = target.ParkId,
            AuthorUserId = author!.Id,
            AuthorDisplayName = BuildAuthorDisplayName(author),
            AuthorRole = ResolveAuthorRole(author),
            Bodies = bodiesResult.Value.ToList(),
            IsOfficial = command.Model.IsOfficial,
            ModerationStatus = CommentModerationStatus.Published,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
        };

        Comment created = await this.commentRepository.CreateAsync(comment, cancellationToken);
        return ApplicationResult<CommentResult>.Success(CommentResultFactory.Create(created));
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
        string displayName = string.Join(
            " ",
            new[] { author.FirstName, author.LastName }
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value!.Trim()));

        return string.IsNullOrWhiteSpace(displayName) ? "Équipe Amusement Parks" : displayName;
    }
}

public sealed class UpdateCommentCommandHandler
    : ICommandHandler<UpdateCommentCommand, ApplicationResult<CommentResult>>
{
    private readonly ICommentRepository commentRepository;
    private readonly ICommentContentSanitizer contentSanitizer;
    private readonly IUserRepository userRepository;

    public UpdateCommentCommandHandler(
        ICommentRepository commentRepository,
        ICommentContentSanitizer contentSanitizer,
        IUserRepository userRepository)
    {
        this.commentRepository = commentRepository;
        this.contentSanitizer = contentSanitizer;
        this.userRepository = userRepository;
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

        ApplicationResult<IReadOnlyCollection<LocalizedText>> bodiesResult = CommentBodyNormalizer.Normalize(
            command.Model.Bodies,
            this.contentSanitizer);
        if (!bodiesResult.IsSuccess || bodiesResult.Value is null)
        {
            return ApplicationResult<CommentResult>.Failure(bodiesResult.Errors);
        }

        comment.UpdateContent(bodiesResult.Value, command.Model.IsOfficial);
        Comment? updated = await this.commentRepository.UpdateAsync(comment, cancellationToken);
        return updated is null
            ? ApplicationResult<CommentResult>.Failure(CommentApplicationErrors.CommentNotFound())
            : ApplicationResult<CommentResult>.Success(CommentResultFactory.Create(updated));
    }
}

public sealed class DeleteCommentCommandHandler : ICommandHandler<DeleteCommentCommand, ApplicationResult>
{
    private readonly ICommentRepository commentRepository;
    private readonly IUserRepository userRepository;

    public DeleteCommentCommandHandler(
        ICommentRepository commentRepository,
        IUserRepository userRepository)
    {
        this.commentRepository = commentRepository;
        this.userRepository = userRepository;
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

        bool deleted = await this.commentRepository.DeleteAsync(commentId, cancellationToken);
        return deleted
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(CommentApplicationErrors.CommentNotFound());
    }
}

public sealed class GetCommentSummaryQueryHandler
    : IQueryHandler<GetCommentSummaryQuery, ApplicationResult<CommentSummaryResult>>
{
    private readonly ICommentRepository commentRepository;
    private readonly CommentTargetResolver targetResolver;

    public GetCommentSummaryQueryHandler(
        ICommentRepository commentRepository,
        CommentTargetResolver targetResolver)
    {
        this.commentRepository = commentRepository;
        this.targetResolver = targetResolver;
    }

    public async Task<ApplicationResult<CommentSummaryResult>> HandleAsync(
        GetCommentSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<CommentTargetMetadataResult> targetResult = await CommentQueryValidation.ResolveTargetAsync(
            query.TargetType,
            query.TargetId,
            query.IncludeHidden,
            this.targetResolver,
            cancellationToken);
        if (!targetResult.IsSuccess || targetResult.Value is null)
        {
            return ApplicationResult<CommentSummaryResult>.Failure(targetResult.Errors);
        }

        string targetId = targetResult.Value.TargetId;
        Task<long> countTask = this.commentRepository.CountPublishedByTargetAsync(
            query.TargetType,
            targetId,
            cancellationToken);
        Task<Comment?> officialTask = this.commentRepository.GetFirstOfficialPublishedByTargetAsync(
            query.TargetType,
            targetId,
            cancellationToken);

        await Task.WhenAll(countTask, officialTask);
        Comment? officialComment = await officialTask;
        return ApplicationResult<CommentSummaryResult>.Success(new CommentSummaryResult(
            query.TargetType,
            targetId,
            await countTask,
            officialComment is null ? null : CommentResultFactory.Create(officialComment)));
    }
}

public sealed class GetCommentThreadQueryHandler
    : IQueryHandler<GetCommentThreadQuery, ApplicationResult<CommentThreadResult>>
{
    private readonly ICommentRepository commentRepository;
    private readonly CommentTargetResolver targetResolver;

    public GetCommentThreadQueryHandler(
        ICommentRepository commentRepository,
        CommentTargetResolver targetResolver)
    {
        this.commentRepository = commentRepository;
        this.targetResolver = targetResolver;
    }

    public async Task<ApplicationResult<CommentThreadResult>> HandleAsync(
        GetCommentThreadQuery query,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<CommentTargetMetadataResult> targetResult = await CommentQueryValidation.ResolveTargetAsync(
            query.TargetType,
            query.TargetId,
            query.IncludeHidden,
            this.targetResolver,
            cancellationToken);
        if (!targetResult.IsSuccess || targetResult.Value is null)
        {
            return ApplicationResult<CommentThreadResult>.Failure(targetResult.Errors);
        }

        CommentTargetMetadataResult target = targetResult.Value;
        IReadOnlyCollection<Comment> comments = await this.commentRepository.GetPublishedByTargetAsync(
            target.TargetType,
            target.TargetId,
            cancellationToken);

        return ApplicationResult<CommentThreadResult>.Success(new CommentThreadResult(
            target.TargetType,
            target.TargetId,
            target.TargetName,
            target.ParkId,
            target.ParkName,
            comments
                .OrderByDescending(static comment => comment.IsOfficial)
                .ThenByDescending(static comment => comment.CreatedAtUtc)
                .Select(CommentResultFactory.Create)
                .ToList()));
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
            if (string.IsNullOrWhiteSpace(plainText))
            {
                continue;
            }

            if (sanitizedValue.Length > MaximumBodyLength)
            {
                return ApplicationResult<IReadOnlyCollection<LocalizedText>>.Failure(
                    CommentApplicationErrors.BodyTooLong());
            }

            normalized[languageCode] = new LocalizedText(languageCode, sanitizedValue);
        }

        if (normalized.Count == 0)
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
            && !actor.IsBlocked
            && (actor.HasRole(Role.Admin) || actor.HasRole(Role.Moderator));
        return isAllowed ? actor : null;
    }

}

internal static class CommentResultFactory
{
    public static CommentResult Create(Comment comment)
    {
        return new CommentResult(
            comment.Id,
            comment.TargetType,
            comment.TargetId,
            comment.AuthorUserId,
            comment.AuthorDisplayName,
            comment.AuthorRole,
            comment.Bodies,
            comment.IsOfficial,
            comment.CreatedAtUtc,
            comment.UpdatedAtUtc);
    }
}
