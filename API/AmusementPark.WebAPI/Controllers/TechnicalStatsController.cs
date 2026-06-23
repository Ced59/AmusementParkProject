using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.TechnicalStats.Commands;
using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.Application.Features.TechnicalStats.Queries;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.TechnicalStats;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("admin/technical-stats")]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[RequireActivatedUnblockedUser]
public sealed class TechnicalStatsController : ControllerBase
{
    private readonly IQueryHandler<GetTechnicalStatsQuery, ApplicationResult<TechnicalStatsSnapshot>> getTechnicalStatsQueryHandler;
    private readonly ICommandHandler<UpdateTechnicalStatsSettingsCommand, ApplicationResult<TechnicalStatsSettings>> updateTechnicalStatsSettingsCommandHandler;

    public TechnicalStatsController(
        IQueryHandler<GetTechnicalStatsQuery, ApplicationResult<TechnicalStatsSnapshot>> getTechnicalStatsQueryHandler,
        ICommandHandler<UpdateTechnicalStatsSettingsCommand, ApplicationResult<TechnicalStatsSettings>> updateTechnicalStatsSettingsCommandHandler)
    {
        this.getTechnicalStatsQueryHandler = getTechnicalStatsQueryHandler;
        this.updateTechnicalStatsSettingsCommandHandler = updateTechnicalStatsSettingsCommandHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TechnicalStatsSnapshotDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken = default)
    {
        ApplicationResult<TechnicalStatsSnapshot> result = await this.getTechnicalStatsQueryHandler.HandleAsync(new GetTechnicalStatsQuery(), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("settings")]
    [AdminAudit("technical-stats.settings.update", "TechnicalStats")]
    [ProducesResponseType(typeof(TechnicalStatsSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettingsAsync([FromBody] UpdateTechnicalStatsSettingsDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<TechnicalStatsSettings> result = await this.updateTechnicalStatsSettingsCommandHandler.HandleAsync(
            new UpdateTechnicalStatsSettingsCommand(request.ToApplication()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
