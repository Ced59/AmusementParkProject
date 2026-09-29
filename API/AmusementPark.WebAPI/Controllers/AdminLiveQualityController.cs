using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Commands;
using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.LiveData;
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
[Route("admin/live/quality")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminLiveQualityController : ControllerBase
{
    private readonly ICommandHandler<
        ReplayLiveQualityIncidentsCommand,
        ApplicationResult<LiveQualityReplayResult>> replayHandler;

    public AdminLiveQualityController(
        ICommandHandler<
            ReplayLiveQualityIncidentsCommand,
            ApplicationResult<LiveQualityReplayResult>> replayHandler)
    {
        this.replayHandler = replayHandler;
    }

    [HttpPost("quarantine/replay")]
    [EnableRateLimiting(RateLimitPolicyNames.LiveDataAdministration)]
    [AdminAudit("live.quality.replay", "LiveQualityIncident", StaticTargetId = "bounded-batch")]
    [ProducesResponseType(typeof(LiveQualityReplayDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReplayAsync(
        [FromBody] ReplayLiveQualityIncidentsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? administratorUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(administratorUserId))
        {
            return this.BadRequest();
        }

        ApplicationResult<LiveQualityReplayResult> result = await this.replayHandler.HandleAsync(
            new ReplayLiveQualityIncidentsCommand(
                request.MaximumCount,
                administratorUserId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
