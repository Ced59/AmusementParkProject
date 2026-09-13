using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("passport/shared/comparisons")]
public sealed class SharedProfileComparisonsController : ControllerBase
{
    private readonly IQueryHandler<GetSharedProfileComparisonQuery,
        ApplicationResult<SharedProfileComparisonResult>> handler;

    public SharedProfileComparisonsController(
        IQueryHandler<GetSharedProfileComparisonQuery,
            ApplicationResult<SharedProfileComparisonResult>> handler)
    {
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    [HttpGet("{shareId}")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharedProfileComparisonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string shareId,
        CancellationToken cancellationToken = default)
    {
        this.Response.Headers["Referrer-Policy"] = "no-referrer";
        ApplicationResult<SharedProfileComparisonResult> result = await this.handler.HandleAsync(
            new GetSharedProfileComparisonQuery(shareId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
