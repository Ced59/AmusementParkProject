using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Passport;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/passport/visits/{visitId}/historical-ride-targets")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PassportHistoricalRideTargetsController : ControllerBase
{
    private readonly IQueryHandler<
        ListVisitHistoricalRideTargetsQuery,
        ApplicationResult<VisitHistoricalRideTargetPageResult>> handler;

    public PassportHistoricalRideTargetsController(
        IQueryHandler<
            ListVisitHistoricalRideTargetsQuery,
            ApplicationResult<VisitHistoricalRideTargetPageResult>> handler)
    {
        this.handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PassportHistoricalRideTargetPageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromRoute] string visitId,
        [FromQuery] string? search,
        [FromQuery] string? scope = null,
        [FromQuery] string? zoneId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status401Unauthorized,
                "Authentication is required.",
                "auth.unauthorized");
        }

        if (!Enum.TryParse(
                scope?.Trim() ?? nameof(VisitHistoricalTargetScope.KnownOpen),
                true,
                out VisitHistoricalTargetScope parsedScope)
            || !Enum.IsDefined(parsedScope))
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status400BadRequest,
                "The historical target scope is invalid.",
                "passport.historical-target-scope-invalid");
        }

        ApplicationResult<VisitHistoricalRideTargetPageResult> result =
            await this.handler.HandleAsync(
                new ListVisitHistoricalRideTargetsQuery(
                    userId,
                    visitId,
                    search,
                    parsedScope,
                    zoneId,
                    page,
                    pageSize),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
