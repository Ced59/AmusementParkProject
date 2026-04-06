using System.ComponentModel.DataAnnotations;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Parks;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la feature Parks migrée en phase 7.
/// </summary>
[ApiController]
[Route("[controller]")]
public sealed class ParksController : ControllerBase
{
    private readonly ICommandHandler<CreateParkCommand, ApplicationResult<Park>> createParkCommandHandler;
    private readonly IQueryHandler<GetParkByIdQuery, ApplicationResult<Park>> getParkByIdQueryHandler;
    private readonly IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<Park>>> getParksPageQueryHandler;
    private readonly IQueryHandler<SearchParksByNameQuery, ApplicationResult<PagedResult<Park>>> searchParksByNameQueryHandler;
    private readonly IQueryHandler<SearchParksByLocationQuery, ApplicationResult<IReadOnlyCollection<Park>>> searchParksByLocationQueryHandler;
    private readonly ICommandHandler<UpdateParkCommand, ApplicationResult<Park>> updateParkCommandHandler;
    private readonly ICommandHandler<UpdateParkVisibilityCommand, ApplicationResult<Park>> updateParkVisibilityCommandHandler;

    public ParksController(
        ICommandHandler<CreateParkCommand, ApplicationResult<Park>> createParkCommandHandler,
        IQueryHandler<GetParkByIdQuery, ApplicationResult<Park>> getParkByIdQueryHandler,
        IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<Park>>> getParksPageQueryHandler,
        IQueryHandler<SearchParksByNameQuery, ApplicationResult<PagedResult<Park>>> searchParksByNameQueryHandler,
        IQueryHandler<SearchParksByLocationQuery, ApplicationResult<IReadOnlyCollection<Park>>> searchParksByLocationQueryHandler,
        ICommandHandler<UpdateParkCommand, ApplicationResult<Park>> updateParkCommandHandler,
        ICommandHandler<UpdateParkVisibilityCommand, ApplicationResult<Park>> updateParkVisibilityCommandHandler)
    {
        this.createParkCommandHandler = createParkCommandHandler;
        this.getParkByIdQueryHandler = getParkByIdQueryHandler;
        this.getParksPageQueryHandler = getParksPageQueryHandler;
        this.searchParksByNameQueryHandler = searchParksByNameQueryHandler;
        this.searchParksByLocationQueryHandler = searchParksByLocationQueryHandler;
        this.updateParkCommandHandler = updateParkCommandHandler;
        this.updateParkVisibilityCommandHandler = updateParkVisibilityCommandHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ParkCreatedDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateParkAsync([FromBody] ParkCreateDto park, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Park> result = await this.createParkCommandHandler.HandleAsync(new CreateParkCommand(park.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToCreatedHttp());
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ParkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParkById([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Park> result = await this.getParkByIdQueryHandler.HandleAsync(new GetParkByIdQuery(id, true), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<ParkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParksAsync(
        [FromQuery][Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        int page = 1,
        [FromQuery][Range(1, 100, ErrorMessage = "Size must be between 1 and 100")]
        int size = 10,
        [FromQuery] string? name = null,
        CancellationToken cancellationToken = default)
    {
        bool includeNonVisible = this.UserCanSeeNonVisible();
        PagedQuery paging = new PagedQuery(page, size);

        ApplicationResult<PagedResult<Park>> result = string.IsNullOrWhiteSpace(name)
            ? await this.getParksPageQueryHandler.HandleAsync(new GetParksPageQuery(paging, includeNonVisible), cancellationToken)
            : await this.searchParksByNameQueryHandler.HandleAsync(new SearchParksByNameQuery(name.Trim(), paging, includeNonVisible), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<ParkDto> response = new PagedResponseDto<ParkDto>
        {
            Data = result.Value.Items.Select(static park => park.ToHttp()).ToList(),
            Pagination = result.Value.ToHttp(),
        };

        return this.Ok(response);
    }

    [HttpGet("geo-search")]
    [ProducesResponseType(typeof(IEnumerable<ParkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchParksByLocationAsync(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radius,
        CancellationToken cancellationToken = default)
    {
        double radiusInKilometers = radius / 1000d;
        ApplicationResult<IReadOnlyCollection<Park>> result = await this.searchParksByLocationQueryHandler.HandleAsync(
            new SearchParksByLocationQuery(latitude, longitude, radiusInKilometers, false),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        List<ParkDto> response = result.Value.Select(static park => park.ToHttp()).ToList();
        return this.Ok(response);
    }

    [HttpPatch("{id}/visibility")]
    [ProducesResponseType(typeof(ParkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateParkVisibilityAsync([FromRoute] string id, [FromBody] ParkVisibilityUpdateDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Park> result = await this.updateParkVisibilityCommandHandler.HandleAsync(new UpdateParkVisibilityCommand(id, request.IsVisible), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ParkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateParkAsync([FromRoute] string id, [FromBody] ParkUpdateDto park, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Park> result = await this.updateParkCommandHandler.HandleAsync(new UpdateParkCommand(id, park.ToDomain()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    private bool UserCanSeeNonVisible()
    {
        return this.User?.IsInRole("ADMIN") == true || this.User?.IsInRole("MODERATOR") == true;
    }
}
