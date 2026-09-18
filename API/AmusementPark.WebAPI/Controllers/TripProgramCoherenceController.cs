using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Trips;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/trips/{tripPlanId}/coherence")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TripProgramCoherenceController : ControllerBase
{
    private readonly IQueryHandler<GetTripProgramCoherenceQuery,
        ApplicationResult<TripProgramCoherenceResult>> handler;

    public TripProgramCoherenceController(
        IQueryHandler<GetTripProgramCoherenceQuery,
            ApplicationResult<TripProgramCoherenceResult>> handler)
    {
        this.handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TripProgramCoherenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string tripPlanId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<TripProgramCoherenceResult> result = await this.handler.HandleAsync(
            new GetTripProgramCoherenceQuery(userId, tripPlanId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
