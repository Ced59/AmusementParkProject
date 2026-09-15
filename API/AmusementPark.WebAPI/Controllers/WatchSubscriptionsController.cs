using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Watchlists;
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
[Route("me/watch-subscriptions")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class WatchSubscriptionsController : ControllerBase
{
    private readonly IQueryHandler<ListMyWatchSubscriptionsQuery,
        ApplicationResult<IReadOnlyCollection<WatchSubscriptionResult>>> listHandler;
    private readonly ICommandHandler<CreateWatchSubscriptionCommand,
        ApplicationResult<WatchSubscriptionResult>> createHandler;
    private readonly ICommandHandler<UpdateWatchSubscriptionCommand,
        ApplicationResult<WatchSubscriptionResult>> updateHandler;
    private readonly ICommandHandler<SetWatchSubscriptionPausedCommand,
        ApplicationResult<WatchSubscriptionResult>> pauseHandler;
    private readonly ICommandHandler<DeleteWatchSubscriptionCommand, ApplicationResult> deleteHandler;

    public WatchSubscriptionsController(
        IQueryHandler<ListMyWatchSubscriptionsQuery,
            ApplicationResult<IReadOnlyCollection<WatchSubscriptionResult>>> listHandler,
        ICommandHandler<CreateWatchSubscriptionCommand,
            ApplicationResult<WatchSubscriptionResult>> createHandler,
        ICommandHandler<UpdateWatchSubscriptionCommand,
            ApplicationResult<WatchSubscriptionResult>> updateHandler,
        ICommandHandler<SetWatchSubscriptionPausedCommand,
            ApplicationResult<WatchSubscriptionResult>> pauseHandler,
        ICommandHandler<DeleteWatchSubscriptionCommand, ApplicationResult> deleteHandler)
    {
        this.listHandler = listHandler;
        this.createHandler = createHandler;
        this.updateHandler = updateHandler;
        this.pauseHandler = pauseHandler;
        this.deleteHandler = deleteHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<WatchSubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] string? targetType = null,
        [FromQuery] string? targetId = null,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        CollectionTargetType? parsedTargetType = null;
        if (!string.IsNullOrWhiteSpace(targetType))
        {
            if (!WatchSubscriptionHttpMapper.TryParseTargetType(targetType, out CollectionTargetType parsed))
            {
                return this.BadRequest();
            }

            parsedTargetType = parsed;
        }

        if (!string.IsNullOrWhiteSpace(targetId) && !parsedTargetType.HasValue)
        {
            return this.BadRequest();
        }

        ApplicationResult<IReadOnlyCollection<WatchSubscriptionResult>> result =
            await this.listHandler.HandleAsync(
                new ListMyWatchSubscriptionsQuery(userId, parsedTargetType, targetId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.Select(static subscription => subscription.ToHttp()).ToArray())
            : this.ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(WatchSubscriptionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] WatchSubscriptionWriteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!request.TryToApplication(out WatchSubscriptionPreferenceInput? input) || input is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<WatchSubscriptionResult> result = await this.createHandler.HandleAsync(
            new CreateWatchSubscriptionCommand(userId, input),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPatch("{subscriptionId}")]
    [ProducesResponseType(typeof(WatchSubscriptionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] string subscriptionId,
        [FromBody] WatchSubscriptionUpdateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!request.TryToApplication(out WatchSubscriptionSettingsInput? input) || input is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<WatchSubscriptionResult> result = await this.updateHandler.HandleAsync(
            new UpdateWatchSubscriptionCommand(userId, subscriptionId, request.ExpectedVersion, input),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("{subscriptionId}/pause")]
    [ProducesResponseType(typeof(WatchSubscriptionDto), StatusCodes.Status200OK)]
    public Task<IActionResult> PauseAsync(
        [FromRoute] string subscriptionId,
        [FromBody] VersionedMutationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.SetPausedAsync(subscriptionId, request.ExpectedVersion, true, cancellationToken);
    }

    [HttpPost("{subscriptionId}/resume")]
    [ProducesResponseType(typeof(WatchSubscriptionDto), StatusCodes.Status200OK)]
    public Task<IActionResult> ResumeAsync(
        [FromRoute] string subscriptionId,
        [FromBody] VersionedMutationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.SetPausedAsync(subscriptionId, request.ExpectedVersion, false, cancellationToken);
    }

    [HttpDelete("{subscriptionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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
            new DeleteWatchSubscriptionCommand(userId, subscriptionId, expectedVersion),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }

    private async Task<IActionResult> SetPausedAsync(
        string subscriptionId,
        long expectedVersion,
        bool paused,
        CancellationToken cancellationToken)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<WatchSubscriptionResult> result = await this.pauseHandler.HandleAsync(
            new SetWatchSubscriptionPausedCommand(userId, subscriptionId, expectedVersion, paused),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
