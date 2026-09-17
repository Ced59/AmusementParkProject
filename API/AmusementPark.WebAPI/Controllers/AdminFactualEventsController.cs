using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.FactualEvents.Commands;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Queries;
using AmusementPark.Application.Features.FactualEvents.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.FactualEvents;
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
[Route("admin/factual-events")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class AdminFactualEventsController : ControllerBase
{
    private readonly IQueryHandler<GetFactualChangeEventsQuery,
        ApplicationResult<PagedResult<FactualChangeEventAdminResult>>> queryHandler;
    private readonly ICommandHandler<VerifyFactualChangeEventCommand, ApplicationResult>
        verifyHandler;
    private readonly ICommandHandler<PublishFactualChangeEventCommand, ApplicationResult>
        publishHandler;
    private readonly ICommandHandler<CorrectFactualChangeEventCommand, ApplicationResult>
        correctHandler;
    private readonly ICommandHandler<RetractFactualChangeEventCommand, ApplicationResult>
        retractHandler;

    public AdminFactualEventsController(
        IQueryHandler<GetFactualChangeEventsQuery,
            ApplicationResult<PagedResult<FactualChangeEventAdminResult>>> queryHandler,
        ICommandHandler<VerifyFactualChangeEventCommand, ApplicationResult> verifyHandler,
        ICommandHandler<PublishFactualChangeEventCommand, ApplicationResult> publishHandler,
        ICommandHandler<CorrectFactualChangeEventCommand, ApplicationResult> correctHandler,
        ICommandHandler<RetractFactualChangeEventCommand, ApplicationResult> retractHandler)
    {
        this.queryHandler = queryHandler ?? throw new ArgumentNullException(nameof(queryHandler));
        this.verifyHandler = verifyHandler ?? throw new ArgumentNullException(nameof(verifyHandler));
        this.publishHandler = publishHandler ?? throw new ArgumentNullException(nameof(publishHandler));
        this.correctHandler = correctHandler ?? throw new ArgumentNullException(nameof(correctHandler));
        this.retractHandler = retractHandler ?? throw new ArgumentNullException(nameof(retractHandler));
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(PagedResponseDto<FactualChangeEventAdminDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] FactualChangeEventSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!request.TryToCriteria(out FactualChangeEventSearchCriteria? criteria)
            || criteria is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<PagedResult<FactualChangeEventAdminResult>> result =
            await this.queryHandler.HandleAsync(
                new GetFactualChangeEventsQuery(criteria),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToPagedResponse(static factualEvent => factualEvent.ToHttp()))
            : this.ToActionResult(result);
    }

    [HttpPost("{eventId}/verify")]
    [EnableRateLimiting(RateLimitPolicyNames.FactualEventAdministration)]
    [AdminAudit("factual-event.verify", "FactualChangeEvent", TargetIdRouteKey = "eventId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> VerifyAsync(
        [FromRoute] string eventId,
        [FromBody] FactualChangeEventMutationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.verifyHandler.HandleAsync(
            new VerifyFactualChangeEventCommand(eventId, request.ExpectedVersion),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }

    [HttpPost("{eventId}/publish")]
    [EnableRateLimiting(RateLimitPolicyNames.FactualEventAdministration)]
    [AdminAudit("factual-event.publish", "FactualChangeEvent", TargetIdRouteKey = "eventId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PublishAsync(
        [FromRoute] string eventId,
        [FromBody] FactualChangeEventMutationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.publishHandler.HandleAsync(
            new PublishFactualChangeEventCommand(eventId, request.ExpectedVersion),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }

    [HttpPost("{eventId}/correct")]
    [EnableRateLimiting(RateLimitPolicyNames.FactualEventAdministration)]
    [AdminAudit("factual-event.correct", "FactualChangeEvent", TargetIdRouteKey = "eventId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CorrectAsync(
        [FromRoute] string eventId,
        [FromBody] CorrectFactualChangeEventRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.correctHandler.HandleAsync(
            new CorrectFactualChangeEventCommand(
                eventId,
                request.ExpectedVersion,
                request.SupersedingEventId),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }

    [HttpPost("{eventId}/retract")]
    [EnableRateLimiting(RateLimitPolicyNames.FactualEventAdministration)]
    [AdminAudit("factual-event.retract", "FactualChangeEvent", TargetIdRouteKey = "eventId")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RetractAsync(
        [FromRoute] string eventId,
        [FromBody] RetractFactualChangeEventRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.retractHandler.HandleAsync(
            new RetractFactualChangeEventCommand(
                eventId,
                request.ExpectedVersion,
                request.ReasonCode),
            cancellationToken);
        return result.IsSuccess ? this.NoContent() : this.ToActionResult(result);
    }
}
