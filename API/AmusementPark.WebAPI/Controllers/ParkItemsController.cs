using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Queries;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la feature ParkItems migrée en phase 8.
/// </summary>
[ApiController]
[Route("park-items")]
public sealed class ParkItemsController : ControllerBase
{
    private readonly IQueryHandler<GetParkItemsByParkIdQuery, ApplicationResult<IReadOnlyCollection<ParkItem>>> getParkItemsByParkIdQueryHandler;
    private readonly IQueryHandler<GetParkItemsPageQuery, ApplicationResult<PagedResult<ParkItemAdminListResult>>> getParkItemsPageQueryHandler;
    private readonly IQueryHandler<GetParkItemByIdQuery, ApplicationResult<ParkItem>> getParkItemByIdQueryHandler;
    private readonly ICommandHandler<CreateParkItemCommand, ApplicationResult<ParkItem>> createParkItemCommandHandler;
    private readonly ICommandHandler<UpdateParkItemCommand, ApplicationResult<ParkItem>> updateParkItemCommandHandler;
    private readonly ICommandHandler<DeleteParkItemCommand, ApplicationResult> deleteParkItemCommandHandler;
    private readonly ICommandHandler<UpdateParkItemsBulkAdministrationCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateParkItemsBulkAdministrationCommandHandler;

    public ParkItemsController(
        IQueryHandler<GetParkItemsByParkIdQuery, ApplicationResult<IReadOnlyCollection<ParkItem>>> getParkItemsByParkIdQueryHandler,
        IQueryHandler<GetParkItemsPageQuery, ApplicationResult<PagedResult<ParkItemAdminListResult>>> getParkItemsPageQueryHandler,
        IQueryHandler<GetParkItemByIdQuery, ApplicationResult<ParkItem>> getParkItemByIdQueryHandler,
        ICommandHandler<CreateParkItemCommand, ApplicationResult<ParkItem>> createParkItemCommandHandler,
        ICommandHandler<UpdateParkItemCommand, ApplicationResult<ParkItem>> updateParkItemCommandHandler,
        ICommandHandler<DeleteParkItemCommand, ApplicationResult> deleteParkItemCommandHandler,
        ICommandHandler<UpdateParkItemsBulkAdministrationCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateParkItemsBulkAdministrationCommandHandler)
    {
        this.getParkItemsByParkIdQueryHandler = getParkItemsByParkIdQueryHandler;
        this.getParkItemsPageQueryHandler = getParkItemsPageQueryHandler;
        this.getParkItemByIdQueryHandler = getParkItemByIdQueryHandler;
        this.createParkItemCommandHandler = createParkItemCommandHandler;
        this.updateParkItemCommandHandler = updateParkItemCommandHandler;
        this.deleteParkItemCommandHandler = deleteParkItemCommandHandler;
        this.updateParkItemsBulkAdministrationCommandHandler = updateParkItemsBulkAdministrationCommandHandler;
    }

    [HttpGet("park/{parkId}")]
    [ProducesResponseType(typeof(PagedResponseDto<ParkItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByParkIdAsync([FromRoute] string parkId, [FromQuery] PaginationRequestDto pagination, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<ParkItem>> result = await this.getParkItemsByParkIdQueryHandler.HandleAsync(
            new GetParkItemsByParkIdQuery(parkId, this.UserCanSeeNonVisible()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<ParkItemDto> response = pagination.ToPagedResponse(result.Value, static item => item.ToHttp());
        return this.Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<ParkItemAdminListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaginatedAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? parkId = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? isVisible = null,
        [FromQuery] string? adminReviewStatus = null,
        [FromQuery] string? category = null,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        PagedQuery paging = pagination.ToApplication();
        ApplicationResult<PagedResult<ParkItemAdminListResult>> result = await this.getParkItemsPageQueryHandler.HandleAsync(
            new GetParkItemsPageQuery(
                paging,
                parkId,
                search,
                this.UserCanSeeNonVisible(),
                isVisible,
                ParseAdminReviewStatus(adminReviewStatus),
                ParseParkItemCategory(category),
                ParseParkItemType(type)),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<ParkItemAdminListDto> response = result.Value.ToPagedResponse(static item => item.ToHttp());

        return this.Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ParkItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkItem> result = await this.getParkItemByIdQueryHandler.HandleAsync(new GetParkItemByIdQuery(id), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost]
    [ProducesResponseType(typeof(ParkItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromBody] ParkItemCreateDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkItem> result = await this.createParkItemCommandHandler.HandleAsync(new CreateParkItemCommand(dto.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ParkItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync([FromRoute] string id, [FromBody] ParkItemUpdateDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkItem> result = await this.updateParkItemCommandHandler.HandleAsync(new UpdateParkItemCommand(id, dto.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPatch("bulk-administration")]
    [ProducesResponseType(typeof(BulkAdministrationUpdateResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateParkItemsBulkAdministrationAsync([FromBody] BulkAdministrationUpdateDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<BulkAdministrationUpdateResult> result = await this.updateParkItemsBulkAdministrationCommandHandler.HandleAsync(
            new UpdateParkItemsBulkAdministrationCommand(request.Ids, request.IsVisible, request.AdminReviewStatus.ToOptionalDomain()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(new BulkAdministrationUpdateResultDto
        {
            RequestedCount = result.Value.RequestedCount,
            UpdatedCount = result.Value.UpdatedCount,
        });
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        ApplicationResult result = await this.deleteParkItemCommandHandler.HandleAsync(new DeleteParkItemCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(true);
    }

    private static AdminReviewStatus? ParseAdminReviewStatus(string? value)
    {
        return Enum.TryParse(value, true, out AdminReviewStatus parsed) ? parsed : null;
    }

    private static ParkItemCategory? ParseParkItemCategory(string? value)
    {
        return Enum.TryParse(value, true, out ParkItemCategory parsed) ? parsed : null;
    }

    private static ParkItemType? ParseParkItemType(string? value)
    {
        return Enum.TryParse(value, true, out ParkItemType parsed) ? parsed : null;
    }

    private bool UserCanSeeNonVisible()
    {
        return this.User?.IsInRole("ADMIN") == true || this.User?.IsInRole("MODERATOR") == true;
    }
}
