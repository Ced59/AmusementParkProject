using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Trips;
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
[Route("admin/trip-pilot")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminTripPilotController : ControllerBase
{
    private readonly IQueryHandler<GetTripPilotMetricsQuery,
        ApplicationResult<TripPilotMetricsResult>> handler;

    public AdminTripPilotController(
        IQueryHandler<GetTripPilotMetricsQuery,
            ApplicationResult<TripPilotMetricsResult>> handler)
    {
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    [HttpGet("metrics")]
    [EnableRateLimiting(RateLimitPolicyNames.FactualEventAdministration)]
    [ProducesResponseType(typeof(TripPilotMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<TripPilotMetricsResult> result = await this.handler.HandleAsync(
            new GetTripPilotMetricsQuery(),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
