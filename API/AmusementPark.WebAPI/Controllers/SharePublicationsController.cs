using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/shares")]
public sealed class SharePublicationsController : ControllerBase
{
    private readonly IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>> previewHandler;
    private readonly ICommandHandler<PublishSharePublicationCommand, ApplicationResult<SharePublicationSettingsResult>> publishHandler;
    private readonly ICommandHandler<RotateShareIdCommand, ApplicationResult<SharePublicationSettingsResult>> rotateHandler;
    private readonly ICommandHandler<RevokeSharePublicationCommand, ApplicationResult<SharePublicationSettingsResult>> revokeHandler;

    public SharePublicationsController(
        IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>> previewHandler,
        ICommandHandler<PublishSharePublicationCommand, ApplicationResult<SharePublicationSettingsResult>> publishHandler,
        ICommandHandler<RotateShareIdCommand, ApplicationResult<SharePublicationSettingsResult>> rotateHandler,
        ICommandHandler<RevokeSharePublicationCommand, ApplicationResult<SharePublicationSettingsResult>> revokeHandler)
    {
        this.previewHandler = previewHandler;
        this.publishHandler = publishHandler;
        this.rotateHandler = rotateHandler;
        this.revokeHandler = revokeHandler;
    }

    [HttpPost("preview")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [EnableRateLimiting(RateLimitPolicyNames.SharePublicationPreviews)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharePublicationPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PreviewAsync(
        [FromBody] SharePublicationPreviewRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!request.TryToApplication(userId, out PreviewSharePublicationQuery? query)
            || query is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<SharePublicationPreviewResult> result =
            await this.previewHandler.HandleAsync(query, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("publish")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [EnableRateLimiting(RateLimitPolicyNames.SharePublicationConfirmations)]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharePublicationSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PublishAsync(
        [FromBody] PublishSharePublicationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!request.TryToApplication(userId, out PublishSharePublicationCommand? command)
            || command is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<SharePublicationSettingsResult> result =
            await this.publishHandler.HandleAsync(command, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToSharingHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("{publicationId}/rotate-link")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [EnableRateLimiting(RateLimitPolicyNames.SharePublicationConfirmations)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharePublicationSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RotateLinkAsync(
        [FromRoute] string publicationId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<SharePublicationSettingsResult> result =
            await this.rotateHandler.HandleAsync(
                new RotateShareIdCommand(userId, publicationId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToSharingHttp())
            : this.ToActionResult(result);
    }

    [HttpDelete("{publicationId}")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [EnableRateLimiting(RateLimitPolicyNames.SharePublicationConfirmations)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharePublicationSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RevokeAsync(
        [FromRoute] string publicationId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<SharePublicationSettingsResult> result =
            await this.revokeHandler.HandleAsync(
                new RevokeSharePublicationCommand(userId, publicationId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToSharingHttp())
            : this.ToActionResult(result);
    }
}
