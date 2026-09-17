using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
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
[Route("me/notification-preferences")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class NotificationEmailPreferencesController : ControllerBase
{
    private readonly IQueryHandler<GetMyNotificationEmailPreferenceQuery,
        ApplicationResult<NotificationEmailPreferenceResult>> getHandler;
    private readonly ICommandHandler<UpdateMyNotificationEmailPreferenceCommand,
        ApplicationResult<NotificationEmailPreferenceResult>> updateHandler;

    public NotificationEmailPreferencesController(
        IQueryHandler<GetMyNotificationEmailPreferenceQuery,
            ApplicationResult<NotificationEmailPreferenceResult>> getHandler,
        ICommandHandler<UpdateMyNotificationEmailPreferenceCommand,
            ApplicationResult<NotificationEmailPreferenceResult>> updateHandler)
    {
        this.getHandler = getHandler;
        this.updateHandler = updateHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(NotificationEmailPreferenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<NotificationEmailPreferenceResult> result =
            await this.getHandler.HandleAsync(
                new GetMyNotificationEmailPreferenceQuery(userId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(NotificationEmailPreferenceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        [FromBody] NotificationEmailPreferenceUpdateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<NotificationEmailPreferenceResult> result =
            await this.updateHandler.HandleAsync(
                new UpdateMyNotificationEmailPreferenceCommand(userId, request.ToApplication()),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
