using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFounders.Commands;
using AmusementPark.Application.Features.ParkFounders.Queries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkFounders;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la feature ParkFounders migrée en phase 6.
/// </summary>
[ApiController]
[Route("park-founders")]
[RequireActivatedUnblockedUser]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
public sealed class ParkFoundersController : ControllerBase
{
    private readonly IQueryHandler<GetParkFoundersQuery, ApplicationResult<IReadOnlyCollection<ParkFounder>>> getParkFoundersQueryHandler;
    private readonly IQueryHandler<GetParkFounderByIdQuery, ApplicationResult<ParkFounder>> getParkFounderByIdQueryHandler;
    private readonly ICommandHandler<CreateParkFounderCommand, ApplicationResult<ParkFounder>> createParkFounderCommandHandler;
    private readonly ICommandHandler<UpdateParkFounderCommand, ApplicationResult<ParkFounder>> updateParkFounderCommandHandler;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="ParkFoundersController"/>.
    /// </summary>
    public ParkFoundersController(
        IQueryHandler<GetParkFoundersQuery, ApplicationResult<IReadOnlyCollection<ParkFounder>>> getParkFoundersQueryHandler,
        IQueryHandler<GetParkFounderByIdQuery, ApplicationResult<ParkFounder>> getParkFounderByIdQueryHandler,
        ICommandHandler<CreateParkFounderCommand, ApplicationResult<ParkFounder>> createParkFounderCommandHandler,
        ICommandHandler<UpdateParkFounderCommand, ApplicationResult<ParkFounder>> updateParkFounderCommandHandler)
    {
        this.getParkFoundersQueryHandler = getParkFoundersQueryHandler;
        this.getParkFounderByIdQueryHandler = getParkFounderByIdQueryHandler;
        this.createParkFounderCommandHandler = createParkFounderCommandHandler;
        this.updateParkFounderCommandHandler = updateParkFounderCommandHandler;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<ParkFounderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<ParkFounder>> result = await this.getParkFoundersQueryHandler.HandleAsync(new GetParkFoundersQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<ParkFounderDto> response = pagination.ToPagedResponse(result.Value, static value => value.ToHttp());
        return this.Ok(response);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParkFounderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkFounder> result = await this.getParkFounderByIdQueryHandler.HandleAsync(new GetParkFounderByIdQuery(id), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost]
    [AdminAudit("park-founder.create", "ParkFounder")]
    [ProducesResponseType(typeof(ParkFounderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromBody] ParkFounderCreateDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkFounder> result = await this.createParkFounderCommandHandler.HandleAsync(new CreateParkFounderCommand(dto.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{id}")]
    [AdminAudit("park-founder.update", "ParkFounder", TargetIdRouteKey = "id")]
    [ProducesResponseType(typeof(ParkFounderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync([FromRoute] string id, [FromBody] ParkFounderUpdateDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkFounder> result = await this.updateParkFounderCommandHandler.HandleAsync(new UpdateParkFounderCommand(id, dto.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
