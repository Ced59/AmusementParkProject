using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Watchlists;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/live-alerts")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class LiveAlertsController : ControllerBase
{
    private readonly IQueryHandler<GetMyLiveAlertsQuery, ApplicationResult<LiveAlertDashboardResult>> getHandler;
    private readonly ICommandHandler<CreateLiveAlertCommand, ApplicationResult<LiveAlertSubscriptionResult>> createHandler;
    private readonly ICommandHandler<DeleteLiveAlertCommand, ApplicationResult> deleteHandler;
    private readonly ICommandHandler<MutateLiveAlertNotificationCommand, ApplicationResult> notificationHandler;

    public LiveAlertsController(
        IQueryHandler<GetMyLiveAlertsQuery, ApplicationResult<LiveAlertDashboardResult>> getHandler,
        ICommandHandler<CreateLiveAlertCommand, ApplicationResult<LiveAlertSubscriptionResult>> createHandler,
        ICommandHandler<DeleteLiveAlertCommand, ApplicationResult> deleteHandler,
        ICommandHandler<MutateLiveAlertNotificationCommand, ApplicationResult> notificationHandler)
    {
        this.getHandler = getHandler;
        this.createHandler = createHandler;
        this.deleteHandler = deleteHandler;
        this.notificationHandler = notificationHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] string? targetId = null,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<LiveAlertDashboardResult> result = await this.getHandler.HandleAsync(
            new GetMyLiveAlertsQuery(userId, targetId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateLiveAlertRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!request.TryToApplication(out LiveAlertPreferenceInput? input) || input is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<LiveAlertSubscriptionResult> result = await this.createHandler.HandleAsync(
            new CreateLiveAlertCommand(userId, input),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpDelete("{subscriptionId}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string subscriptionId,
        [FromQuery] long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult result = await this.deleteHandler.HandleAsync(
            new DeleteLiveAlertCommand(userId, subscriptionId, expectedVersion),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }

    [HttpPost("notifications/{notificationId}/read")]
    public Task<IActionResult> MarkReadAsync(
        [FromRoute] string notificationId,
        [FromBody] VersionedMutationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.MutateNotificationAsync(
            notificationId,
            request.ExpectedVersion,
            LiveAlertNotificationMutation.Read,
            cancellationToken);
    }

    [HttpPost("notifications/{notificationId}/dismiss")]
    public Task<IActionResult> DismissAsync(
        [FromRoute] string notificationId,
        [FromBody] VersionedMutationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.MutateNotificationAsync(
            notificationId,
            request.ExpectedVersion,
            LiveAlertNotificationMutation.Dismiss,
            cancellationToken);
    }

    private async Task<IActionResult> MutateNotificationAsync(
        string notificationId,
        long expectedVersion,
        LiveAlertNotificationMutation mutation,
        CancellationToken cancellationToken)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult result = await this.notificationHandler.HandleAsync(
            new MutateLiveAlertNotificationCommand(userId, notificationId, expectedVersion, mutation),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }
}
