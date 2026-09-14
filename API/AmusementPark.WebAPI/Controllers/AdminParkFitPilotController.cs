using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.ParkFit;
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
[Route("admin/park-fit/pilot")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminParkFitPilotController : ControllerBase
{
    private readonly IQueryHandler<
        GetParkFitPilotMetricsQuery,
        ApplicationResult<ParkFitPilotMetricsResult>> metricsHandler;

    public AdminParkFitPilotController(
        IQueryHandler<
            GetParkFitPilotMetricsQuery,
            ApplicationResult<ParkFitPilotMetricsResult>> metricsHandler)
    {
        this.metricsHandler = metricsHandler;
    }

    [HttpGet("metrics")]
    [EnableRateLimiting(RateLimitPolicyNames.ParkFitAdministration)]
    [ProducesResponseType(typeof(ParkFitPilotMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetricsAsync(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkFitPilotMetricsResult> result =
            await this.metricsHandler.HandleAsync(
                new GetParkFitPilotMetricsQuery(fromUtc, toUtc),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
