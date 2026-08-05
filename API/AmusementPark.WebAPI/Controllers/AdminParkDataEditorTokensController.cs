using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkDataEditorTokens.Commands;
using AmusementPark.Application.Features.ParkDataEditorTokens.Queries;
using AmusementPark.Application.Features.ParkDataEditorTokens.Results;
using AmusementPark.Core.Domain.Users;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.ParkDataEditorTokens;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/users/{userId}/park-data-editor-tokens")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class AdminParkDataEditorTokensController : ControllerBase
{
    private readonly IQueryHandler<ListParkDataEditorTokensQuery, ApplicationResult<IReadOnlyCollection<ParkDataEditorAccessToken>>> listHandler;
    private readonly ICommandHandler<RevokeParkDataEditorTokensCommand, ApplicationResult<RevokedParkDataEditorTokensResult>> revokeHandler;

    public AdminParkDataEditorTokensController(
        IQueryHandler<ListParkDataEditorTokensQuery, ApplicationResult<IReadOnlyCollection<ParkDataEditorAccessToken>>> listHandler,
        ICommandHandler<RevokeParkDataEditorTokensCommand, ApplicationResult<RevokedParkDataEditorTokensResult>> revokeHandler)
    {
        this.listHandler = listHandler;
        this.revokeHandler = revokeHandler;
    }

    [HttpGet]
    [AdminAudit("park-data-editor-token.admin-list", "ParkDataEditorAccessToken", TargetIdRouteKey = "userId")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ParkDataEditorTokenDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(
        [FromRoute] string userId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<ParkDataEditorAccessToken>> result = await this.listHandler.HandleAsync(
            new ListParkDataEditorTokensQuery(userId),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.Select(static token => token.ToHttp()).ToList());
    }

    [HttpDelete("{tokenId}")]
    [AdminAudit("park-data-editor-token.admin-revoke", "ParkDataEditorAccessToken", TargetIdRouteKey = "tokenId")]
    [ProducesResponseType(typeof(RevokedParkDataEditorTokensDto), StatusCodes.Status200OK)]
    public Task<IActionResult> RevokeAsync(
        [FromRoute] string userId,
        [FromRoute] string tokenId,
        CancellationToken cancellationToken = default)
    {
        return this.RevokeInternalAsync(userId, tokenId, cancellationToken);
    }

    [HttpDelete]
    [AdminAudit("park-data-editor-token.admin-revoke-all", "ParkDataEditorAccessToken", TargetIdRouteKey = "userId")]
    [ProducesResponseType(typeof(RevokedParkDataEditorTokensDto), StatusCodes.Status200OK)]
    public Task<IActionResult> RevokeAllAsync(
        [FromRoute] string userId,
        CancellationToken cancellationToken = default)
    {
        return this.RevokeInternalAsync(userId, null, cancellationToken);
    }

    private async Task<IActionResult> RevokeInternalAsync(
        string userId,
        string? tokenId,
        CancellationToken cancellationToken)
    {
        string? adminUserId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(adminUserId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<RevokedParkDataEditorTokensResult> result = await this.revokeHandler.HandleAsync(
            new RevokeParkDataEditorTokensCommand(
                userId,
                tokenId,
                adminUserId,
                string.IsNullOrWhiteSpace(tokenId) ? "All tokens revoked by administrator" : "Revoked by administrator"),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new RevokedParkDataEditorTokensDto { RevokedCount = result.Value.RevokedCount });
    }
}
