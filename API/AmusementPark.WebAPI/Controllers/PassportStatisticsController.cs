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
public sealed class PassportStatisticsController : ControllerBase
{
    private readonly IQueryHandler<
        GetPassportItemStatisticsQuery,
        ApplicationResult<PassportItemStatisticsResult>> itemStatisticsHandler;

    public PassportStatisticsController(
        IQueryHandler<
            GetPassportItemStatisticsQuery,
            ApplicationResult<PassportItemStatisticsResult>> itemStatisticsHandler)
    {
        this.itemStatisticsHandler = itemStatisticsHandler;
    }

    [HttpGet("items/{parkItemId}/stats")]
    [ProducesResponseType(typeof(PassportItemStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetItemStatisticsAsync(
        [FromRoute] string parkItemId,
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

        ApplicationResult<PassportItemStatisticsResult> result =
            await this.itemStatisticsHandler.HandleAsync(
                new GetPassportItemStatisticsQuery(userId, parkItemId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
