using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Queries;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkFit;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Audit administratif en lecture seule de la couverture des faits FIT.
/// </summary>
[ApiController]
[Route("admin/park-fit/data-quality")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminParkFitDataQualityController : ControllerBase
{
    private readonly IQueryHandler<
        GetParkFitDataQualityPageQuery,
        ApplicationResult<PagedResult<ParkFitDataQualityOperationsResult>>> handler;

    public AdminParkFitDataQualityController(
        IQueryHandler<
            GetParkFitDataQualityPageQuery,
            ApplicationResult<PagedResult<ParkFitDataQualityOperationsResult>>> handler)
    {
        this.handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<ParkFitDataQualityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAsync(
        [FromQuery] PaginationRequestDto pagination,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<PagedResult<ParkFitDataQualityOperationsResult>> result =
            await this.handler.HandleAsync(
                new GetParkFitDataQualityPageQuery(pagination.ToApplication()),
                cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static assessment => assessment.ToHttp()));
    }
}
