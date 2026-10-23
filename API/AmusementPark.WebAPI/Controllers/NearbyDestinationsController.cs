using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.NearbyDestinations.Queries;
using AmusementPark.Application.Features.NearbyDestinations.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.NearbyDestinations;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("nearby-destinations")]
public sealed class NearbyDestinationsController : ControllerBase
{
    private readonly IQueryHandler<GetNearbyDestinationsQuery, ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>> handler;

    public NearbyDestinationsController(
        IQueryHandler<GetNearbyDestinationsQuery, ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>>> handler)
    {
        this.handler = handler;
    }

    [HttpGet("{sourceKind}/{sourceId}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataShort)]
    [ProducesResponseType(typeof(IReadOnlyCollection<NearbyDestinationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] NearbyDestinationKind sourceKind,
        [FromRoute] string sourceId,
        [FromQuery] int limit = 4,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<NearbyDestinationResult>> result = await this.handler.HandleAsync(
            new GetNearbyDestinationsQuery(sourceKind, sourceId, limit), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.Select(static target => new NearbyDestinationDto(
            (target.Park is not null ? NearbyDestinationKind.Park : NearbyDestinationKind.StandaloneAttraction).ToString(),
            target.Park?.ToHttp(), target.StandaloneAttraction?.ToHttp(), target.DistanceKilometers,
            target.EstimatedTravelDurationMinutes, target.ProximityRank)).ToList());
    }
}
