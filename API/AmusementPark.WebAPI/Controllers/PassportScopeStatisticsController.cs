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
[Route("me/passport")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PassportScopeStatisticsController : ControllerBase
{
    private readonly IQueryHandler<
        GetPassportParkStatisticsQuery,
        ApplicationResult<PassportParkStatisticsResult>> parkStatisticsHandler;
    private readonly IQueryHandler<
        GetPassportYearStatisticsQuery,
        ApplicationResult<PassportYearStatisticsResult>> yearStatisticsHandler;

    public PassportScopeStatisticsController(
        IQueryHandler<
            GetPassportParkStatisticsQuery,
            ApplicationResult<PassportParkStatisticsResult>> parkStatisticsHandler,
        IQueryHandler<
            GetPassportYearStatisticsQuery,
            ApplicationResult<PassportYearStatisticsResult>> yearStatisticsHandler)
    {
        this.parkStatisticsHandler = parkStatisticsHandler;
        this.yearStatisticsHandler = yearStatisticsHandler;
    }

    [HttpGet("parks/{parkId}/stats")]
    [ProducesResponseType(typeof(PassportParkStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetParkStatisticsAsync(
        [FromRoute] string parkId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.UnauthorizedResult();
        }

        ApplicationResult<PassportParkStatisticsResult> result =
            await this.parkStatisticsHandler.HandleAsync(
                new GetPassportParkStatisticsQuery(userId, parkId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpGet("years/{year:int}/stats")]
    [ProducesResponseType(typeof(PassportYearStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetYearStatisticsAsync(
        [FromRoute] int year,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.UnauthorizedResult();
        }

        ApplicationResult<PassportYearStatisticsResult> result =
            await this.yearStatisticsHandler.HandleAsync(
                new GetPassportYearStatisticsQuery(userId, year),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    private IActionResult UnauthorizedResult()
    {
        return this.ToProblemDetailsResult(
            StatusCodes.Status401Unauthorized,
            "Authentication is required.",
            "auth.unauthorized");
    }
}
