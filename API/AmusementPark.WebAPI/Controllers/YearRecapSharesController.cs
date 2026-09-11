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
[Route("me/passport/years/{year:int}/share")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
public sealed class YearRecapSharesController : ControllerBase
{
    private readonly IQueryHandler<GetSharePublicationSettingsQuery, ApplicationResult<SharePublicationSettingsResult>> getSettingsHandler;
    private readonly IQueryHandler<GetYearRecapShareSelectionQuery, ApplicationResult<YearRecapShareSelectionResult>> getSelectionHandler;
    private readonly ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>> setVisibilityHandler;

    public YearRecapSharesController(
        IQueryHandler<GetSharePublicationSettingsQuery, ApplicationResult<SharePublicationSettingsResult>> getSettingsHandler,
        IQueryHandler<GetYearRecapShareSelectionQuery, ApplicationResult<YearRecapShareSelectionResult>> getSelectionHandler,
        ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>> setVisibilityHandler)
    {
        this.getSettingsHandler = getSettingsHandler;
        this.getSelectionHandler = getSelectionHandler;
        this.setVisibilityHandler = setVisibilityHandler;
    }

    [HttpGet("selection")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(YearRecapShareSelectionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSelectionAsync(
        [FromRoute] int year,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<YearRecapShareSelectionResult> result =
            await this.getSelectionHandler.HandleAsync(
                new GetYearRecapShareSelectionQuery(userId, year),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(new YearRecapShareSelectionDto
            {
                SavedPublicCaption = result.Value.SavedPublicCaption,
                HasSavedSnapshot = result.Value.HasSavedSnapshot,
            })
            : this.ToActionResult(result);
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharePublicationSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] int year,
        CancellationToken cancellationToken = default)
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
                    SharePublicationType.YearRecap,
                    year.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToSharingHttp())
            : this.ToActionResult(result);
    }

    [HttpDelete]
    [InvalidatesPublicCache(PublicCacheScope.Data)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharePublicationSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeAsync(
        [FromRoute] int year,
        CancellationToken cancellationToken = default)
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
                    SharePublicationType.YearRecap,
                    year.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    false),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToSharingHttp())
            : this.ToActionResult(result);
    }
}
