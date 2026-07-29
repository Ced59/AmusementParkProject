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
    private readonly ICommandHandler<UpdateCommentCommand, ApplicationResult<CommentResult>> updateCommentHandler;
    private readonly ICommandHandler<DeleteCommentCommand, ApplicationResult> deleteCommentHandler;

    public CommentsController(
        IQueryHandler<GetCommentSummaryQuery, ApplicationResult<CommentSummaryResult>> getSummaryHandler,
        IQueryHandler<GetCommentThreadQuery, ApplicationResult<CommentThreadResult>> getThreadHandler,
        ICommandHandler<CreateCommentCommand, ApplicationResult<CommentResult>> createCommentHandler,
        ICommandHandler<UpdateCommentCommand, ApplicationResult<CommentResult>> updateCommentHandler,
        ICommandHandler<DeleteCommentCommand, ApplicationResult> deleteCommentHandler)
    {
        this.getSummaryHandler = getSummaryHandler;
        this.getThreadHandler = getThreadHandler;
        this.createCommentHandler = createCommentHandler;
        this.updateCommentHandler = updateCommentHandler;
        this.deleteCommentHandler = deleteCommentHandler;
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

        string? actorUserId = this.User.GetUserId();
        return this.Ok(result.Value.ToHttp(
            actorUserId,
            this.User.IsInRole(AuthorizationRoleGroups.Admin)));
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

        string? actorUserId = this.User.GetUserId();
        return this.Ok(result.Value.ToHttp(
            actorUserId,
            this.User.IsInRole(AuthorizationRoleGroups.Admin)));
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

        return this.Ok(result.Value.ToHttp(
            authorUserId,
            this.User.IsInRole(AuthorizationRoleGroups.Admin)));
    }

    [HttpPut("{commentId}")]
    [Authorize(Roles = AuthorizationRoleGroups.ModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("comment.update", "Comment")]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] string commentId,
        [FromBody] UpdateCommentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? actorUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<CommentResult> result = await this.updateCommentHandler.HandleAsync(
            new UpdateCommentCommand(actorUserId, commentId, request.ToApplication()),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp(
            actorUserId,
            this.User.IsInRole(AuthorizationRoleGroups.Admin)));
    }

    [HttpDelete("{commentId}")]
    [Authorize(Roles = AuthorizationRoleGroups.ModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("comment.delete", "Comment")]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string commentId,
        CancellationToken cancellationToken = default)
    {
        string? actorUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return this.Unauthorized();
        }

        ApplicationResult result = await this.deleteCommentHandler.HandleAsync(
            new DeleteCommentCommand(actorUserId, commentId),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.NoContent();
    }
}
