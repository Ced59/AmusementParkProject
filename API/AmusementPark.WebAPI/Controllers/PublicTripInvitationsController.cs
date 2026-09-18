using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Contracts.Trips;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("public/trip-invitations")]
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PublicTripInvitationsController : ControllerBase
{
    private readonly IQueryHandler<GetTripInvitationPreviewQuery,
        ApplicationResult<TripInvitationPreviewResult>> previewHandler;

    public PublicTripInvitationsController(
        IQueryHandler<GetTripInvitationPreviewQuery,
            ApplicationResult<TripInvitationPreviewResult>> previewHandler)
    {
        this.previewHandler = previewHandler;
    }

    [HttpGet("{token}/preview")]
    [EnableRateLimiting(RateLimitPolicyNames.SharePublicationPreviews)]
    [ProducesResponseType(typeof(TripInvitationPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewAsync(
        [FromRoute] string token,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<TripInvitationPreviewResult> result = await this.previewHandler.HandleAsync(
            new GetTripInvitationPreviewQuery(token),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
