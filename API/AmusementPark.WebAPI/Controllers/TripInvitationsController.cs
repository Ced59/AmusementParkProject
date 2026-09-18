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
[Route("me/trips/{tripPlanId}/invitations")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TripInvitationsController : ControllerBase
{
    private readonly IQueryHandler<ListTripInvitationsQuery,
        ApplicationResult<TripInvitationListResult>> listHandler;
    private readonly ICommandHandler<CreateTripInvitationCommand,
        ApplicationResult<TripInvitationCreationResult>> createHandler;
    private readonly ICommandHandler<RevokeTripInvitationCommand, ApplicationResult> revokeHandler;

    public TripInvitationsController(
        IQueryHandler<ListTripInvitationsQuery,
            ApplicationResult<TripInvitationListResult>> listHandler,
        ICommandHandler<CreateTripInvitationCommand,
            ApplicationResult<TripInvitationCreationResult>> createHandler,
        ICommandHandler<RevokeTripInvitationCommand, ApplicationResult> revokeHandler)
    {
        this.listHandler = listHandler;
        this.createHandler = createHandler;
        this.revokeHandler = revokeHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TripInvitationListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromRoute] string tripPlanId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<TripInvitationListResult> result =
            await this.listHandler.HandleAsync(
                new ListTripInvitationsQuery(userId, tripPlanId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TripInvitationCreationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(TripInvitationCreationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync(
        [FromRoute] string tripPlanId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreateTripInvitationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey)
            || !request.TryToApplication(out TripInvitationCreateInput? input)
            || input is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<TripInvitationCreationResult> result = await this.createHandler.HandleAsync(
            new CreateTripInvitationCommand(userId, tripPlanId, idempotencyKey, input),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        TripInvitationCreationDto response = result.Value.ToHttp();
        if (result.Value.WasReplayed)
        {
            this.Response.Headers["Idempotency-Replayed"] = "true";
            return this.Ok(response);
        }

        return this.StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpDelete("{invitationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeAsync(
        [FromRoute] string tripPlanId,
        [FromRoute] string invitationId,
        [FromQuery] long expectedVersion,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return this.BadRequest();
        }

        ApplicationResult result = await this.revokeHandler.HandleAsync(
            new RevokeTripInvitationCommand(
                userId,
                tripPlanId,
                invitationId,
                expectedVersion,
                idempotencyKey),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }
}
