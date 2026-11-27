using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.SocialPublishing;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("park-data-editor/social-publications")]
[Authorize(Policy = AuthorizationPolicyNames.AdminOrParkDataEditorToken)]
[AllowParkDataEditorToken]
[RequireActivatedUnblockedUser]
public sealed class ParkDataEditorSocialPublicationsController : ControllerBase
{
    private readonly IQueryHandler<GetSocialPublicationDraftQuery, ApplicationResult<SocialPublicationDraft>> draftHandler;
    private readonly ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>> publishHandler;
    private readonly ICommandHandler<RetryOwnFacebookPublicationCommand, ApplicationResult<SocialPublication>> retryHandler;
    private readonly ICommandHandler<DeleteSocialPublicationCommand, ApplicationResult<SocialPublication>> deleteHandler;

    public ParkDataEditorSocialPublicationsController(
        IQueryHandler<GetSocialPublicationDraftQuery, ApplicationResult<SocialPublicationDraft>> draftHandler,
        ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>> publishHandler,
        ICommandHandler<RetryOwnFacebookPublicationCommand, ApplicationResult<SocialPublication>> retryHandler,
        ICommandHandler<DeleteSocialPublicationCommand, ApplicationResult<SocialPublication>> deleteHandler)
    {
        this.draftHandler = draftHandler;
        this.publishHandler = publishHandler;
        this.retryHandler = retryHandler;
        this.deleteHandler = deleteHandler;
    }

    [HttpPost("facebook/{publicationId}/retry")]
    [AdminAudit("park-data-editor.social-publication.retry-own", "SocialPublication", TargetIdRouteKey = "publicationId")]
    [ProducesResponseType(typeof(SocialPublicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RetryOwnFacebookAsync(
        [FromRoute] string publicationId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<SocialPublication> result = await this.retryHandler.HandleAsync(
            new RetryOwnFacebookPublicationCommand(publicationId, this.User.GetUserId()),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("facebook/draft")]
    [AdminAudit("park-data-editor.social-publication.draft", "SocialPublication", StaticTargetId = "facebook")]
    [ProducesResponseType(typeof(SocialPublicationDraftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFacebookDraftAsync(
        [FromQuery] string? url,
        [FromQuery] PaginationRequestDto imagePagination,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<SocialPublicationDraft> result = await this.draftHandler.HandleAsync(
            new GetSocialPublicationDraftQuery(url, imagePagination.Page, imagePagination.Size),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("facebook")]
    [AdminAudit("park-data-editor.social-publication.publish", "SocialPublication", StaticTargetId = "facebook")]
    [ProducesResponseType(typeof(SocialPublicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishFacebookAsync(
        [FromBody] PublishSocialLinkRequestDto request,
        CancellationToken cancellationToken = default)
    {
        request.Network = SocialNetworkDto.Facebook;
        ApplicationResult<SocialPublication> result = await this.publishHandler.HandleAsync(
            new PublishSocialLinkCommand(request.ToApplication(), this.User.GetUserId()),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpDelete("facebook/{publicationId}")]
    [AdminAudit("park-data-editor.social-publication.delete-own", "SocialPublication", TargetIdRouteKey = "publicationId")]
    [ProducesResponseType(typeof(SocialPublicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteOwnFacebookAsync(
        [FromRoute] string publicationId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<SocialPublication> result = await this.deleteHandler.HandleAsync(
            new DeleteSocialPublicationCommand(publicationId, this.User.GetUserId(), RequireRequesterOwnership: true),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
