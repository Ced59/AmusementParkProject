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
public sealed class AdminHistoricalFactsController : ControllerBase
{
    private readonly ICommandHandler<
        SaveHistoricalFactCommand,
        ApplicationResult<HistoricalEditorialMutationResult>> saveHandler;
    private readonly ICommandHandler<
        AdvanceHistoricalEditorialResourceCommand,
        ApplicationResult<HistoricalEditorialMutationResult>> advanceHandler;
    private readonly ICommandHandler<
        RetractHistoricalEditorialResourceCommand,
        ApplicationResult<HistoricalEditorialMutationResult>> retractHandler;

    public AdminHistoricalFactsController(
        ICommandHandler<
            SaveHistoricalFactCommand,
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

    [HttpPost("parks/{parkId}/facts")]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalEditorialAdministration)]
    [AdminAudit("history.fact.create", "HistoricalFact", StaticTargetId = "new")]
    public Task<IActionResult> CreateAsync(
        [FromRoute] string parkId,
        [FromBody] SaveHistoricalFactRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.SaveAsync(parkId, null, request, cancellationToken);
    }

    [HttpPatch("parks/{parkId}/facts/{factId}")]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalEditorialAdministration)]
    [AdminAudit("history.fact.update", "HistoricalFact", TargetIdRouteKey = "factId")]
    public Task<IActionResult> UpdateAsync(
        [FromRoute] string parkId,
        [FromRoute] string factId,
        [FromBody] SaveHistoricalFactRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return Guid.TryParse(factId, out Guid parsedFactId)
            ? this.SaveAsync(parkId, parsedFactId, request, cancellationToken)
            : Task.FromResult<IActionResult>(this.BadRequest());
    }

    [HttpPost("facts/{factId}/review")]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalEditorialAdministration)]
    [AdminAudit("history.fact.review", "HistoricalFact", TargetIdRouteKey = "factId")]
    public Task<IActionResult> ReviewAsync(
        [FromRoute] string factId,
        [FromBody] ReviewHistoricalEditorialResourceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.ChangeStateAsync(factId, request, false, cancellationToken);
    }

    [HttpPost("facts/{factId}/retract")]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalEditorialAdministration)]
    [AdminAudit("history.fact.retract", "HistoricalFact", TargetIdRouteKey = "factId")]
    public Task<IActionResult> RetractAsync(
        [FromRoute] string factId,
        [FromBody] ReviewHistoricalEditorialResourceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return this.ChangeStateAsync(factId, request, true, cancellationToken);
    }

    private async Task<IActionResult> SaveAsync(
        string parkId,
        Guid? factId,
        SaveHistoricalFactRequestDto request,
        CancellationToken cancellationToken)
    {
        string? actorUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(actorUserId)
            || !request.TryToCommand(
                parkId,
                factId,
                actorUserId,
                out SaveHistoricalFactCommand? command)
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
        string factId,
        ReviewHistoricalEditorialResourceRequestDto request,
        bool retract,
        CancellationToken cancellationToken)
    {
        string? actorUserId = this.User.GetUserId();
        if (!Guid.TryParse(factId, out Guid resourceId)
            || string.IsNullOrWhiteSpace(actorUserId)
            || request.ExpectedRevision < 1)
        {
            return this.BadRequest();
        }

        ApplicationResult<HistoricalEditorialMutationResult> result = retract
            ? await this.retractHandler.HandleAsync(
                new RetractHistoricalEditorialResourceCommand(
                    HistoricalReviewResourceType.Fact,
                    resourceId,
                    request.ExpectedRevision,
                    actorUserId,
                    request.ReviewNote),
                cancellationToken)
            : await this.advanceHandler.HandleAsync(
                new AdvanceHistoricalEditorialResourceCommand(
                    HistoricalReviewResourceType.Fact,
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
