using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Comments.Queries;
using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Comments.Handlers;

public sealed class GetCommentSummaryQueryHandler
    : IQueryHandler<GetCommentSummaryQuery, ApplicationResult<CommentSummaryResult>>
{
    private readonly ICommentRepository commentRepository;
    private readonly IUserRepository userRepository;
    private readonly CommentTargetResolver targetResolver;

    public GetCommentSummaryQueryHandler(
        ICommentRepository commentRepository,
        IUserRepository userRepository,
        CommentTargetResolver targetResolver)
    {
        this.commentRepository = commentRepository;
        this.userRepository = userRepository;
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
        User? officialAuthor = officialComment is null
            ? null
            : await this.userRepository.GetByIdAsync(officialComment.AuthorUserId, cancellationToken);
        return ApplicationResult<CommentSummaryResult>.Success(new CommentSummaryResult(
            query.TargetType,
            targetId,
            await countTask,
            officialComment is null ? null : CommentResultFactory.Create(officialComment, officialAuthor)));
    }
}

public sealed class GetCommentThreadQueryHandler
    : IQueryHandler<GetCommentThreadQuery, ApplicationResult<CommentThreadResult>>
{
    private readonly ICommentRepository commentRepository;
    private readonly IUserRepository userRepository;
    private readonly CommentTargetResolver targetResolver;

    public GetCommentThreadQueryHandler(
        ICommentRepository commentRepository,
        IUserRepository userRepository,
        CommentTargetResolver targetResolver)
    {
        this.commentRepository = commentRepository;
        this.userRepository = userRepository;
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
        IReadOnlyCollection<User> authors = await this.userRepository.GetByIdsAsync(
            comments
                .Select(static comment => comment.AuthorUserId)
                .Distinct(StringComparer.Ordinal)
                .ToList(),
            cancellationToken);
        IReadOnlyDictionary<string, User> authorsById = authors.ToDictionary(
            static author => author.Id,
            StringComparer.Ordinal);

        return ApplicationResult<CommentThreadResult>.Success(new CommentThreadResult(
            target.TargetType,
            target.TargetId,
            target.TargetName,
            target.ParkId,
            target.ParkName,
            comments
                .OrderByDescending(static comment => comment.IsOfficial)
                .ThenByDescending(static comment => comment.CreatedAtUtc)
                .Select(comment => CommentResultFactory.Create(
                    comment,
                    authorsById.GetValueOrDefault(comment.AuthorUserId)))
                .ToList()));
    }
}
