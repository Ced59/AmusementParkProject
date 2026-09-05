using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.PassportBeta;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/passport-beta")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminPassportBetaController : ControllerBase
{
    private readonly IQueryHandler<
        GetPassportBetaMetricsQuery,
        ApplicationResult<PassportBetaMetricsResult>> metricsHandler;

    public AdminPassportBetaController(
        IQueryHandler<
            GetPassportBetaMetricsQuery,
            ApplicationResult<PassportBetaMetricsResult>> metricsHandler)
    {
        this.metricsHandler = metricsHandler;
    }

    [HttpGet("metrics")]
    [ProducesResponseType(typeof(PassportBetaMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMetricsAsync(
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<PassportBetaMetricsResult> result =
            await this.metricsHandler.HandleAsync(
                new GetPassportBetaMetricsQuery(fromUtc, toUtc),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
