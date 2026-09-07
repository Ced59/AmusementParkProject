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
[Route("passport/shared/visits")]
public sealed class SharedVisitRecapsController : ControllerBase
{
    private readonly IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>> handler;

    public SharedVisitRecapsController(
        IQueryHandler<GetSharedVisitRecapQuery, ApplicationResult<SharedVisitRecapResult>> handler)
    {
        this.handler = handler;
    }

    [HttpGet("{shareId}")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharedVisitRecapDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string shareId,
        CancellationToken cancellationToken = default)
    {
        this.Response.Headers["Referrer-Policy"] = "no-referrer";
        ApplicationResult<SharedVisitRecapResult> result = await this.handler.HandleAsync(
            new GetSharedVisitRecapQuery(shareId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(new SharedVisitRecapDto
            {
                PublishedAtUtc = result.Value.PublishedAtUtc,
                VisitRecap = result.Value.Content.ToPublicHttp(),
            })
            : this.ToActionResult(result);
    }
}
