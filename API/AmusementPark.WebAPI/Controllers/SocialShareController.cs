using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialShare.Commands;
using AmusementPark.Application.Features.SocialShare.Contracts;
using AmusementPark.Application.Features.SocialShare.Queries;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.SocialShare;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("social-share")]
public sealed class SocialShareController : ControllerBase
{
    private readonly ICommandHandler<CaptureSocialShareEventCommand, ApplicationResult<SocialShareEventCaptureResult>> captureEventCommandHandler;
    private readonly IQueryHandler<GetSocialShareStatsQuery, ApplicationResult<SocialShareStatsResult>> getStatsQueryHandler;

    public SocialShareController(
        ICommandHandler<CaptureSocialShareEventCommand, ApplicationResult<SocialShareEventCaptureResult>> captureEventCommandHandler,
        IQueryHandler<GetSocialShareStatsQuery, ApplicationResult<SocialShareStatsResult>> getStatsQueryHandler)
    {
        this.captureEventCommandHandler = captureEventCommandHandler;
        this.getStatsQueryHandler = getStatsQueryHandler;
    }

    [HttpPost("events")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicyNames.SocialShareEvents)]
    [ProducesResponseType(typeof(SocialShareEventResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CaptureEventAsync([FromBody] SocialShareEventRequestDto? request, CancellationToken cancellationToken = default)
    {
        SocialShareEventRequestDto safeRequest = request ?? new SocialShareEventRequestDto();
        string? userId = this.User?.Identity?.IsAuthenticated == true ? this.User.GetUserId() : null;
        ApplicationResult<SocialShareEventCaptureResult> result = await this.captureEventCommandHandler.HandleAsync(
            new CaptureSocialShareEventCommand(safeRequest.ToApplication(userId)),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("/admin/social-share/stats")]
    [RequireActivatedUnblockedUser]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [ProducesResponseType(typeof(SocialShareStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatsAsync([FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null, CancellationToken cancellationToken = default)
    {
        SocialShareStatsCriteria criteria = new SocialShareStatsCriteria(fromUtc, toUtc);
        ApplicationResult<SocialShareStatsResult> result = await this.getStatsQueryHandler.HandleAsync(new GetSocialShareStatsQuery(criteria), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
