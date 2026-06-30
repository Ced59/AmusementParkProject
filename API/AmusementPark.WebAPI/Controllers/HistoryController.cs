using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.AdminPublicView;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.History;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("history")]
public sealed class HistoryController : ControllerBase
{
    private readonly IQueryHandler<GetParkHistoryTimelineQuery, ApplicationResult<HistoryTimelineResult>> getParkTimelineHandler;
    private readonly IQueryHandler<GetParkItemHistoryTimelineQuery, ApplicationResult<HistoryTimelineResult>> getParkItemTimelineHandler;
    private readonly IQueryHandler<GetHistoryArticleQuery, ApplicationResult<HistoryArticleResult>> getArticleHandler;
    private readonly IQueryHandler<GetHistoryEventsPageQuery, ApplicationResult<PagedResult<HistoryTimelineEventResult>>> getAdminPageHandler;
    private readonly ICommandHandler<UpsertHistoryEventCommand, ApplicationResult<HistoryEvent>> upsertHandler;
    private readonly ICommandHandler<DeleteHistoryEventCommand, ApplicationResult> deleteHandler;

    public HistoryController(
        IQueryHandler<GetParkHistoryTimelineQuery, ApplicationResult<HistoryTimelineResult>> getParkTimelineHandler,
        IQueryHandler<GetParkItemHistoryTimelineQuery, ApplicationResult<HistoryTimelineResult>> getParkItemTimelineHandler,
        IQueryHandler<GetHistoryArticleQuery, ApplicationResult<HistoryArticleResult>> getArticleHandler,
        IQueryHandler<GetHistoryEventsPageQuery, ApplicationResult<PagedResult<HistoryTimelineEventResult>>> getAdminPageHandler,
        ICommandHandler<UpsertHistoryEventCommand, ApplicationResult<HistoryEvent>> upsertHandler,
        ICommandHandler<DeleteHistoryEventCommand, ApplicationResult> deleteHandler)
    {
        this.getParkTimelineHandler = getParkTimelineHandler;
        this.getParkItemTimelineHandler = getParkItemTimelineHandler;
        this.getArticleHandler = getArticleHandler;
        this.getAdminPageHandler = getAdminPageHandler;
        this.upsertHandler = upsertHandler;
        this.deleteHandler = deleteHandler;
    }

    [HttpGet("parks/{parkId}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [ProducesResponseType(typeof(HistoryTimelineDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParkTimelineAsync(
        [FromRoute] string parkId,
        [FromQuery] bool includeParkItems = false,
        [FromQuery] string[]? parkItemIds = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<HistoryTimelineResult> result = await this.getParkTimelineHandler.HandleAsync(
            new GetParkHistoryTimelineQuery(
                parkId,
                this.UserCanSeeNonVisible(),
                includeParkItems,
                parkItemIds ?? Array.Empty<string>()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("park-items/{parkItemId}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [ProducesResponseType(typeof(HistoryTimelineDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParkItemTimelineAsync([FromRoute] string parkItemId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<HistoryTimelineResult> result = await this.getParkItemTimelineHandler.HandleAsync(
            new GetParkItemHistoryTimelineQuery(parkItemId, this.UserCanSeeNonVisible()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("articles/{eventId}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [ProducesResponseType(typeof(HistoryArticleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetArticleAsync([FromRoute] string eventId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<HistoryArticleResult> result = await this.getArticleHandler.HandleAsync(
            new GetHistoryArticleQuery(eventId, this.UserCanSeeNonVisible()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("/admin/history/events")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(PagedResponseDto<HistoryTimelineEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminEventsAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? entityType = null,
        [FromQuery] string? ownerId = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<PagedResult<HistoryTimelineEventResult>> result = await this.getAdminPageHandler.HandleAsync(
            new GetHistoryEventsPageQuery(
                pagination.ToApplication(),
                ParseEntityType(entityType),
                ownerId,
                search),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHistoryPagedHttp());
    }

    [HttpPost("/admin/history/events")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("history-event.upsert", "HistoryEvent")]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [ProducesResponseType(typeof(HistoryEventDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertEventAsync([FromBody] HistoryEventDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<HistoryEvent> result = await this.upsertHandler.HandleAsync(
            new UpsertHistoryEventCommand(request.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("/admin/history/events/{eventId}")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("history-event.update", "HistoryEvent", TargetIdRouteKey = "eventId")]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [ProducesResponseType(typeof(HistoryEventDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateEventAsync([FromRoute] string eventId, [FromBody] HistoryEventDto request, CancellationToken cancellationToken = default)
    {
        request.Id = eventId;
        ApplicationResult<HistoryEvent> result = await this.upsertHandler.HandleAsync(
            new UpsertHistoryEventCommand(request.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpDelete("/admin/history/events/{eventId}")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("history-event.delete", "HistoryEvent", TargetIdRouteKey = "eventId")]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    public async Task<IActionResult> DeleteEventAsync([FromRoute] string eventId, CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.deleteHandler.HandleAsync(new DeleteHistoryEventCommand(eventId), cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(true);
    }

    private static HistoryEntityType? ParseEntityType(string? value)
    {
        return Enum.TryParse(value, true, out HistoryEntityType entityType) ? entityType : null;
    }

    private bool UserCanSeeNonVisible()
    {
        return this.HttpContext.UserCanSeeNonVisibleInPublicView();
    }
}
