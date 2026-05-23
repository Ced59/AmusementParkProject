using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOperators.Commands;
using AmusementPark.Application.Features.ParkOperators.Queries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkOperators;
using AmusementPark.Application.Common.Results;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la feature ParkOperators migrée en phase 6.
/// </summary>
[ApiController]
[Route("park-operators")]
[RequireActivatedUnblockedUser]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
public sealed class ParkOperatorsController : ControllerBase
{
    private readonly IQueryHandler<GetParkOperatorsQuery, ApplicationResult<IReadOnlyCollection<ParkOperator>>> getParkOperatorsQueryHandler;
    private readonly IQueryHandler<GetParkOperatorByIdQuery, ApplicationResult<ParkOperator>> getParkOperatorByIdQueryHandler;
    private readonly ICommandHandler<CreateParkOperatorCommand, ApplicationResult<ParkOperator>> createParkOperatorCommandHandler;
    private readonly ICommandHandler<UpdateParkOperatorCommand, ApplicationResult<ParkOperator>> updateParkOperatorCommandHandler;
    private readonly ICommandHandler<UpdateParkOperatorsBulkReviewStatusCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateParkOperatorsBulkReviewStatusCommandHandler;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="ParkOperatorsController"/>.
    /// </summary>
    public ParkOperatorsController(
        IQueryHandler<GetParkOperatorsQuery, ApplicationResult<IReadOnlyCollection<ParkOperator>>> getParkOperatorsQueryHandler,
        IQueryHandler<GetParkOperatorByIdQuery, ApplicationResult<ParkOperator>> getParkOperatorByIdQueryHandler,
        ICommandHandler<CreateParkOperatorCommand, ApplicationResult<ParkOperator>> createParkOperatorCommandHandler,
        ICommandHandler<UpdateParkOperatorCommand, ApplicationResult<ParkOperator>> updateParkOperatorCommandHandler,
        ICommandHandler<UpdateParkOperatorsBulkReviewStatusCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateParkOperatorsBulkReviewStatusCommandHandler)
    {
        this.getParkOperatorsQueryHandler = getParkOperatorsQueryHandler;
        this.getParkOperatorByIdQueryHandler = getParkOperatorByIdQueryHandler;
        this.createParkOperatorCommandHandler = createParkOperatorCommandHandler;
        this.updateParkOperatorCommandHandler = updateParkOperatorCommandHandler;
        this.updateParkOperatorsBulkReviewStatusCommandHandler = updateParkOperatorsBulkReviewStatusCommandHandler;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<ParkOperatorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAsync([FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<ParkOperator>> result = await this.getParkOperatorsQueryHandler.HandleAsync(new GetParkOperatorsQuery(), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<ParkOperatorDto> response = pagination.ToPagedResponse(result.Value, static value => value.ToHttp());
        return this.Ok(response);
    }

    [HttpPatch("bulk-review-status")]
    [ProducesResponseType(typeof(BulkAdministrationUpdateResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBulkReviewStatusAsync([FromBody] BulkAdministrationUpdateDto request, CancellationToken cancellationToken = default)
    {
        if (request.AdminReviewStatus is null)
        {
            return this.ToProblemDetailsResult(StatusCodes.Status400BadRequest, "adminReviewStatus is required.", "admin-review-status.required");
        }

        ApplicationResult<BulkAdministrationUpdateResult> result = await this.updateParkOperatorsBulkReviewStatusCommandHandler.HandleAsync(
            new UpdateParkOperatorsBulkReviewStatusCommand(request.Ids, request.AdminReviewStatus.Value.ToDomain()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParkOperatorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkOperator> result = await this.getParkOperatorByIdQueryHandler.HandleAsync(new GetParkOperatorByIdQuery(id), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost]
    [ProducesResponseType(typeof(ParkOperatorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromBody] ParkOperatorCreateDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkOperator> result = await this.createParkOperatorCommandHandler.HandleAsync(new CreateParkOperatorCommand(dto.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ParkOperatorDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync([FromRoute] string id, [FromBody] ParkOperatorUpdateDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkOperator> result = await this.updateParkOperatorCommandHandler.HandleAsync(new UpdateParkOperatorCommand(id, dto.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
