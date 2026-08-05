using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.SocialPublishing.Commands;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.SocialPublishing.Queries;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.WebAPI.Authorization;
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
[Route("admin/social-publications")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class AdminSocialPublicationsController : ControllerBase
{
    private readonly IQueryHandler<GetSocialPublishingOverviewQuery, SocialPublishingOverview> overviewHandler;
    private readonly ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>> publishHandler;
    private readonly ICommandHandler<RetrySocialPublicationCommand, ApplicationResult<SocialPublication>> retryHandler;

    public AdminSocialPublicationsController(
        IQueryHandler<GetSocialPublishingOverviewQuery, SocialPublishingOverview> overviewHandler,
        ICommandHandler<PublishSocialLinkCommand, ApplicationResult<SocialPublication>> publishHandler,
        ICommandHandler<RetrySocialPublicationCommand, ApplicationResult<SocialPublication>> retryHandler)
    {
        this.overviewHandler = overviewHandler;
        this.publishHandler = publishHandler;
        this.retryHandler = retryHandler;
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
}
