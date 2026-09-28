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
[Route("admin/history")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminHistoricalRelationsController : ControllerBase
{
    private readonly ICommandHandler<
        SaveHistoricalRelationCommand,
        ApplicationResult<HistoricalEditorialMutationResult>> saveHandler;
    private readonly ICommandHandler<
        AdvanceHistoricalEditorialResourceCommand,
        ApplicationResult<HistoricalEditorialMutationResult>> advanceHandler;
    private readonly ICommandHandler<
        RetractHistoricalEditorialResourceCommand,
        ApplicationResult<HistoricalEditorialMutationResult>> retractHandler;

    public AdminHistoricalRelationsController(
        ICommandHandler<
            SaveHistoricalRelationCommand,
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

    [HttpPost("parks/{parkId}/relations")]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalEditorialAdministration)]
    [AdminAudit("history.relation.create", "HistoricalRelation", StaticTargetId = "new")]
    public Task<IActionResult> CreateAsync(
        [FromRoute] string parkId,
        [FromBody] SaveHistoricalRelationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.SaveAsync(parkId, null, request, cancellationToken);
    }

    [HttpPatch("parks/{parkId}/relations/{relationId}")]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalEditorialAdministration)]
    [AdminAudit("history.relation.update", "HistoricalRelation", TargetIdRouteKey = "relationId")]
    public Task<IActionResult> UpdateAsync(
        [FromRoute] string parkId,
        [FromRoute] string relationId,
        [FromBody] SaveHistoricalRelationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return Guid.TryParse(relationId, out Guid parsedRelationId)
            ? this.SaveAsync(parkId, parsedRelationId, request, cancellationToken)
            : Task.FromResult<IActionResult>(this.BadRequest());
    }

    [HttpPost("relations/{relationId}/review")]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalEditorialAdministration)]
    [AdminAudit("history.relation.review", "HistoricalRelation", TargetIdRouteKey = "relationId")]
    public Task<IActionResult> ReviewAsync(
        [FromRoute] string relationId,
        [FromBody] ReviewHistoricalEditorialResourceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.ChangeStateAsync(relationId, request, false, cancellationToken);
    }

    [HttpPost("relations/{relationId}/retract")]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalEditorialAdministration)]
    [AdminAudit("history.relation.retract", "HistoricalRelation", TargetIdRouteKey = "relationId")]
    public Task<IActionResult> RetractAsync(
        [FromRoute] string relationId,
        [FromBody] ReviewHistoricalEditorialResourceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.ChangeStateAsync(relationId, request, true, cancellationToken);
    }

    private async Task<IActionResult> SaveAsync(
        string parkId,
        Guid? relationId,
        SaveHistoricalRelationRequestDto request,
        CancellationToken cancellationToken)
    {
        string? actorUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(actorUserId)
            || !request.TryToCommand(
                parkId,
                relationId,
                actorUserId,
                out SaveHistoricalRelationCommand? command)
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
        string relationId,
        ReviewHistoricalEditorialResourceRequestDto request,
        bool retract,
        CancellationToken cancellationToken)
    {
        string? actorUserId = this.User.GetUserId();
        if (!Guid.TryParse(relationId, out Guid resourceId)
            || string.IsNullOrWhiteSpace(actorUserId)
            || request.ExpectedRevision < 1)
        {
            return this.BadRequest();
        }

        ApplicationResult<HistoricalEditorialMutationResult> result = retract
            ? await this.retractHandler.HandleAsync(
                new RetractHistoricalEditorialResourceCommand(
                    HistoricalReviewResourceType.Relation,
                    resourceId,
                    request.ExpectedRevision,
                    actorUserId,
                    request.ReviewNote),
                cancellationToken)
            : await this.advanceHandler.HandleAsync(
                new AdvanceHistoricalEditorialResourceCommand(
                    HistoricalReviewResourceType.Relation,
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
