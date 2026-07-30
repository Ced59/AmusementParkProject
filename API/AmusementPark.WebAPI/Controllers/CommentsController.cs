using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Commands;
using AmusementPark.Application.Features.Comments.Queries;
using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.WebAPI.AdminPublicView;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Comments;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

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
    private readonly ICommandHandler<UploadCommentImageCommand, ApplicationResult<UploadedImageResult>> uploadCommentImageHandler;
    private readonly ICommandHandler<DeleteCommentDraftImageCommand, ApplicationResult> deleteCommentDraftImageHandler;

    public CommentsController(
        IQueryHandler<GetCommentSummaryQuery, ApplicationResult<CommentSummaryResult>> getSummaryHandler,
        IQueryHandler<GetCommentThreadQuery, ApplicationResult<CommentThreadResult>> getThreadHandler,
        ICommandHandler<CreateCommentCommand, ApplicationResult<CommentResult>> createCommentHandler,
        ICommandHandler<UpdateCommentCommand, ApplicationResult<CommentResult>> updateCommentHandler,
        ICommandHandler<DeleteCommentCommand, ApplicationResult> deleteCommentHandler,
        ICommandHandler<UploadCommentImageCommand, ApplicationResult<UploadedImageResult>> uploadCommentImageHandler,
        ICommandHandler<DeleteCommentDraftImageCommand, ApplicationResult> deleteCommentDraftImageHandler)
    {
        this.getSummaryHandler = getSummaryHandler;
        this.getThreadHandler = getThreadHandler;
        this.createCommentHandler = createCommentHandler;
        this.updateCommentHandler = updateCommentHandler;
        this.deleteCommentHandler = deleteCommentHandler;
        this.uploadCommentImageHandler = uploadCommentImageHandler;
        this.deleteCommentDraftImageHandler = deleteCommentDraftImageHandler;
    }

    [HttpPost("images")]
    [Authorize(Roles = AuthorizationRoleGroups.ModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("comment-image.upload", "Image")]
    [EnableRateLimiting(RateLimitPolicyNames.ImageUploadProcessing)]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CommentImageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UploadImageAsync(
        [FromForm] CommentImageUploadDto request,
        CancellationToken cancellationToken = default)
    {
        string? actorUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return this.Unauthorized();
        }

        if (request.File is null)
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status400BadRequest,
                "No image file provided.",
                "comment.image.invalid");
        }

        await using Stream content = request.File.OpenReadStream();
        FilePayload file = new FilePayload
        {
            FileName = request.File.FileName,
            ContentType = request.File.ContentType,
            Length = request.File.Length,
            Content = content,
        };
        ApplicationResult<UploadedImageResult> result = await this.uploadCommentImageHandler.HandleAsync(
            new UploadCommentImageCommand(actorUserId, file),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new CommentImageDto
        {
            Id = result.Value.Image.Id,
            Url = $"/images/{result.Value.Image.Id}",
            Width = result.Value.Image.Width,
            Height = result.Value.Image.Height,
            SizeInBytes = result.Value.Image.SizeInBytes,
            ContentType = result.Value.Image.ContentType,
        });
    }

    [HttpDelete("images/{imageId}")]
    [Authorize(Roles = AuthorizationRoleGroups.ModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("comment-image.draft.delete", "Image", TargetIdRouteKey = "imageId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteDraftImageAsync(
        [FromRoute] string imageId,
        CancellationToken cancellationToken = default)
    {
        string? actorUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return this.Unauthorized();
        }

        ApplicationResult result = await this.deleteCommentDraftImageHandler.HandleAsync(
            new DeleteCommentDraftImageCommand(actorUserId, imageId),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.NoContent();
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
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
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
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("comment.delete", "Comment")]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string commentId,
        [FromQuery] long? revision = null,
        CancellationToken cancellationToken = default)
    {
        string? actorUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return this.Unauthorized();
        }

        ApplicationResult result = await this.deleteCommentHandler.HandleAsync(
            new DeleteCommentCommand(actorUserId, commentId, revision),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.NoContent();
    }
}
