using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Models;
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
[Route("me/trips/{tripPlanId}/preference-summary")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TripPreferenceSummaryController : ControllerBase
{
    private readonly IQueryHandler<GetTripPreferenceSummaryQuery,
        ApplicationResult<TripPreferenceSummaryResult>> getHandler;
    private readonly ICommandHandler<SetTripItemDecisionCommand,
        ApplicationResult<TripPreferenceSummaryResult>> setDecisionHandler;

    public TripPreferenceSummaryController(
        IQueryHandler<GetTripPreferenceSummaryQuery,
            ApplicationResult<TripPreferenceSummaryResult>> getHandler,
        ICommandHandler<SetTripItemDecisionCommand,
            ApplicationResult<TripPreferenceSummaryResult>> setDecisionHandler)
    {
        this.getHandler = getHandler;
        this.setDecisionHandler = setDecisionHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TripPreferenceSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string tripPlanId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<TripPreferenceSummaryResult> result = await this.getHandler.HandleAsync(
            new GetTripPreferenceSummaryQuery(userId, tripPlanId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPut("{parkItemId}/decision")]
    [ProducesResponseType(typeof(TripPreferenceSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetDecisionAsync(
        [FromRoute] string tripPlanId,
        [FromRoute] string parkItemId,
        [FromBody] SetTripItemDecisionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!request.TryToApplication(parkItemId, out TripItemDecisionInput? decision)
            || decision is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<TripPreferenceSummaryResult> result = await this.setDecisionHandler.HandleAsync(
            new SetTripItemDecisionCommand(
                userId,
                tripPlanId,
                request.ExpectedPlanVersion,
                decision),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
