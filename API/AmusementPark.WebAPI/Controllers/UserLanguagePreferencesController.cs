using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Core.Domain.Users;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Users;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Manages the authenticated user's language preference.
/// </summary>
[ApiController]
[Route("users/me/preferences/language")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
public sealed class UserLanguagePreferencesController : ControllerBase
{
    private readonly ICommandHandler<UpdatePreferredLanguageCommand, ApplicationResult<User>> updatePreferredLanguageCommandHandler;

    public UserLanguagePreferencesController(
        ICommandHandler<UpdatePreferredLanguageCommand, ApplicationResult<User>> updatePreferredLanguageCommandHandler)
    {
        this.updatePreferredLanguageCommandHandler = updatePreferredLanguageCommandHandler;
    }

    [HttpPatch]
    [ProducesResponseType(typeof(UserUpdatedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        [FromBody] PreferredLanguageUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        string? currentUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return this.ToProblemDetailsResult(
                StatusCodes.Status401Unauthorized,
                "Authentication is required.",
                "auth.unauthorized");
        }

        ApplicationResult<User> result = await this.updatePreferredLanguageCommandHandler.HandleAsync(
            new UpdatePreferredLanguageCommand(currentUserId, request.PreferredLanguage),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToUpdatedDto());
    }
}
