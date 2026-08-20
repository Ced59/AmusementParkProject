using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.WebAPI.AdminPublicView;
using AmusementPark.WebAPI.Contracts.History;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("history/standalone-attractions")]
public sealed class StandaloneAttractionHistoryController : ControllerBase
{
    private readonly IQueryHandler<GetStandaloneAttractionHistoryTimelineQuery, ApplicationResult<StandaloneAttractionHistoryTimelineResult>> getTimelineHandler;

    public StandaloneAttractionHistoryController(
        IQueryHandler<GetStandaloneAttractionHistoryTimelineQuery, ApplicationResult<StandaloneAttractionHistoryTimelineResult>> getTimelineHandler)
    {
        this.getTimelineHandler = getTimelineHandler;
    }

    [HttpGet("{standaloneAttractionId}")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [ProducesResponseType(typeof(StandaloneAttractionHistoryTimelineDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimelineAsync(
        [FromRoute] string standaloneAttractionId,
        [FromQuery] int page = HistoryTimelinePaging.DefaultPage,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<StandaloneAttractionHistoryTimelineResult> result = await this.getTimelineHandler.HandleAsync(
            new GetStandaloneAttractionHistoryTimelineQuery(
                standaloneAttractionId,
                this.HttpContext.UserCanSeeNonVisibleInPublicView(),
                page,
                HistoryTimelinePaging.DefaultPageSize),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
