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
using AmusementPark.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("park-data-editor/tokens")]
public sealed class ParkDataEditorTokensController : ControllerBase
{
    private readonly ICommandHandler<CreateParkDataEditorTokenCommand, ApplicationResult<CreatedParkDataEditorTokenResult>> createHandler;
    private readonly IQueryHandler<ListParkDataEditorTokensQuery, ApplicationResult<IReadOnlyCollection<ParkDataEditorAccessToken>>> listHandler;
    private readonly ICommandHandler<RevokeParkDataEditorTokensCommand, ApplicationResult<RevokedParkDataEditorTokensResult>> revokeHandler;

    public ParkDataEditorTokensController(
        ICommandHandler<CreateParkDataEditorTokenCommand, ApplicationResult<CreatedParkDataEditorTokenResult>> createHandler,
        IQueryHandler<ListParkDataEditorTokensQuery, ApplicationResult<IReadOnlyCollection<ParkDataEditorAccessToken>>> listHandler,
        ICommandHandler<RevokeParkDataEditorTokensCommand, ApplicationResult<RevokedParkDataEditorTokensResult>> revokeHandler)
    {
        this.createHandler = createHandler;
        this.listHandler = listHandler;
        this.revokeHandler = revokeHandler;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.ParkDataEditorJwt)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("park-data-editor-token.create", "ParkDataEditorAccessToken")]
    [ProducesResponseType(typeof(CreatedParkDataEditorTokenDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateParkDataEditorTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        ApplicationResult<CreatedParkDataEditorTokenResult> result = await this.createHandler.HandleAsync(
            new CreateParkDataEditorTokenCommand(userId, request.Label, request.ExpiresInDays),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new CreatedParkDataEditorTokenDto
        {
            Token = result.Value.Token.ToHttp(),
            PlainTextToken = result.Value.PlainTextToken,
        });
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.ParkDataEditorJwt)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("park-data-editor-token.list", "ParkDataEditorAccessToken", StaticTargetId = "self")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ParkDataEditorTokenDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListOwnAsync(CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        return await this.ListAsync(userId, cancellationToken);
    }

    [HttpDelete("{tokenId}")]
    [Authorize(Policy = AuthorizationPolicyNames.ParkDataEditorJwt)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("park-data-editor-token.revoke", "ParkDataEditorAccessToken", TargetIdRouteKey = "tokenId")]
    [ProducesResponseType(typeof(RevokedParkDataEditorTokensDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeOwnAsync(
        [FromRoute] string tokenId,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        return await this.RevokeAsync(userId, tokenId, userId, "Self-revoked", cancellationToken);
    }

    [HttpDelete("current")]
    [Authorize(Policy = AuthorizationPolicyNames.ParkDataEditorToken)]
    [AllowParkDataEditorToken]
    [RequireActivatedUnblockedUser]
    [AdminAudit("park-data-editor-token.revoke-current", "ParkDataEditorAccessToken")]
    [ProducesResponseType(typeof(RevokedParkDataEditorTokensDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeCurrentAsync(CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        string? tokenId = this.User.FindFirst(ParkDataEditorAuthenticationDefaults.TokenIdClaim)?.Value;
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(tokenId))
        {
            return this.Unauthorized();
        }

        return await this.RevokeAsync(userId, tokenId, userId, "Current token self-revoked", cancellationToken);
    }

    private async Task<IActionResult> ListAsync(string userId, CancellationToken cancellationToken)
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

    private async Task<IActionResult> RevokeAsync(
        string userId,
        string tokenId,
        string revokedByUserId,
        string reason,
        CancellationToken cancellationToken)
    {
        ApplicationResult<RevokedParkDataEditorTokensResult> result = await this.revokeHandler.HandleAsync(
            new RevokeParkDataEditorTokensCommand(userId, tokenId, revokedByUserId, reason),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new RevokedParkDataEditorTokensDto { RevokedCount = result.Value.RevokedCount });
    }
}
