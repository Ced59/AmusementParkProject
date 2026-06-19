using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkWeather.Commands;
using AmusementPark.Application.Features.ParkWeather.Queries;
using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.ParkWeather;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
public sealed class ParkWeatherController : ControllerBase
{
    private readonly IQueryHandler<GetParkWeatherForecastQuery, ApplicationResult<ParkWeatherForecastResult>> getParkWeatherForecastQueryHandler;
    private readonly IQueryHandler<GetParkWeatherHistoricalComparisonsQuery, ApplicationResult<ParkWeatherHistoricalComparisonsResult>> getParkWeatherHistoricalComparisonsQueryHandler;
    private readonly IQueryHandler<GetLatestParkWeatherRunQuery, ApplicationResult<ParkWeatherRunResult?>> getLatestParkWeatherRunQueryHandler;
    private readonly IQueryHandler<GetParkWeatherRunQuery, ApplicationResult<ParkWeatherRunResult>> getParkWeatherRunQueryHandler;
    private readonly IQueryHandler<GetParkWeatherRunItemsQuery, ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>> getParkWeatherRunItemsQueryHandler;
    private readonly ICommandHandler<StartParkWeatherManualRefreshCommand, ApplicationResult<ParkWeatherRunResult>> startParkWeatherManualRefreshCommandHandler;
    private readonly ICommandHandler<RetryFailedParkWeatherRunCommand, ApplicationResult<ParkWeatherRunResult>> retryFailedParkWeatherRunCommandHandler;
    private readonly ICommandHandler<RefreshSingleParkWeatherCommand, ApplicationResult<ParkWeatherRunResult>> refreshSingleParkWeatherCommandHandler;

    public ParkWeatherController(
        IQueryHandler<GetParkWeatherForecastQuery, ApplicationResult<ParkWeatherForecastResult>> getParkWeatherForecastQueryHandler,
        IQueryHandler<GetParkWeatherHistoricalComparisonsQuery, ApplicationResult<ParkWeatherHistoricalComparisonsResult>> getParkWeatherHistoricalComparisonsQueryHandler,
        IQueryHandler<GetLatestParkWeatherRunQuery, ApplicationResult<ParkWeatherRunResult?>> getLatestParkWeatherRunQueryHandler,
        IQueryHandler<GetParkWeatherRunQuery, ApplicationResult<ParkWeatherRunResult>> getParkWeatherRunQueryHandler,
        IQueryHandler<GetParkWeatherRunItemsQuery, ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>> getParkWeatherRunItemsQueryHandler,
        ICommandHandler<StartParkWeatherManualRefreshCommand, ApplicationResult<ParkWeatherRunResult>> startParkWeatherManualRefreshCommandHandler,
        ICommandHandler<RetryFailedParkWeatherRunCommand, ApplicationResult<ParkWeatherRunResult>> retryFailedParkWeatherRunCommandHandler,
        ICommandHandler<RefreshSingleParkWeatherCommand, ApplicationResult<ParkWeatherRunResult>> refreshSingleParkWeatherCommandHandler)
    {
        this.getParkWeatherForecastQueryHandler = getParkWeatherForecastQueryHandler;
        this.getParkWeatherHistoricalComparisonsQueryHandler = getParkWeatherHistoricalComparisonsQueryHandler;
        this.getLatestParkWeatherRunQueryHandler = getLatestParkWeatherRunQueryHandler;
        this.getParkWeatherRunQueryHandler = getParkWeatherRunQueryHandler;
        this.getParkWeatherRunItemsQueryHandler = getParkWeatherRunItemsQueryHandler;
        this.startParkWeatherManualRefreshCommandHandler = startParkWeatherManualRefreshCommandHandler;
        this.retryFailedParkWeatherRunCommandHandler = retryFailedParkWeatherRunCommandHandler;
        this.refreshSingleParkWeatherCommandHandler = refreshSingleParkWeatherCommandHandler;
    }

    [HttpGet("parks/{parkId}/weather")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicWeatherDataShort)]
    [ProducesResponseType(typeof(ParkWeatherForecastDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParkWeatherForecastAsync(
        [FromRoute] string parkId,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkWeatherForecastResult> result = await this.getParkWeatherForecastQueryHandler.HandleAsync(
            new GetParkWeatherForecastQuery(parkId, days),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("parks/{parkId}/weather/historical-comparisons")]
    [AllowAnonymous]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicWeatherDataShort)]
    [ProducesResponseType(typeof(ParkWeatherHistoricalComparisonsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParkWeatherHistoricalComparisonsAsync(
        [FromRoute] string parkId,
        [FromQuery] int days = 7,
        [FromQuery] int years = 10,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkWeatherHistoricalComparisonsResult> result = await this.getParkWeatherHistoricalComparisonsQueryHandler.HandleAsync(
            new GetParkWeatherHistoricalComparisonsQuery(parkId, days, years),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("admin/park-weather/runs/latest")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(ParkWeatherRunDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetLatestRunAsync(CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkWeatherRunResult?> result = await this.getLatestParkWeatherRunQueryHandler.HandleAsync(new GetLatestParkWeatherRunQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        if (result.Value is null)
        {
            return this.NoContent();
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("admin/park-weather/runs/{runId}")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(ParkWeatherRunDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRunAsync([FromRoute] string runId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkWeatherRunResult> result = await this.getParkWeatherRunQueryHandler.HandleAsync(new GetParkWeatherRunQuery(runId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("admin/park-weather/runs/{runId}/items")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [ProducesResponseType(typeof(IReadOnlyCollection<ParkWeatherRunItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRunItemsAsync([FromRoute] string runId, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>> result = await this.getParkWeatherRunItemsQueryHandler.HandleAsync(
            new GetParkWeatherRunItemsQuery(runId, status),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.Select(static item => item.ToHttp()).ToList());
    }

    [HttpPost("admin/park-weather/refresh")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("park-weather.refresh.start", "ParkWeather", StaticTargetId = "full")]
    [ProducesResponseType(typeof(ParkWeatherRunDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> StartManualRefreshAsync(CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkWeatherRunResult> result = await this.startParkWeatherManualRefreshCommandHandler.HandleAsync(new StartParkWeatherManualRefreshCommand(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Accepted(result.Value.ToHttp());
    }

    [HttpPost("admin/park-weather/runs/{runId}/retry-failed")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("park-weather.refresh.retry-failed", "ParkWeather", TargetIdRouteKey = "runId")]
    [ProducesResponseType(typeof(ParkWeatherRunDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RetryFailedAsync([FromRoute] string runId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkWeatherRunResult> result = await this.retryFailedParkWeatherRunCommandHandler.HandleAsync(new RetryFailedParkWeatherRunCommand(runId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Accepted(result.Value.ToHttp());
    }

    [HttpPost("admin/park-weather/parks/{parkId}/refresh")]
    [Authorize(Roles = AuthorizationRoleGroups.Admin)]
    [RequireActivatedUnblockedUser]
    [AdminAudit("park-weather.refresh.single", "ParkWeather", TargetIdRouteKey = "parkId")]
    [ProducesResponseType(typeof(ParkWeatherRunDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> RefreshParkAsync([FromRoute] string parkId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkWeatherRunResult> result = await this.refreshSingleParkWeatherCommandHandler.HandleAsync(new RefreshSingleParkWeatherCommand(parkId), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Accepted(result.Value.ToHttp());
    }
}
