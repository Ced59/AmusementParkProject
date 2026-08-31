using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
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

/// <summary>
/// Diagnostic en lecture seule des notes et des indexes nécessaires aux classements.
/// </summary>
[ApiController]
[Route("admin/ratings/diagnostics")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class AdminRatingDiagnosticsController : ControllerBase
{
    private readonly IQueryHandler<GetRatingDiagnosticsQuery, ApplicationResult<RatingDiagnosticsResult>> handler;

    public AdminRatingDiagnosticsController(
        IQueryHandler<GetRatingDiagnosticsQuery, ApplicationResult<RatingDiagnosticsResult>> handler)
    {
        this.handler = handler;
    }

    [HttpGet]
    [EnableRateLimiting(RateLimitPolicyNames.RatingDiagnostics)]
    [ProducesResponseType(typeof(RatingDiagnosticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken = default)
    {
        ApplicationResult<RatingDiagnosticsResult> result = await this.handler.HandleAsync(
            new GetRatingDiagnosticsQuery(),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
