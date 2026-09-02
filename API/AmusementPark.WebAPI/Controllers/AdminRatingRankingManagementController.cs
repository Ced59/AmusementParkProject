using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Ratings;
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
[Route("admin/ratings/ranking-management")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[EnableRateLimiting(RateLimitPolicyNames.RatingDiagnostics)]
public sealed class AdminRatingRankingManagementController : ControllerBase
{
    private readonly IQueryHandler<GetRatingRankingAdministrationQuery,
        ApplicationResult<RatingRankingAdministrationResult>> dashboardHandler;
    private readonly IQueryHandler<PreviewRatingRankingPolicyImpactQuery,
        ApplicationResult<RatingRankingPolicyImpactResult>> previewHandler;
    private readonly ICommandHandler<RebuildRatingRankingSnapshotsCommand,
        ApplicationResult<RatingRankingRebuildRequestResult>> rebuildHandler;

    public AdminRatingRankingManagementController(
        IQueryHandler<GetRatingRankingAdministrationQuery,
            ApplicationResult<RatingRankingAdministrationResult>> dashboardHandler,
        IQueryHandler<PreviewRatingRankingPolicyImpactQuery,
            ApplicationResult<RatingRankingPolicyImpactResult>> previewHandler,
        ICommandHandler<RebuildRatingRankingSnapshotsCommand,
            ApplicationResult<RatingRankingRebuildRequestResult>> rebuildHandler)
    {
        this.dashboardHandler = dashboardHandler;
        this.previewHandler = previewHandler;
        this.rebuildHandler = rebuildHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(RatingRankingAdministrationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken = default)
    {
        ApplicationResult<RatingRankingAdministrationResult> result =
            await this.dashboardHandler.HandleAsync(
                new GetRatingRankingAdministrationQuery(),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("preview")]
    [AdminAudit("rating-ranking.policy.preview", "RatingMethodology", StaticTargetId = "candidate")]
    [ProducesResponseType(typeof(RatingRankingPolicyImpactDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewAsync(
        [FromBody] RatingRankingPolicyCandidateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<RatingRankingPolicyImpactResult> result =
            await this.previewHandler.HandleAsync(
                new PreviewRatingRankingPolicyImpactQuery(request.ToApplication()),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("rebuild")]
    [AdminAudit("rating-ranking.snapshots.rebuild", "RatingRankingSnapshot", StaticTargetId = "all")]
    [ProducesResponseType(typeof(RatingRankingRebuildRequestResultDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RebuildAsync(
        [FromBody] RatingRankingRebuildRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<RatingRankingRebuildRequestResult> result =
            await this.rebuildHandler.HandleAsync(
                new RebuildRatingRankingSnapshotsCommand(request.Confirmed),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Accepted(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
