using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Passport.Queries;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Passport;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/passport/ride-targets")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PassportRideTargetsController : ControllerBase
{
    private readonly IQueryHandler<ValidateRideTargetsQuery, ApplicationResult<bool>>
        validateHandler;

    public PassportRideTargetsController(
        IQueryHandler<ValidateRideTargetsQuery, ApplicationResult<bool>> validateHandler)
    {
        this.validateHandler = validateHandler;
    }

    [HttpPost(":validate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ValidateAsync(
        [FromBody] ValidatePassportRideTargetsRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<bool> result = await this.validateHandler.HandleAsync(
            new ValidateRideTargetsQuery(userId, request.ParkId, request.ParkItemIds),
            cancellationToken);
        return result.IsSuccess
            ? this.NoContent()
            : this.ToActionResult(result);
    }
}
