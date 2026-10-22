using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.StandaloneAttractions;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("park-data-editor/standalone-attractions")]
[Authorize(Policy = AuthorizationPolicyNames.AdminOrParkDataEditorToken)]
[AllowParkDataEditorToken]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ParkDataEditorStandaloneAttractionsController : ControllerBase
{
    private readonly IQueryHandler<GetStandaloneAttractionsPageQuery, ApplicationResult<PagedResult<StandaloneAttraction>>> searchHandler;

    public ParkDataEditorStandaloneAttractionsController(
        IQueryHandler<GetStandaloneAttractionsPageQuery, ApplicationResult<PagedResult<StandaloneAttraction>>> searchHandler)
    {
        this.searchHandler = searchHandler;
    }

    [HttpGet]
    [AdminAudit("park-data-editor.standalone-attraction-search", "StandaloneAttraction", StaticTargetId = "search")]
    [ProducesResponseType(typeof(PagedResponseDto<StandaloneAttractionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? query = null,
        [FromQuery] string? countryCode = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<PagedResult<StandaloneAttraction>> result = await this.searchHandler.HandleAsync(
            new GetStandaloneAttractionsPageQuery(
                pagination.ToApplication(),
                Search: string.IsNullOrWhiteSpace(query) ? null : query.Trim(),
                IncludeHidden: true,
                IsVisible: null,
                AdminReviewStatus: null,
                Type: null,
                CountryCode: string.IsNullOrWhiteSpace(countryCode) ? null : countryCode.Trim().ToUpperInvariant(),
                ManufacturerId: null,
                SortField: StandaloneAttractionAdminSortField.Name,
                SortDescending: false),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static attraction => attraction.ToHttp()));
    }
}
