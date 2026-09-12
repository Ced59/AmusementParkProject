using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/passport/share")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
public sealed class PassportProfileSharesController : ControllerBase
{
    private readonly IQueryHandler<GetSharePublicationSettingsQuery, ApplicationResult<SharePublicationSettingsResult>> getSettingsHandler;
    private readonly IQueryHandler<GetPassportProfileShareSelectionQuery, ApplicationResult<PassportProfileShareSelectionResult>> getSelectionHandler;
    private readonly ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>> setVisibilityHandler;

    public PassportProfileSharesController(
        IQueryHandler<GetSharePublicationSettingsQuery, ApplicationResult<SharePublicationSettingsResult>> getSettingsHandler,
        IQueryHandler<GetPassportProfileShareSelectionQuery, ApplicationResult<PassportProfileShareSelectionResult>> getSelectionHandler,
        ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>> setVisibilityHandler)
    {
        this.getSettingsHandler = getSettingsHandler;
        this.getSelectionHandler = getSelectionHandler;
        this.setVisibilityHandler = setVisibilityHandler;
    }

    [HttpGet("selection")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(PassportProfileShareSelectionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSelectionAsync(CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<PassportProfileShareSelectionResult> result =
            await this.getSelectionHandler.HandleAsync(
                new GetPassportProfileShareSelectionQuery(userId),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharePublicationSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<SharePublicationSettingsResult> result =
            await this.getSettingsHandler.HandleAsync(
                new GetSharePublicationSettingsQuery(
                    userId,
                    SharePublicationType.PassportProfile,
                    null),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToSharingHttp())
            : this.ToActionResult(result);
    }

    [HttpDelete]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharePublicationSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeAsync(CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<SharePublicationSettingsResult> result =
            await this.setVisibilityHandler.HandleAsync(
                new SetSharePublicationVisibilityCommand(
                    userId,
                    SharePublicationType.PassportProfile,
                    null,
                    false),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToSharingHttp())
            : this.ToActionResult(result);
    }
}
