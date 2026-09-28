using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.History;
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
[Route("admin/history/parks/{parkId}/workbench")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminHistoricalWorkbenchController : ControllerBase
{
    private readonly IQueryHandler<
        GetAdminHistoricalParkWorkbenchQuery,
        ApplicationResult<AdminHistoricalParkWorkbenchResult>> queryHandler;
    private readonly IQueryHandler<
        PreviewHistoricalSnapshotImpactQuery,
        ApplicationResult<HistoricalPublicationImpactPreviewResult>> previewHandler;

    public AdminHistoricalWorkbenchController(
        IQueryHandler<
            GetAdminHistoricalParkWorkbenchQuery,
            ApplicationResult<AdminHistoricalParkWorkbenchResult>> queryHandler,
        IQueryHandler<
            PreviewHistoricalSnapshotImpactQuery,
            ApplicationResult<HistoricalPublicationImpactPreviewResult>> previewHandler)
    {
        this.queryHandler = queryHandler;
        this.previewHandler = previewHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminHistoricalParkWorkbenchDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string parkId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<AdminHistoricalParkWorkbenchResult> result =
            await this.queryHandler.HandleAsync(
                new GetAdminHistoricalParkWorkbenchQuery(parkId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpPost("preview")]
    [EnableRateLimiting(RateLimitPolicyNames.HistoricalEditorialAdministration)]
    [ProducesResponseType(typeof(HistoricalPublicationImpactPreviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewAsync(
        [FromRoute] string parkId,
        [FromBody] PreviewHistoricalImpactRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse(
                request.ResourceType,
                true,
                out HistoricalReviewResourceType resourceType)
            || !Enum.IsDefined(resourceType)
            || !Guid.TryParse(request.ResourceId, out Guid resourceId))
        {
            return this.BadRequest();
        }

        ApplicationResult<HistoricalPublicationImpactPreviewResult> result =
            await this.previewHandler.HandleAsync(
                new PreviewHistoricalSnapshotImpactQuery(
                    parkId,
                    resourceType,
                    resourceId,
                    request.Year),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
