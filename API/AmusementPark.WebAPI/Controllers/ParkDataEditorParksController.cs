using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Parks;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("park-data-editor/parks")]
[Authorize(Policy = AuthorizationPolicyNames.AdminOrParkDataEditorToken)]
[AllowParkDataEditorToken]
[RequireActivatedUnblockedUser]
public sealed class ParkDataEditorParksController : ControllerBase
{
    private readonly IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<ParkListResult>>> getParksPageHandler;
    private readonly IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<ParkListResult>>> searchParksHandler;
    private readonly IQueryHandler<GetParkDataCompletenessScoreQuery, ApplicationResult<DataCompletenessScore>> completenessHandler;

    public ParkDataEditorParksController(
        IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<ParkListResult>>> getParksPageHandler,
        IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<ParkListResult>>> searchParksHandler,
        IQueryHandler<GetParkDataCompletenessScoreQuery, ApplicationResult<DataCompletenessScore>> completenessHandler)
    {
        this.getParksPageHandler = getParksPageHandler;
        this.searchParksHandler = searchParksHandler;
        this.completenessHandler = completenessHandler;
    }

    [HttpGet]
    [AdminAudit("park-data-editor.park-search", "Park", StaticTargetId = "search")]
    [ProducesResponseType(typeof(PagedResponseDto<ParkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? query = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<PagedResult<ParkListResult>> result = string.IsNullOrWhiteSpace(query)
            ? await this.getParksPageHandler.HandleAsync(
                new GetParksPageQuery(
                    pagination.ToApplication(),
                    IncludeHidden: true,
                    ClosedFilter: ClosedEntityFilter.All,
                    SortField: ParkAdminSortField.Name),
                cancellationToken)
            : await this.searchParksHandler.HandleAsync(
                new SearchParksQuery(
                    query.Trim(),
                    null,
                    pagination.ToApplication(),
                    IncludeHidden: true,
                    ClosedFilter: ClosedEntityFilter.All,
                    SortField: ParkAdminSortField.Name),
                cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static park => park.ToHttp()));
    }

    [HttpGet("{parkId}/data-completeness")]
    [AdminAudit("park-data-editor.data-completeness", "Park", TargetIdRouteKey = "parkId")]
    [ProducesResponseType(typeof(DataCompletenessScoreDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDataCompletenessAsync(
        [FromRoute] string parkId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<DataCompletenessScore> result = await this.completenessHandler.HandleAsync(
            new GetParkDataCompletenessScoreQuery(parkId, IncludeHidden: true),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
