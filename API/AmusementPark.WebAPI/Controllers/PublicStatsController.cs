using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.WebAPI.Contracts.PublicStats;
using AmusementPark.WebAPI.Responses;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Authorization;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur des statistiques publiques transverses.
/// </summary>
[ApiController]
[Route("public-stats")]
public sealed class PublicStatsController : ControllerBase
{
    private readonly IQueryHandler<GetPublicHomeStatsQuery, ApplicationResult<PublicHomeStatsResult>> getPublicHomeStatsQueryHandler;

    public PublicStatsController(IQueryHandler<GetPublicHomeStatsQuery, ApplicationResult<PublicHomeStatsResult>> getPublicHomeStatsQueryHandler)
    {
        this.getPublicHomeStatsQueryHandler = getPublicHomeStatsQueryHandler;
    }

    [HttpGet("home")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicHomeStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHomeStatsAsync(CancellationToken cancellationToken = default)
    {
        ApplicationResult<PublicHomeStatsResult> result = await this.getPublicHomeStatsQueryHandler.HandleAsync(
            new GetPublicHomeStatsQuery(),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PublicHomeStatsDto response = new PublicHomeStatsDto(
            result.Value.ParksCount,
            result.Value.AttractionsCount,
            result.Value.CountriesCount);

        return this.Ok(response);
    }

}
