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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/notifications")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class UserNotificationsController : ControllerBase
{
    private readonly IQueryHandler<GetMyUserNotificationsQuery,
        ApplicationResult<UserNotificationPageResult>> queryHandler;
    private readonly ICommandHandler<MarkUserNotificationReadCommand, ApplicationResult> readHandler;
    private readonly ICommandHandler<DismissUserNotificationCommand, ApplicationResult> dismissHandler;
    private readonly ICommandHandler<MarkAllUserNotificationsReadCommand, ApplicationResult> readAllHandler;
    private readonly ICommandHandler<DeleteNotificationSourceSubscriptionCommand, ApplicationResult>
        unsubscribeHandler;

    public UserNotificationsController(
        IQueryHandler<GetMyUserNotificationsQuery,
            ApplicationResult<UserNotificationPageResult>> queryHandler,
        ICommandHandler<MarkUserNotificationReadCommand, ApplicationResult> readHandler,
        ICommandHandler<DismissUserNotificationCommand, ApplicationResult> dismissHandler,
        ICommandHandler<MarkAllUserNotificationsReadCommand, ApplicationResult> readAllHandler,
        ICommandHandler<DeleteNotificationSourceSubscriptionCommand, ApplicationResult> unsubscribeHandler)
    {
        this.queryHandler = queryHandler;
        this.readHandler = readHandler;
        this.dismissHandler = dismissHandler;
        this.readAllHandler = readAllHandler;
        this.unsubscribeHandler = unsubscribeHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserNotificationPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] UserNotificationSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!request.TryToApplication(out UserNotificationSearchCriteria? criteria) || criteria is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<UserNotificationPageResult> result = await this.queryHandler.HandleAsync(
            new GetMyUserNotificationsQuery(userId, criteria),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("{notificationId}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkReadAsync(
        [FromRoute] string notificationId,
        [FromBody] VersionedMutationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        return await this.MutateAsync(
            new MarkUserNotificationReadCommand(userId, notificationId, request.ExpectedVersion),
            this.readHandler,
            cancellationToken);
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult result = await this.readAllHandler.HandleAsync(
            new MarkAllUserNotificationsReadCommand(userId),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }

    [HttpPost("{notificationId}/dismiss")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DismissAsync(
        [FromRoute] string notificationId,
        [FromBody] VersionedMutationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        return await this.MutateAsync(
            new DismissUserNotificationCommand(userId, notificationId, request.ExpectedVersion),
            this.dismissHandler,
            cancellationToken);
    }

    [HttpDelete("{notificationId}/subscription")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSourceSubscriptionAsync(
        [FromRoute] string notificationId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult result = await this.unsubscribeHandler.HandleAsync(
            new DeleteNotificationSourceSubscriptionCommand(userId, notificationId),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }

    private async Task<IActionResult> MutateAsync<TCommand>(
        TCommand command,
        ICommandHandler<TCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
        where TCommand : ICommand<ApplicationResult>
    {
        ApplicationResult result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }
}
