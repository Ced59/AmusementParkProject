using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.LiveData.Queries;
using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.WebAPI.Contracts.LiveData;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[AllowAnonymous]
[Route("public/live")]
public sealed class PublicLiveController : ControllerBase
{
    private readonly IQueryHandler<
        GetPublicParkLiveQuery,
        ApplicationResult<PublicLiveTargetResult>> parkHandler;
    private readonly IQueryHandler<
        GetPublicParkItemLiveQuery,
        ApplicationResult<PublicLiveTargetResult>> parkItemHandler;
    private readonly IQueryHandler<
        GetPublicParkLiveItemsQuery,
        ApplicationResult<PublicParkLiveItemsResult>> parkItemsHandler;

    public PublicLiveController(
        IQueryHandler<GetPublicParkLiveQuery, ApplicationResult<PublicLiveTargetResult>> parkHandler,
        IQueryHandler<GetPublicParkItemLiveQuery, ApplicationResult<PublicLiveTargetResult>> parkItemHandler,
        IQueryHandler<GetPublicParkLiveItemsQuery, ApplicationResult<PublicParkLiveItemsResult>> parkItemsHandler)
    {
        this.parkHandler = parkHandler;
        this.parkItemHandler = parkItemHandler;
        this.parkItemsHandler = parkItemsHandler;
    }

    [HttpGet("parks/{parkId}")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicLiveData)]
    [ProducesResponseType(typeof(PublicLiveTargetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParkAsync(
        [FromRoute] string parkId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<PublicLiveTargetResult> result = await this.parkHandler.HandleAsync(
            new GetPublicParkLiveQuery(parkId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.ToConditionalResponse(
                result.Value.ToHttp(),
                PublicLiveCacheLifetimeCalculator.Resolve(result.Value))
            : this.ToActionResult(result);
    }

    [HttpGet("items/{itemId}")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicLiveData)]
    [ProducesResponseType(typeof(PublicLiveTargetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParkItemAsync(
        [FromRoute] string itemId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<PublicLiveTargetResult> result = await this.parkItemHandler.HandleAsync(
            new GetPublicParkItemLiveQuery(itemId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.ToConditionalResponse(
                result.Value.ToHttp(),
                PublicLiveCacheLifetimeCalculator.Resolve(result.Value))
            : this.ToActionResult(result);
    }

    [HttpGet("parks/{parkId}/items")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicLiveData)]
    [ProducesResponseType(typeof(PublicParkLiveItemsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParkItemsAsync(
        [FromRoute] string parkId,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<PublicParkLiveItemsResult> result = await this.parkItemsHandler.HandleAsync(
            new GetPublicParkLiveItemsQuery(parkId),
            cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.ToConditionalResponse(
                result.Value.ToHttp(),
                PublicLiveCacheLifetimeCalculator.Resolve(result.Value))
            : this.ToActionResult(result);
    }

    private IActionResult ToConditionalResponse<TValue>(TValue value, TimeSpan cacheLifetime)
    {
        string entityTag = PublicLiveEntityTagFactory.Create(value);
        long maxAgeSeconds = checked((long)cacheLifetime.TotalSeconds);
        this.HttpContext.Items[PublicLiveExpirationOutputCachePolicy.CacheLifetimeItemKey] = cacheLifetime;
        this.Response.Headers.CacheControl = $"public,max-age={maxAgeSeconds},must-revalidate";
        this.Response.Headers.ETag = entityTag;
        this.Response.Headers.Vary = "Accept-Language";
        if (EntityTagMatcher.Matches(this.Request.Headers.IfNoneMatch, entityTag))
        {
            return this.StatusCode(StatusCodes.Status304NotModified);
        }

        return this.Ok(value);
    }
}
