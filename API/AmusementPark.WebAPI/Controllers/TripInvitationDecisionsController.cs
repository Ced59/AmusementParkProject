using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Trips;
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
[Route("me/trip-invitations")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TripInvitationDecisionsController : ControllerBase
{
    private readonly ICommandHandler<AcceptTripInvitationCommand,
        ApplicationResult<TripInvitationDecisionResult>> acceptHandler;
    private readonly ICommandHandler<DeclineTripInvitationCommand,
        ApplicationResult<TripInvitationDecisionResult>> declineHandler;

    public TripInvitationDecisionsController(
        ICommandHandler<AcceptTripInvitationCommand,
            ApplicationResult<TripInvitationDecisionResult>> acceptHandler,
        ICommandHandler<DeclineTripInvitationCommand,
            ApplicationResult<TripInvitationDecisionResult>> declineHandler)
    {
        this.acceptHandler = acceptHandler;
        this.declineHandler = declineHandler;
    }

    [HttpPost("accept")]
    [EnableRateLimiting(RateLimitPolicyNames.TripInvitationMutations)]
    [ProducesResponseType(typeof(TripInvitationDecisionDto), StatusCodes.Status200OK)]
    public Task<IActionResult> AcceptAsync(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] TripInvitationDecisionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.DecideAsync(true, idempotencyKey, request, cancellationToken);
    }

    [HttpPost("decline")]
    [EnableRateLimiting(RateLimitPolicyNames.TripInvitationMutations)]
    [ProducesResponseType(typeof(TripInvitationDecisionDto), StatusCodes.Status200OK)]
    public Task<IActionResult> DeclineAsync(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] TripInvitationDecisionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.DecideAsync(false, idempotencyKey, request, cancellationToken);
    }

    private async Task<IActionResult> DecideAsync(
        bool accept,
        string? idempotencyKey,
        TripInvitationDecisionRequestDto request,
        CancellationToken cancellationToken)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey) || string.IsNullOrWhiteSpace(request.Token))
        {
            return this.BadRequest();
        }

        ApplicationResult<TripInvitationDecisionResult> result = accept
            ? await this.acceptHandler.HandleAsync(
                new AcceptTripInvitationCommand(userId, request.Token, idempotencyKey),
                cancellationToken)
            : await this.declineHandler.HandleAsync(
                new DeclineTripInvitationCommand(userId, request.Token, idempotencyKey),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
