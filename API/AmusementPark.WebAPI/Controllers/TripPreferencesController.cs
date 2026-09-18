using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;
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
[Route("me/trips/{tripPlanId}")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TripPreferencesController : ControllerBase
{
    private readonly IQueryHandler<GetMyTripPreferencesQuery,
        ApplicationResult<TripPreferenceBoardResult>> getHandler;
    private readonly ICommandHandler<SetTripItemPreferenceCommand,
        ApplicationResult<TripPreferenceBoardResult>> setHandler;
    private readonly ICommandHandler<BulkSetTripItemPreferencesCommand,
        ApplicationResult<TripPreferenceBoardResult>> bulkHandler;

    public TripPreferencesController(
        IQueryHandler<GetMyTripPreferencesQuery,
            ApplicationResult<TripPreferenceBoardResult>> getHandler,
        ICommandHandler<SetTripItemPreferenceCommand,
            ApplicationResult<TripPreferenceBoardResult>> setHandler,
        ICommandHandler<BulkSetTripItemPreferencesCommand,
            ApplicationResult<TripPreferenceBoardResult>> bulkHandler)
    {
        this.getHandler = getHandler;
        this.setHandler = setHandler;
        this.bulkHandler = bulkHandler;
    }

    [HttpGet("preferences")]
    [ProducesResponseType(typeof(TripPreferenceBoardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string tripPlanId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<TripPreferenceBoardResult> result = await this.getHandler.HandleAsync(
            new GetMyTripPreferencesQuery(userId, tripPlanId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPut("preferences/{parkItemId}")]
    [ProducesResponseType(typeof(TripPreferenceBoardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetAsync(
        [FromRoute] string tripPlanId,
        [FromRoute] string parkItemId,
        [FromBody] SetTripItemPreferenceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!request.TryToApplication(parkItemId, out TripItemPreferenceInput? input)
            || input is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<TripPreferenceBoardResult> result = await this.setHandler.HandleAsync(
            new SetTripItemPreferenceCommand(
                userId,
                tripPlanId,
                request.ExpectedPlanVersion,
                input),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("preferences:batch")]
    [ProducesResponseType(typeof(TripPreferenceBoardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetBatchAsync(
        [FromRoute] string tripPlanId,
        [FromBody] BulkSetTripItemPreferencesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (request.Preferences is null
            || request.Preferences.Count is < 1 or > TripItemPreference.MaximumBatchSize)
        {
            return this.BadRequest();
        }

        List<TripItemPreferenceInput> inputs = new(request.Preferences.Count);
        foreach (BulkTripItemPreferenceRequestDto preference in request.Preferences)
        {
            if (!preference.TryToApplication(out TripItemPreferenceInput? input) || input is null)
            {
                return this.BadRequest();
            }

            inputs.Add(input);
        }

        ApplicationResult<TripPreferenceBoardResult> result = await this.bulkHandler.HandleAsync(
            new BulkSetTripItemPreferencesCommand(
                userId,
                tripPlanId,
                request.ExpectedPlanVersion,
                inputs),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
