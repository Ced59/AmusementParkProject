using AmusementPark.Application.Features.FeatureFlags.Results;
using AmusementPark.Application.Features.FeatureFlags.Services;
using AmusementPark.WebAPI.Contracts.FeatureFlags;
using AmusementPark.WebAPI.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("public/capabilities")]
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PublicCapabilitiesController : ControllerBase
{
    private readonly PublicFeatureCapabilityReader reader;

    public PublicCapabilitiesController(PublicFeatureCapabilityReader reader)
    {
        this.reader = reader;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyCollection<PublicFeatureCapabilityDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<PublicFeatureCapabilityResult> results =
            await this.reader.ReadAsync(cancellationToken);
        return this.Ok(results.Select(static item => item.ToHttp()).ToArray());
    }
}
