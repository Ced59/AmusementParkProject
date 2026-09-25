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
[Route("me/trips/{tripPlanId}/notifications")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TripNotificationsController : ControllerBase
{
    private readonly IQueryHandler<GetTripNotificationStateQuery,
        ApplicationResult<TripNotificationStateResult>> getHandler;
    private readonly ICommandHandler<SetTripNotificationsCommand,
        ApplicationResult<TripNotificationStateResult>> setHandler;
    private readonly ICommandHandler<MarkTripNotificationsReadCommand,
        ApplicationResult<TripNotificationStateResult>> markReadHandler;

    public TripNotificationsController(
        IQueryHandler<GetTripNotificationStateQuery,
            ApplicationResult<TripNotificationStateResult>> getHandler,
        ICommandHandler<SetTripNotificationsCommand,
            ApplicationResult<TripNotificationStateResult>> setHandler,
        ICommandHandler<MarkTripNotificationsReadCommand,
            ApplicationResult<TripNotificationStateResult>> markReadHandler)
    {
        this.getHandler = getHandler ?? throw new ArgumentNullException(nameof(getHandler));
        this.setHandler = setHandler ?? throw new ArgumentNullException(nameof(setHandler));
        this.markReadHandler = markReadHandler ?? throw new ArgumentNullException(nameof(markReadHandler));
    }

    [HttpGet]
    [ProducesResponseType(typeof(TripNotificationStateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string tripPlanId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<TripNotificationStateResult> result = await this.getHandler.HandleAsync(
            new GetTripNotificationStateQuery(userId, tripPlanId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(TripNotificationStateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetAsync(
        [FromRoute] string tripPlanId,
        [FromBody] SetTripNotificationsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<TripNotificationStateResult> result = await this.setHandler.HandleAsync(
            new SetTripNotificationsCommand(
                userId,
                tripPlanId,
                request.Enabled,
                request.ExpectedVersion),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("read")]
    [ProducesResponseType(typeof(TripNotificationStateDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkReadAsync(
        [FromRoute] string tripPlanId,
        [FromBody] MarkTripNotificationsReadRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<TripNotificationStateResult> result =
            await this.markReadHandler.HandleAsync(
                new MarkTripNotificationsReadCommand(
                    userId,
                    tripPlanId,
                    request.ExpectedVersion),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
