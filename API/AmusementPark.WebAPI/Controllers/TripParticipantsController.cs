using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
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
[Route("me/trips/{tripPlanId}/participants")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TripParticipantsController : ControllerBase
{
    private readonly IQueryHandler<ListTripParticipantsQuery,
        ApplicationResult<TripParticipantListResult>> listHandler;
    private readonly ICommandHandler<ChangeTripParticipantRoleCommand,
        ApplicationResult<TripParticipantListResult>> changeRoleHandler;
    private readonly ICommandHandler<TransferTripOwnershipCommand,
        ApplicationResult<TripParticipantListResult>> transferHandler;
    private readonly ICommandHandler<LeaveTripCommand, ApplicationResult> leaveHandler;

    public TripParticipantsController(
        IQueryHandler<ListTripParticipantsQuery,
            ApplicationResult<TripParticipantListResult>> listHandler,
        ICommandHandler<ChangeTripParticipantRoleCommand,
            ApplicationResult<TripParticipantListResult>> changeRoleHandler,
        ICommandHandler<TransferTripOwnershipCommand,
            ApplicationResult<TripParticipantListResult>> transferHandler,
        ICommandHandler<LeaveTripCommand, ApplicationResult> leaveHandler)
    {
        this.listHandler = listHandler;
        this.changeRoleHandler = changeRoleHandler;
        this.transferHandler = transferHandler;
        this.leaveHandler = leaveHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TripParticipantListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromRoute] string tripPlanId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<TripParticipantListResult> result = await this.listHandler.HandleAsync(
            new ListTripParticipantsQuery(userId, tripPlanId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPatch("{memberId}/role")]
    [ProducesResponseType(typeof(TripParticipantListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeRoleAsync(
        [FromRoute] string tripPlanId,
        [FromRoute] string memberId,
        [FromBody] ChangeTripParticipantRoleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!Enum.TryParse(request.Role?.Trim(), true, out TripDelegatedRole role)
            || !Enum.IsDefined(role))
        {
            return this.BadRequest();
        }

        ApplicationResult<TripParticipantListResult> result = await this.changeRoleHandler.HandleAsync(
            new ChangeTripParticipantRoleCommand(
                userId,
                tripPlanId,
                memberId,
                role,
                request.ExpectedVersion),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("{memberId}/transfer-ownership")]
    [ProducesResponseType(typeof(TripParticipantListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> TransferOwnershipAsync(
        [FromRoute] string tripPlanId,
        [FromRoute] string memberId,
        [FromBody] TransferTripOwnershipRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!Enum.TryParse(request.PreviousOwnerRole?.Trim(), true, out TripDelegatedRole previousRole)
            || !Enum.IsDefined(previousRole))
        {
            return this.BadRequest();
        }

        ApplicationResult<TripParticipantListResult> result = await this.transferHandler.HandleAsync(
            new TransferTripOwnershipCommand(
                userId,
                tripPlanId,
                memberId,
                previousRole,
                request.ExpectedVersion),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LeaveAsync(
        [FromRoute] string tripPlanId,
        [FromQuery] long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult result = await this.leaveHandler.HandleAsync(
            new LeaveTripCommand(userId, tripPlanId, expectedVersion),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }
}
