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
[Route("passport/shared/years")]
public sealed class SharedYearRecapsController : ControllerBase
{
    private readonly IQueryHandler<GetSharedYearRecapQuery, ApplicationResult<SharedYearRecapResult>> handler;

    public SharedYearRecapsController(
        IQueryHandler<GetSharedYearRecapQuery, ApplicationResult<SharedYearRecapResult>> handler)
    {
        this.handler = handler;
    }

    [HttpGet("{shareId}")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharedYearRecapDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string shareId,
        CancellationToken cancellationToken = default)
    {
        this.Response.Headers["Referrer-Policy"] = "no-referrer";
        ApplicationResult<SharedYearRecapResult> result = await this.handler.HandleAsync(
            new GetSharedYearRecapQuery(shareId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(new SharedYearRecapDto
            {
                PublishedAtUtc = result.Value.PublishedAtUtc,
                YearRecap = result.Value.Content.ToHttp(),
            })
            : this.ToActionResult(result);
    }
}
