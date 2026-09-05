using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Queries;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Sharing;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("me/shares")]
public sealed class SharePublicationsController : ControllerBase
{
    private readonly IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>> previewHandler;

    public SharePublicationsController(
        IQueryHandler<PreviewSharePublicationQuery, ApplicationResult<SharePublicationPreviewResult>> previewHandler)
    {
        this.previewHandler = previewHandler;
    }

    [HttpPost("preview")]
    [Authorize(Roles = AuthorizationRoleGroups.UserModeratorAdmin)]
    [RequireActivatedUnblockedUser]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(SharePublicationPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PreviewAsync(
        [FromBody] SharePublicationPreviewRequestDto request,
        CancellationToken cancellationToken = default)
    {
        string? userId = this.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return this.Unauthorized();
        }

        if (!request.TryToApplication(userId, out PreviewSharePublicationQuery? query)
            || query is null)
        {
            return this.BadRequest();
        }

        ApplicationResult<SharePublicationPreviewResult> result =
            await this.previewHandler.HandleAsync(query, cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
