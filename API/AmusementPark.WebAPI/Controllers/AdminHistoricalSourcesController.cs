using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.History;
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
[Route("admin/history/sources")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminHistoricalSourcesController : ControllerBase
{
    private readonly ICommandHandler<
        SaveHistoricalSourceCommand,
        ApplicationResult<HistoricalEditorialMutationResult>> saveHandler;
    private readonly ICommandHandler<
        AdvanceHistoricalEditorialResourceCommand,
        ApplicationResult<HistoricalEditorialMutationResult>> advanceHandler;
    private readonly ICommandHandler<
        RetractHistoricalEditorialResourceCommand,
        ApplicationResult<HistoricalEditorialMutationResult>> retractHandler;

    public AdminHistoricalSourcesController(
        ICommandHandler<
            SaveHistoricalSourceCommand,
            ApplicationResult<HistoricalEditorialMutationResult>> saveHandler,
        ICommandHandler<
            AdvanceHistoricalEditorialResourceCommand,
            ApplicationResult<HistoricalEditorialMutationResult>> advanceHandler,
        ICommandHandler<
            RetractHistoricalEditorialResourceCommand,
            ApplicationResult<HistoricalEditorialMutationResult>> retractHandler)
    {
        this.saveHandler = saveHandler;
        this.advanceHandler = advanceHandler;
        this.retractHandler = retractHandler;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalEditorialAdministration)]
    [AdminAudit("history.source.create", "HistoricalSource", StaticTargetId = "new")]
    public Task<IActionResult> CreateAsync(
        [FromBody] SaveHistoricalSourceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.SaveAsync(null, request, cancellationToken);
    }

    [HttpPatch("{sourceId}")]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalEditorialAdministration)]
    [AdminAudit("history.source.update", "HistoricalSource", TargetIdRouteKey = "sourceId")]
    public Task<IActionResult> UpdateAsync(
        [FromRoute] string sourceId,
        [FromBody] SaveHistoricalSourceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return Guid.TryParse(sourceId, out Guid parsedSourceId)
            ? this.SaveAsync(parsedSourceId, request, cancellationToken)
            : Task.FromResult<IActionResult>(this.BadRequest());
    }

    [HttpPost("{sourceId}/review")]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalEditorialAdministration)]
    [AdminAudit("history.source.review", "HistoricalSource", TargetIdRouteKey = "sourceId")]
    public Task<IActionResult> ReviewAsync(
        [FromRoute] string sourceId,
        [FromBody] ReviewHistoricalEditorialResourceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.ChangeStateAsync(sourceId, request, false, cancellationToken);
    }

    [HttpPost("{sourceId}/retract")]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalEditorialAdministration)]
    [AdminAudit("history.source.retract", "HistoricalSource", TargetIdRouteKey = "sourceId")]
    public Task<IActionResult> RetractAsync(
        [FromRoute] string sourceId,
        [FromBody] ReviewHistoricalEditorialResourceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.ChangeStateAsync(sourceId, request, true, cancellationToken);
    }

    private async Task<IActionResult> SaveAsync(
        Guid? sourceId,
        SaveHistoricalSourceRequestDto request,
        CancellationToken cancellationToken)
    {
        string? actorUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(actorUserId)
            || !request.TryToCommand(sourceId, actorUserId, out SaveHistoricalSourceCommand? command)
            || command is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<HistoricalEditorialMutationResult> result =
            await this.saveHandler.HandleAsync(command, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    private async Task<IActionResult> ChangeStateAsync(
        string sourceId,
        ReviewHistoricalEditorialResourceRequestDto request,
        bool retract,
        CancellationToken cancellationToken)
    {
        string? actorUserId = this.User.GetUserId();
        if (!Guid.TryParse(sourceId, out Guid resourceId)
            || string.IsNullOrWhiteSpace(actorUserId)
            || request.ExpectedRevision < 1)
        {
            return this.BadRequest();
        }

        ApplicationResult<HistoricalEditorialMutationResult> result = retract
            ? await this.retractHandler.HandleAsync(
                new RetractHistoricalEditorialResourceCommand(
                    HistoricalReviewResourceType.Source,
                    resourceId,
                    request.ExpectedRevision,
                    actorUserId,
                    request.ReviewNote),
                cancellationToken)
            : await this.advanceHandler.HandleAsync(
                new AdvanceHistoricalEditorialResourceCommand(
                    HistoricalReviewResourceType.Source,
                    resourceId,
                    request.ExpectedRevision,
                    actorUserId,
                    request.ReviewNote),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
