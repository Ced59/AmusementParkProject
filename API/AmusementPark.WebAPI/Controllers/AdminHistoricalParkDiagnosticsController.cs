using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.History;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/history/parks/{parkId}/diagnostics")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminHistoricalParkDiagnosticsController : ControllerBase
{
    private readonly IQueryHandler<
        GetAdminHistoricalParkDiagnosticsQuery,
        ApplicationResult<AdminHistoricalParkDiagnosticsResult>> queryHandler;

    public AdminHistoricalParkDiagnosticsController(
        IQueryHandler<
            GetAdminHistoricalParkDiagnosticsQuery,
            ApplicationResult<AdminHistoricalParkDiagnosticsResult>> queryHandler)
    {
        this.queryHandler = queryHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminHistoricalParkDiagnosticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string parkId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<AdminHistoricalParkDiagnosticsResult> result =
            await this.queryHandler.HandleAsync(
                new GetAdminHistoricalParkDiagnosticsQuery(parkId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
