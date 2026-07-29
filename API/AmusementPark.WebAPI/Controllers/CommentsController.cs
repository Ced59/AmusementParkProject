using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Commands;
using AmusementPark.Application.Features.Comments.Queries;
using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.WebAPI.AdminPublicView;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Comments;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("comments")]
public sealed class CommentsController : ControllerBase
{
    private readonly IQueryHandler<GetCommentSummaryQuery, ApplicationResult<CommentSummaryResult>> getSummaryHandler;
    private readonly IQueryHandler<GetCommentThreadQuery, ApplicationResult<CommentThreadResult>> getThreadHandler;
    private readonly ICommandHandler<CreateCommentCommand, ApplicationResult<CommentResult>> createCommentHandler;

    public CommentsController(
        IQueryHandler<GetCommentSummaryQuery, ApplicationResult<CommentSummaryResult>> getSummaryHandler,
        IQueryHandler<GetCommentThreadQuery, ApplicationResult<CommentThreadResult>> getThreadHandler,
        ICommandHandler<CreateCommentCommand, ApplicationResult<CommentResult>> createCommentHandler)
    {
        this.getSummaryHandler = getSummaryHandler;
        this.getThreadHandler = getThreadHandler;
        this.createCommentHandler = createCommentHandler;
    }

    [HttpGet("{targetType}/{targetId}/summary")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataShort)]
    [ProducesResponseType(typeof(CommentSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummaryAsync(
        [FromRoute] string targetType,
        [FromRoute] string targetId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<CommentSummaryResult> result = await this.getSummaryHandler.HandleAsync(
            new GetCommentSummaryQuery(
                targetType.ToCommentTargetType(),
                targetId,
                this.HttpContext.UserCanSeeNonVisibleInPublicView()),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{targetType}/{targetId}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataShort)]
    [ProducesResponseType(typeof(CommentThreadDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThreadAsync(
        [FromRoute] string targetType,
        [FromRoute] string targetId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<CommentThreadResult> result = await this.getThreadHandler.HandleAsync(
            new GetCommentThreadQuery(
                targetType.ToCommentTargetType(),
                targetId,
                this.HttpContext.UserCanSeeNonVisibleInPublicView()),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost]
    [Authorize(Roles = AuthorizationRoleGroups.ModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("comment.create", "Comment")]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? authorUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(authorUserId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<CommentResult> result = await this.createCommentHandler.HandleAsync(
            new CreateCommentCommand(authorUserId, request.ToApplication()),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
