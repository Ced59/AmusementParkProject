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
[Route("me/passport/visits/{visitId}/share")]
[Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
[RequireActivatedUnblockedUser]
public sealed class VisitRecapSharesController : ControllerBase
{
    private readonly IQueryHandler<GetSharePublicationSettingsQuery, ApplicationResult<SharePublicationSettingsResult>> getSettingsHandler;
    private readonly IQueryHandler<GetVisitRecapShareCandidatesQuery, ApplicationResult<VisitRecapShareCandidatesResult>> getCandidatesHandler;
    private readonly ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>> setVisibilityHandler;

    public VisitRecapSharesController(
        IQueryHandler<GetSharePublicationSettingsQuery, ApplicationResult<SharePublicationSettingsResult>> getSettingsHandler,
        IQueryHandler<GetVisitRecapShareCandidatesQuery, ApplicationResult<VisitRecapShareCandidatesResult>> getCandidatesHandler,
        ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>> setVisibilityHandler)
    {
        this.getSettingsHandler = getSettingsHandler;
        this.getCandidatesHandler = getCandidatesHandler;
        this.setVisibilityHandler = setVisibilityHandler;
    }

    [HttpGet("candidates")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(VisitRecapShareCandidatesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCandidatesAsync(
        [FromRoute] string visitId,
        [FromQuery] bool includeMissedItems = false,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<VisitRecapShareCandidatesResult> result =
            await this.getCandidatesHandler.HandleAsync(
                new GetVisitRecapShareCandidatesQuery(
                    userId,
                    visitId,
                    includeMissedItems),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharePublicationSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string visitId,
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
                    SharePublicationType.VisitRecap,
                    visitId),
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
        [FromRoute] string visitId,
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
                    SharePublicationType.VisitRecap,
                    visitId,
                    false),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToSharingHttp())
            : this.ToActionResult(result);
    }
}
