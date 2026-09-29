using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
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
[Route("admin/live/forecast-backtests")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminLiveForecastBacktestsController : ControllerBase
{
    private readonly IQueryHandler<
        GetAdminLiveWaitForecastBacktestQuery,
        ApplicationResult<LiveWaitForecastBacktestResult>> queryHandler;

    public AdminLiveForecastBacktestsController(
        IQueryHandler<
            GetAdminLiveWaitForecastBacktestQuery,
            ApplicationResult<LiveWaitForecastBacktestResult>> queryHandler)
    {
        this.queryHandler = queryHandler;
    }

    [HttpGet("{parkItemId}")]
    [EnableRateLimiting(RateLimitPolicyNames.LiveDataAdministration)]
    [ProducesResponseType(typeof(LiveWaitForecastBacktestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        string parkItemId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<LiveWaitForecastBacktestResult> result =
            await this.queryHandler.HandleAsync(
                new GetAdminLiveWaitForecastBacktestQuery(parkItemId, from, to),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
