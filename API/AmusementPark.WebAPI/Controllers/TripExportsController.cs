using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Trips;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/trips/{tripPlanId}/export")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TripExportsController : ControllerBase
{
    private const int MaximumRequestIdLength = 200;
    private readonly IQueryHandler<ExportTripPlanQuery,
        ApplicationResult<TripExportResult>> handler;

    public TripExportsController(
        IQueryHandler<ExportTripPlanQuery,
            ApplicationResult<TripExportResult>> handler)
    {
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    [HttpGet]
    [ProducesResponseType(typeof(TripExportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string tripPlanId,
        [FromHeader(Name = "Idempotency-Key")] string? exportRequestId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(exportRequestId)
            || exportRequestId.Trim().Length > MaximumRequestIdLength)
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status400BadRequest,
                "A valid Idempotency-Key header is required.",
                "trip.export.request-id-invalid");
        }

        ApplicationResult<TripExportResult> result = await this.handler.HandleAsync(
            new ExportTripPlanQuery(userId, tripPlanId, exportRequestId.Trim()),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
