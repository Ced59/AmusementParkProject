using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkZones.Commands;
using AmusementPark.Application.Features.ParkZones.Queries;
using AmusementPark.Application.Features.ParkZones.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkZones;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la feature ParkZones migrée en phase 7.
/// </summary>
[ApiController]
[Route("park-zones")]
[RequireActivatedUnblockedUser]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
public sealed class ParkZonesController : ControllerBase
{
    private readonly IQueryHandler<GetParkZonesByParkIdQuery, ApplicationResult<IReadOnlyCollection<ParkZone>>> getParkZonesByParkIdQueryHandler;
    private readonly IQueryHandler<GetParkZoneByIdQuery, ApplicationResult<ParkZone>> getParkZoneByIdQueryHandler;
    private readonly IQueryHandler<GetParkExplorerQuery, ApplicationResult<ParkExplorerResult>> getParkExplorerQueryHandler;
    private readonly ICommandHandler<CreateParkZoneCommand, ApplicationResult<ParkZone>> createParkZoneCommandHandler;
    private readonly ICommandHandler<UpdateParkZoneCommand, ApplicationResult<ParkZone>> updateParkZoneCommandHandler;
    private readonly ICommandHandler<DeleteParkZoneCommand, ApplicationResult> deleteParkZoneCommandHandler;

    public ParkZonesController(
        IQueryHandler<GetParkZonesByParkIdQuery, ApplicationResult<IReadOnlyCollection<ParkZone>>> getParkZonesByParkIdQueryHandler,
        IQueryHandler<GetParkZoneByIdQuery, ApplicationResult<ParkZone>> getParkZoneByIdQueryHandler,
        IQueryHandler<GetParkExplorerQuery, ApplicationResult<ParkExplorerResult>> getParkExplorerQueryHandler,
        ICommandHandler<CreateParkZoneCommand, ApplicationResult<ParkZone>> createParkZoneCommandHandler,
        ICommandHandler<UpdateParkZoneCommand, ApplicationResult<ParkZone>> updateParkZoneCommandHandler,
        ICommandHandler<DeleteParkZoneCommand, ApplicationResult> deleteParkZoneCommandHandler)
    {
        this.getParkZonesByParkIdQueryHandler = getParkZonesByParkIdQueryHandler;
        this.getParkZoneByIdQueryHandler = getParkZoneByIdQueryHandler;
        this.getParkExplorerQueryHandler = getParkExplorerQueryHandler;
        this.createParkZoneCommandHandler = createParkZoneCommandHandler;
        this.updateParkZoneCommandHandler = updateParkZoneCommandHandler;
        this.deleteParkZoneCommandHandler = deleteParkZoneCommandHandler;
    }

    [HttpGet("park/{parkId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<ParkZoneDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByParkIdAsync([FromRoute] string parkId, [FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<ParkZone>> result = await this.getParkZonesByParkIdQueryHandler.HandleAsync(new GetParkZonesByParkIdQuery(parkId, this.UserCanSeeNonVisible()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        IReadOnlyCollection<ParkZone> orderedZones = result.Value
            .OrderBy(static zone => zone.SortOrder)
            .ThenBy(static zone => zone.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        PagedResponseDto<ParkZoneDto> response = pagination.ToPagedResponse(orderedZones, static zone => zone.ToHttp());
        return this.Ok(response);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParkZoneDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkZone> result = await this.getParkZoneByIdQueryHandler.HandleAsync(new GetParkZoneByIdQuery(id, this.UserCanSeeNonVisible()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("park/{parkId}/explorer")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParkExplorerDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExplorerAsync([FromRoute] string parkId, CancellationToken cancellationToken = default)
    {
        bool includeNonVisible = this.UserCanSeeNonVisible();
        ApplicationResult<ParkExplorerResult> result = await this.getParkExplorerQueryHandler.HandleAsync(new GetParkExplorerQuery(parkId, includeNonVisible), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost]
    [ProducesResponseType(typeof(ParkZoneDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromBody] ParkZoneCreateDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkZone> result = await this.createParkZoneCommandHandler.HandleAsync(new CreateParkZoneCommand(dto.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ParkZoneDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync([FromRoute] string id, [FromBody] ParkZoneUpdateDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkZone> result = await this.updateParkZoneCommandHandler.HandleAsync(new UpdateParkZoneCommand(id, dto.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.deleteParkZoneCommandHandler.HandleAsync(new DeleteParkZoneCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(true);
    }

    private bool UserCanSeeNonVisible()
    {
        return this.User?.IsInRole("ADMIN") == true;
    }
}
