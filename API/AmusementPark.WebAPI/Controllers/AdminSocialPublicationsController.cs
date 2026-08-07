using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.SocialPublishing;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Extensions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/social-publications")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class AdminSocialPublicationsController : ControllerBase
{
    private readonly IQueryHandler<GetSocialPublishingOverviewQuery, SocialPublishingOverview> overviewHandler;
    private readonly IQueryHandler<GetSocialPublicationDraftQuery, ApplicationResult<SocialPublicationDraft>> draftHandler;
    private readonly ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>> publishHandler;
    private readonly ICommandHandler<RetrySocialPublicationCommand, ApplicationResult<SocialPublication>> retryHandler;
    private readonly ICommandHandler<UpdateSocialPublicationCommand, ApplicationResult<SocialPublication>> updateHandler;
    private readonly ICommandHandler<DeleteSocialPublicationCommand, ApplicationResult<SocialPublication>> deleteHandler;
    private readonly ICommandHandler<SynchronizeSocialPublicationsCommand, SocialPublicationSynchronizationResult> synchronizeHandler;

    public AdminSocialPublicationsController(
        IQueryHandler<GetSocialPublishingOverviewQuery, SocialPublishingOverview> overviewHandler,
        IQueryHandler<GetSocialPublicationDraftQuery, ApplicationResult<SocialPublicationDraft>> draftHandler,
        ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>> publishHandler,
        ICommandHandler<RetrySocialPublicationCommand, ApplicationResult<SocialPublication>> retryHandler,
        ICommandHandler<UpdateSocialPublicationCommand, ApplicationResult<SocialPublication>> updateHandler,
        ICommandHandler<DeleteSocialPublicationCommand, ApplicationResult<SocialPublication>> deleteHandler,
        ICommandHandler<SynchronizeSocialPublicationsCommand, SocialPublicationSynchronizationResult> synchronizeHandler)
    {
        this.overviewHandler = overviewHandler;
        this.draftHandler = draftHandler;
        this.publishHandler = publishHandler;
        this.retryHandler = retryHandler;
        this.updateHandler = updateHandler;
        this.deleteHandler = deleteHandler;
        this.synchronizeHandler = synchronizeHandler;
    }

    [HttpGet("draft")]
    [ProducesResponseType(typeof(SocialPublicationDraftDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDraftAsync(
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

    [HttpGet]
    [ProducesResponseType(typeof(SocialPublishingOverviewDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverviewAsync([FromQuery] int limit = 25, CancellationToken cancellationToken = default)
    {
        SocialPublishingOverview overview = await this.overviewHandler.HandleAsync(
            new GetSocialPublishingOverviewQuery(limit),
            cancellationToken);
        return this.Ok(overview.ToHttp());
    }

    [HttpPost]
    [AdminAudit("social-publication.publish", "SocialPublication", StaticTargetId = "manual")]
    [ProducesResponseType(typeof(SocialPublicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublishAsync([FromBody] PublishSocialLinkRequestDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<SocialPublication> result = await this.publishHandler.HandleAsync(
            new PublishSocialLinkCommand(request.ToApplication(), this.User.GetUserId()),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("{publicationId}/retry")]
    [AdminAudit("social-publication.retry", "SocialPublication", TargetIdRouteKey = "publicationId")]
    [ProducesResponseType(typeof(SocialPublicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RetryAsync([FromRoute] string publicationId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<SocialPublication> result = await this.retryHandler.HandleAsync(
            new RetrySocialPublicationCommand(publicationId, this.User.GetUserId()),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{publicationId}")]
    [AdminAudit("social-publication.update", "SocialPublication", TargetIdRouteKey = "publicationId")]
    [ProducesResponseType(typeof(SocialPublicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] string publicationId,
        [FromBody] UpdateSocialPublicationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<SocialPublication> result = await this.updateHandler.HandleAsync(
            new UpdateSocialPublicationCommand(publicationId, request.Message, this.User.GetUserId()),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpDelete("{publicationId}")]
    [AdminAudit("social-publication.delete", "SocialPublication", TargetIdRouteKey = "publicationId")]
    [ProducesResponseType(typeof(SocialPublicationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string publicationId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<SocialPublication> result = await this.deleteHandler.HandleAsync(
            new DeleteSocialPublicationCommand(publicationId, this.User.GetUserId()),
            cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("synchronize")]
    [AdminAudit("social-publication.synchronize", "SocialPublication", StaticTargetId = "recent")]
    [ProducesResponseType(typeof(SocialPublicationSynchronizationResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SynchronizeAsync(
        [FromQuery] int limit = 25,
        CancellationToken cancellationToken = default)
    {
        SocialPublicationSynchronizationResult result = await this.synchronizeHandler.HandleAsync(
            new SynchronizeSocialPublicationsCommand(limit),
            cancellationToken);
        return this.Ok(result.ToHttp());
    }
}
