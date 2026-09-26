using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
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
[AllowAnonymous]
[Route("public/parks/{parkId}/history")]
public sealed class PublicParkHistoryController : ControllerBase
{
    private readonly IQueryHandler<
        GetPublicParkHistoricalTimelineQuery,
        ApplicationResult<PublicParkHistoricalTimelineResult>> timelineHandler;
    private readonly IQueryHandler<
        GetPublicParkHistoricalSnapshotQuery,
        ApplicationResult<PublicParkHistoricalSnapshotResult>> snapshotHandler;

    public PublicParkHistoryController(
        IQueryHandler<
            GetPublicParkHistoricalTimelineQuery,
            ApplicationResult<PublicParkHistoricalTimelineResult>> timelineHandler,
        IQueryHandler<
            GetPublicParkHistoricalSnapshotQuery,
            ApplicationResult<PublicParkHistoricalSnapshotResult>> snapshotHandler)
    {
        this.timelineHandler = timelineHandler;
        this.snapshotHandler = snapshotHandler;
    }

    [HttpGet("timeline")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [ProducesResponseType(typeof(PublicParkHistoricalTimelineDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimelineAsync(
        [FromRoute] string parkId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetPublicParkHistoricalTimelineQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<PublicParkHistoricalTimelineResult> result =
            await this.timelineHandler.HandleAsync(
                new GetPublicParkHistoricalTimelineQuery(parkId, page, pageSize),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }

    [HttpGet("snapshot")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [ProducesResponseType(typeof(PublicParkHistoricalSnapshotDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSnapshotAsync(
        [FromRoute] string parkId,
        [FromQuery] int year,
        [FromQuery] int? month = null,
        [FromQuery] int? day = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<PublicParkHistoricalSnapshotResult> result =
            await this.snapshotHandler.HandleAsync(
                new GetPublicParkHistoricalSnapshotQuery(parkId, year, month, day),
                cancellationToken);
        return result.IsSuccess && result.Value is not null
            ? this.Ok(result.Value.ToHttp())
            : this.ToActionResult(result);
    }
}
