using System.Globalization;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
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
[Route("me/trips/{tripPlanId}/passport-transition")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TripPassportTransitionsController : ControllerBase
{
    private readonly IQueryHandler<GetTripPassportTransitionQuery,
        ApplicationResult<TripPassportTransitionResult>> getHandler;
    private readonly ICommandHandler<ConfirmTripPassportTransitionCommand,
        ApplicationResult<ConfirmTripPassportTransitionResult>> confirmHandler;

    public TripPassportTransitionsController(
        IQueryHandler<GetTripPassportTransitionQuery,
            ApplicationResult<TripPassportTransitionResult>> getHandler,
        ICommandHandler<ConfirmTripPassportTransitionCommand,
            ApplicationResult<ConfirmTripPassportTransitionResult>> confirmHandler)
    {
        this.getHandler = getHandler ?? throw new ArgumentNullException(nameof(getHandler));
        this.confirmHandler = confirmHandler
            ?? throw new ArgumentNullException(nameof(confirmHandler));
    }

    [HttpGet]
    [ProducesResponseType(typeof(TripPassportTransitionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string tripPlanId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<TripPassportTransitionResult> result =
            await this.getHandler.HandleAsync(
                new GetTripPassportTransitionQuery(userId, tripPlanId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("days/{localDate}/confirm")]
    [ProducesResponseType(typeof(ConfirmTripPassportTransitionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmAsync(
        [FromRoute] string tripPlanId,
        [FromRoute] string localDate,
        [FromBody] ConfirmTripPassportTransitionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!DateOnly.TryParseExact(
            localDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly parsedDate)
            || request.ParkItemIds is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<ConfirmTripPassportTransitionResult> result =
            await this.confirmHandler.HandleAsync(
                new ConfirmTripPassportTransitionCommand(
                    userId,
                    tripPlanId,
                    parsedDate,
                    request.ParkItemIds),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
