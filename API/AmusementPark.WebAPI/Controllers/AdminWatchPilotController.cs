using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Watchlists;
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
[Route("admin/watch-pilot")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminWatchPilotController : ControllerBase
{
    private readonly IQueryHandler<GetWatchPilotMetricsQuery,
        ApplicationResult<WatchPilotMetricsResult>> handler;

    public AdminWatchPilotController(
        IQueryHandler<GetWatchPilotMetricsQuery,
            ApplicationResult<WatchPilotMetricsResult>> handler)
    {
        this.handler = handler;
    }

    [HttpGet("metrics")]
    [EnableRateLimiting(RateLimitPolicyNames.FactualEventAdministration)]
    [ProducesResponseType(typeof(WatchPilotMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetricsAsync(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<WatchPilotMetricsResult> result = await this.handler.HandleAsync(
            new GetWatchPilotMetricsQuery(fromUtc, toUtc),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
