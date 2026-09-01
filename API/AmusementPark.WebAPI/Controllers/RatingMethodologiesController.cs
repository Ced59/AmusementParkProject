using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.WebAPI.Contracts.Ratings;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("ratings/methodology")]
[AllowAnonymous]
public sealed class RatingMethodologiesController : ControllerBase
{
    private readonly IQueryHandler<GetCurrentRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>> currentHandler;
    private readonly IQueryHandler<GetRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>> versionHandler;
    private readonly IQueryHandler<ListRatingMethodologiesQuery, ApplicationResult<IReadOnlyCollection<RatingMethodologyResult>>> listHandler;

    public RatingMethodologiesController(
        IQueryHandler<GetCurrentRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>> currentHandler,
        IQueryHandler<GetRatingMethodologyQuery, ApplicationResult<RatingMethodologyResult>> versionHandler,
        IQueryHandler<ListRatingMethodologiesQuery, ApplicationResult<IReadOnlyCollection<RatingMethodologyResult>>> listHandler)
    {
        this.currentHandler = currentHandler;
        this.versionHandler = versionHandler;
        this.listHandler = listHandler;
    }

    [HttpGet]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicReferenceData)]
    [ProducesResponseType(typeof(IReadOnlyCollection<RatingMethodologyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<RatingMethodologyResult>> result =
            await this.listHandler.HandleAsync(new ListRatingMethodologiesQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.Select(static methodology => methodology.ToHttp()).ToList());
    }

    [HttpGet("current")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicReferenceData)]
    [ProducesResponseType(typeof(RatingMethodologyDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        ApplicationResult<RatingMethodologyResult> result =
            await this.currentHandler.HandleAsync(new GetCurrentRatingMethodologyQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{version}")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicReferenceData)]
    [ProducesResponseType(typeof(RatingMethodologyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByVersionAsync(
        [FromRoute] string version,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<RatingMethodologyResult> result =
            await this.versionHandler.HandleAsync(new GetRatingMethodologyQuery(version), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
