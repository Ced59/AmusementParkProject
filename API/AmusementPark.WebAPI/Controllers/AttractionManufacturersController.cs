using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Commands;
using AmusementPark.Application.Features.AttractionManufacturers.Queries;
using AmusementPark.Application.Features.AttractionManufacturers.Results;
using AmusementPark.Application.Common.Results;
using AmusementPark.WebAPI.Contracts.AttractionManufacturers;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la feature AttractionManufacturers migrée en phase 6.
/// </summary>
[ApiController]
[Route("attraction-manufacturers")]
[RequireActivatedUnblockedUser]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
public sealed class AttractionManufacturersController : ControllerBase
{
    private readonly IQueryHandler<GetAttractionManufacturersQuery, ApplicationResult<IReadOnlyCollection<AttractionManufacturerResult>>> getAttractionManufacturersQueryHandler;
    private readonly IQueryHandler<GetAttractionManufacturerByIdQuery, ApplicationResult<AttractionManufacturerResult>> getAttractionManufacturerByIdQueryHandler;
    private readonly ICommandHandler<CreateAttractionManufacturerCommand, ApplicationResult<AttractionManufacturerResult>> createAttractionManufacturerCommandHandler;
    private readonly ICommandHandler<UpdateAttractionManufacturerCommand, ApplicationResult<AttractionManufacturerResult>> updateAttractionManufacturerCommandHandler;
    private readonly ICommandHandler<UpdateAttractionManufacturersBulkReviewStatusCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateAttractionManufacturersBulkReviewStatusCommandHandler;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="AttractionManufacturersController"/>.
    /// </summary>
    public AttractionManufacturersController(
        IQueryHandler<GetAttractionManufacturersQuery, ApplicationResult<IReadOnlyCollection<AttractionManufacturerResult>>> getAttractionManufacturersQueryHandler,
        IQueryHandler<GetAttractionManufacturerByIdQuery, ApplicationResult<AttractionManufacturerResult>> getAttractionManufacturerByIdQueryHandler,
        ICommandHandler<CreateAttractionManufacturerCommand, ApplicationResult<AttractionManufacturerResult>> createAttractionManufacturerCommandHandler,
        ICommandHandler<UpdateAttractionManufacturerCommand, ApplicationResult<AttractionManufacturerResult>> updateAttractionManufacturerCommandHandler,
        ICommandHandler<UpdateAttractionManufacturersBulkReviewStatusCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateAttractionManufacturersBulkReviewStatusCommandHandler)
    {
        this.getAttractionManufacturersQueryHandler = getAttractionManufacturersQueryHandler;
        this.getAttractionManufacturerByIdQueryHandler = getAttractionManufacturerByIdQueryHandler;
        this.createAttractionManufacturerCommandHandler = createAttractionManufacturerCommandHandler;
        this.updateAttractionManufacturerCommandHandler = updateAttractionManufacturerCommandHandler;
        this.updateAttractionManufacturersBulkReviewStatusCommandHandler = updateAttractionManufacturersBulkReviewStatusCommandHandler;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<AttractionManufacturerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<AttractionManufacturerResult>> result = await this.getAttractionManufacturersQueryHandler.HandleAsync(new GetAttractionManufacturersQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<AttractionManufacturerDto> response = pagination.ToPagedResponse(result.Value, static value => value.ToHttp());
        return this.Ok(response);
    }

    [HttpPatch("bulk-review-status")]
    [ProducesResponseType(typeof(BulkAdministrationUpdateResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBulkReviewStatusAsync([FromBody] BulkAdministrationUpdateDto request, CancellationToken cancellationToken = default)
    {
        if (request.AdminReviewStatus is null)
        {
            return this.BadRequest("adminReviewStatus is required.");
        }

        ApplicationResult<BulkAdministrationUpdateResult> result = await this.updateAttractionManufacturersBulkReviewStatusCommandHandler.HandleAsync(
            new UpdateAttractionManufacturersBulkReviewStatusCommand(request.Ids, request.AdminReviewStatus.Value.ToDomain()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AttractionManufacturerDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        ApplicationResult<AttractionManufacturerResult> result = await this.getAttractionManufacturerByIdQueryHandler.HandleAsync(new GetAttractionManufacturerByIdQuery(id), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost]
    [ProducesResponseType(typeof(AttractionManufacturerDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromBody] AttractionManufacturerCreateDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<AttractionManufacturerResult> result = await this.createAttractionManufacturerCommandHandler.HandleAsync(new CreateAttractionManufacturerCommand(dto.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(AttractionManufacturerDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync([FromRoute] string id, [FromBody] AttractionManufacturerUpdateDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<AttractionManufacturerResult> result = await this.updateAttractionManufacturerCommandHandler.HandleAsync(new UpdateAttractionManufacturerCommand(id, dto.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
