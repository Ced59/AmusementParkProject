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
[Route("passport/shared/profiles")]
public sealed class SharedPassportProfilesController : ControllerBase
{
    private readonly IQueryHandler<GetSharedPassportProfileQuery, ApplicationResult<SharedPassportProfileResult>> handler;

    public SharedPassportProfilesController(
        IQueryHandler<GetSharedPassportProfileQuery, ApplicationResult<SharedPassportProfileResult>> handler)
    {
        this.handler = handler;
    }

    [HttpGet("{shareId}")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharedPassportProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string shareId,
        CancellationToken cancellationToken = default)
    {
        this.Response.Headers["Referrer-Policy"] = "no-referrer";
        ApplicationResult<SharedPassportProfileResult> result = await this.handler.HandleAsync(
            new GetSharedPassportProfileQuery(shareId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(new SharedPassportProfileDto
            {
                PublishedAtUtc = result.Value.PublishedAtUtc,
                PassportProfile = result.Value.Content.ToHttp(),
            })
            : this.ToActionResult(result);
    }
}
