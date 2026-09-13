using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/comparisons/invitations")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProfileComparisonInvitationsController : ControllerBase
{
    private readonly ICommandHandler<CreateProfileComparisonInvitationCommand,
        ApplicationResult<ProfileComparisonInvitationCreationResult>> createHandler;
    private readonly IQueryHandler<GetProfileComparisonInvitationPreviewQuery,
        ApplicationResult<ProfileComparisonInvitationPreviewResult>> previewHandler;
    private readonly ICommandHandler<AcceptProfileComparisonInvitationCommand,
        ApplicationResult<ProfileComparisonInvitationAcceptanceResult>> acceptHandler;
    private readonly SharePublicationRolloutSettings rolloutSettings;

    public ProfileComparisonInvitationsController(
        ICommandHandler<CreateProfileComparisonInvitationCommand,
            ApplicationResult<ProfileComparisonInvitationCreationResult>> createHandler,
        IQueryHandler<GetProfileComparisonInvitationPreviewQuery,
            ApplicationResult<ProfileComparisonInvitationPreviewResult>> previewHandler,
        ICommandHandler<AcceptProfileComparisonInvitationCommand,
            ApplicationResult<ProfileComparisonInvitationAcceptanceResult>> acceptHandler,
        IOptions<SharePublicationRolloutSettings> rolloutSettings)
    {
        this.createHandler = createHandler;
        this.previewHandler = previewHandler;
        this.acceptHandler = acceptHandler;
        this.rolloutSettings = rolloutSettings.Value;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicyNames.SharePublicationConfirmations)]
    [ProducesResponseType(typeof(ProfileComparisonInvitationCreationDto),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateProfileComparisonInvitationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!this.rolloutSettings.Enabled)
        {
            return this.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!request.TryToApplication(userId, out CreateProfileComparisonInvitationCommand? command)
            || command is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<ProfileComparisonInvitationCreationResult> result =
            await this.createHandler.HandleAsync(command, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpGet("{token}/preview")]
    [EnableRateLimiting(RateLimitPolicyNames.SharePublicationPreviews)]
    [ProducesResponseType(typeof(ProfileComparisonInvitationPreviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewAsync(
        [FromRoute] string token,
        CancellationToken cancellationToken = default)
    {
        if (!this.rolloutSettings.Enabled)
        {
            return this.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<ProfileComparisonInvitationPreviewResult> result =
            await this.previewHandler.HandleAsync(
                new GetProfileComparisonInvitationPreviewQuery(userId, token),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("{token}/accept")]
    [EnableRateLimiting(RateLimitPolicyNames.SharePublicationConfirmations)]
    [ProducesResponseType(typeof(ProfileComparisonInvitationAcceptanceDto),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> AcceptAsync(
        [FromRoute] string token,
        CancellationToken cancellationToken = default)
    {
        if (!this.rolloutSettings.Enabled)
        {
            return this.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<ProfileComparisonInvitationAcceptanceResult> result =
            await this.acceptHandler.HandleAsync(
                new AcceptProfileComparisonInvitationCommand(userId, token),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
