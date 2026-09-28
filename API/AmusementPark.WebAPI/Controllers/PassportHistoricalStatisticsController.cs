using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
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
[Route("me/passport/stats/history")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PassportHistoricalStatisticsController : ControllerBase
{
    private readonly IQueryHandler<
        GetPassportHistoricalStatisticsQuery,
        ApplicationResult<PassportHistoricalStatisticsResult>> handler;

    public PassportHistoricalStatisticsController(
        IQueryHandler<
            GetPassportHistoricalStatisticsQuery,
            ApplicationResult<PassportHistoricalStatisticsResult>> handler)
    {
        this.handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PassportHistoricalStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status401Unauthorized,
                "Authentication is required.",
                "auth.unauthorized");
        }

        ApplicationResult<PassportHistoricalStatisticsResult> result =
            await this.handler.HandleAsync(
                new GetPassportHistoricalStatisticsQuery(userId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
