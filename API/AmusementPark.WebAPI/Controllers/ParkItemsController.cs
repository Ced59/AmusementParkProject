using System.ComponentModel.DataAnnotations;
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

    public ParkItemsController(
        IQueryHandler<GetParkItemsByParkIdQuery, ApplicationResult<IReadOnlyCollection<ParkItem>>> getParkItemsByParkIdQueryHandler,
        IQueryHandler<GetParkItemsPageQuery, ApplicationResult<PagedResult<ParkItemAdminListResult>>> getParkItemsPageQueryHandler,
        IQueryHandler<GetParkItemByIdQuery, ApplicationResult<ParkItem>> getParkItemByIdQueryHandler,
        ICommandHandler<CreateParkItemCommand, ApplicationResult<ParkItem>> createParkItemCommandHandler,
        ICommandHandler<UpdateParkItemCommand, ApplicationResult<ParkItem>> updateParkItemCommandHandler,
        ICommandHandler<DeleteParkItemCommand, ApplicationResult> deleteParkItemCommandHandler)
    {
        this.getParkItemsByParkIdQueryHandler = getParkItemsByParkIdQueryHandler;
        this.getParkItemsPageQueryHandler = getParkItemsPageQueryHandler;
        this.getParkItemByIdQueryHandler = getParkItemByIdQueryHandler;
        this.createParkItemCommandHandler = createParkItemCommandHandler;
        this.updateParkItemCommandHandler = updateParkItemCommandHandler;
        this.deleteParkItemCommandHandler = deleteParkItemCommandHandler;
    }

    [HttpGet("park/{parkId}")]
    [ProducesResponseType(typeof(IEnumerable<ParkItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByParkIdAsync([FromRoute] string parkId, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<ParkItem>> result = await this.getParkItemsByParkIdQueryHandler.HandleAsync(
            new GetParkItemsByParkIdQuery(parkId, this.UserCanSeeNonVisible()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        List<ParkItemDto> response = result.Value.Select(static item => item.ToHttp()).ToList();
        return this.Ok(response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<ParkItemAdminListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaginatedAsync(
        [FromQuery][Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")] int page = 1,
        [FromQuery][Range(1, 100, ErrorMessage = "Size must be between 1 and 100")] int size = 20,
        [FromQuery] string? parkId = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        PagedQuery paging = new PagedQuery(page, size);
        ApplicationResult<PagedResult<ParkItemAdminListResult>> result = await this.getParkItemsPageQueryHandler.HandleAsync(
            new GetParkItemsPageQuery(paging, parkId, search, this.UserCanSeeNonVisible()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<ParkItemAdminListDto> response = new PagedResponseDto<ParkItemAdminListDto>
        {
            Data = result.Value.Items.Select(static item => item.ToHttp()).ToList(),
            Pagination = result.Value.ToHttp(),
        };

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

    private bool UserCanSeeNonVisible()
    {
        return this.User?.IsInRole("ADMIN") == true || this.User?.IsInRole("MODERATOR") == true;
    }
}
