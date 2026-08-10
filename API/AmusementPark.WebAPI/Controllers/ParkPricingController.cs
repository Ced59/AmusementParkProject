using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkPricing.Commands;
using AmusementPark.Application.Features.ParkPricing.Queries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.AdminPublicView;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.ParkPricing;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
public sealed class ParkPricingController : ControllerBase
{
    private readonly IQueryHandler<GetParkPricingQuery, ApplicationResult<ParkPricingEntity>> getPricingQueryHandler;
    private readonly ICommandHandler<UpsertParkPricingCommand, ApplicationResult<ParkPricingEntity>> upsertPricingCommandHandler;

    public ParkPricingController(
        IQueryHandler<GetParkPricingQuery, ApplicationResult<ParkPricingEntity>> getPricingQueryHandler,
        ICommandHandler<UpsertParkPricingCommand, ApplicationResult<ParkPricingEntity>> upsertPricingCommandHandler)
    {
        this.getPricingQueryHandler = getPricingQueryHandler;
        this.upsertPricingCommandHandler = upsertPricingCommandHandler;
    }

    [HttpGet("parks/{parkId}/pricing")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicPricingData)]
    [ProducesResponseType(typeof(ParkPricingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPricingAsync([FromRoute] string parkId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkPricingEntity> result = await this.getPricingQueryHandler.HandleAsync(
            new GetParkPricingQuery(parkId, this.HttpContext.UserCanSeeNonVisibleInPublicView()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPublicHttp());
    }

    [HttpGet("admin/parks/{parkId}/pricing")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(ParkPricingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdminPricingAsync([FromRoute] string parkId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkPricingEntity> result = await this.getPricingQueryHandler.HandleAsync(
            new GetParkPricingQuery(parkId, IncludeHidden: true),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("admin/parks/{parkId}/pricing")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("park-pricing.upsert", "Park", TargetIdRouteKey = "parkId")]
    [InvalidatesPublicCache(PublicCacheScope.Data, PublicCacheScope.Seo)]
    [ProducesResponseType(typeof(ParkPricingDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertAdminPricingAsync(
        [FromRoute] string parkId,
        [FromBody] ParkPricingDto request,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkPricingEntity> mappingResult = request.ToDomainResult(parkId);
        if (!mappingResult.IsSuccess || mappingResult.Value is null)
        {
            return this.ToActionResult(mappingResult);
        }

        ApplicationResult<ParkPricingEntity> result = await this.upsertPricingCommandHandler.HandleAsync(
            new UpsertParkPricingCommand(mappingResult.Value),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
