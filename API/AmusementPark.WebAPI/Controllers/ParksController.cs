using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Parks;
using AmusementPark.WebAPI.Contracts.Home;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Http;
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
    private readonly IQueryHandler<GetVisibleParkMapPointsQuery, ApplicationResult<IReadOnlyCollection<Park>>> getVisibleParkMapPointsQueryHandler;
    private readonly IQueryHandler<GetRandomVisibleParksQuery, ApplicationResult<IReadOnlyCollection<Park>>> getRandomVisibleParksQueryHandler;
    private readonly IQueryHandler<GetHomeFeaturedParksQuery, ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>>> getHomeFeaturedParksQueryHandler;
    private readonly ICommandHandler<UpdateParkCommand, ApplicationResult<Park>> updateParkCommandHandler;
    private readonly ICommandHandler<UpdateParkVisibilityCommand, ApplicationResult<Park>> updateParkVisibilityCommandHandler;

    public ParksController(
        ICommandHandler<CreateParkCommand, ApplicationResult<Park>> createParkCommandHandler,
        IQueryHandler<GetParkByIdQuery, ApplicationResult<Park>> getParkByIdQueryHandler,
        IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<Park>>> getParksPageQueryHandler,
        IQueryHandler<SearchParksByNameQuery, ApplicationResult<PagedResult<Park>>> searchParksByNameQueryHandler,
        IQueryHandler<SearchParksByLocationQuery, ApplicationResult<IReadOnlyCollection<Park>>> searchParksByLocationQueryHandler,
        IQueryHandler<GetVisibleParkMapPointsQuery, ApplicationResult<IReadOnlyCollection<Park>>> getVisibleParkMapPointsQueryHandler,
        IQueryHandler<GetRandomVisibleParksQuery, ApplicationResult<IReadOnlyCollection<Park>>> getRandomVisibleParksQueryHandler,
        IQueryHandler<GetHomeFeaturedParksQuery, ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>>> getHomeFeaturedParksQueryHandler,
        ICommandHandler<UpdateParkCommand, ApplicationResult<Park>> updateParkCommandHandler,
        ICommandHandler<UpdateParkVisibilityCommand, ApplicationResult<Park>> updateParkVisibilityCommandHandler)
    {
        this.createParkCommandHandler = createParkCommandHandler;
        this.getParkByIdQueryHandler = getParkByIdQueryHandler;
        this.getParksPageQueryHandler = getParksPageQueryHandler;
        this.searchParksByNameQueryHandler = searchParksByNameQueryHandler;
        this.searchParksByLocationQueryHandler = searchParksByLocationQueryHandler;
        this.getVisibleParkMapPointsQueryHandler = getVisibleParkMapPointsQueryHandler;
        this.getRandomVisibleParksQueryHandler = getRandomVisibleParksQueryHandler;
        this.getHomeFeaturedParksQueryHandler = getHomeFeaturedParksQueryHandler;
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

    [HttpGet("random-visible")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ParkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRandomVisibleParksAsync([FromQuery] int limit = 4, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<Park>> result = await this.getRandomVisibleParksQueryHandler.HandleAsync(
            new GetRandomVisibleParksQuery(limit),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        List<ParkDto> response = result.Value.Select(static park => park.ToHttp()).ToList();
        return this.Ok(response);
    }


    [HttpGet("home-featured")]
    [ProducesResponseType(typeof(IReadOnlyCollection<HomeFeaturedParkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHomeFeaturedParksAsync([FromQuery] int limit = 3, [FromQuery] string[]? excludeIds = null, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>> result = await this.getHomeFeaturedParksQueryHandler.HandleAsync(
            new GetHomeFeaturedParksQuery(limit, excludeIds ?? Array.Empty<string>()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        List<HomeFeaturedParkDto> response = result.Value.Select(static park => park.ToHttp()).ToList();
        return this.Ok(response);
    }

    [HttpGet("map-visible")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ParkMapPointDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVisibleParkMapPointsAsync([FromQuery] string? name = null, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<Park>> result = await this.getVisibleParkMapPointsQueryHandler.HandleAsync(
            new GetVisibleParkMapPointsQuery(name),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        List<ParkMapPointDto> response = result.Value
            .Where(static park => park.Position is not null)
            .Select(static park => park.ToMapPointHttp())
            .ToList();

        return this.Ok(response);
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
    public async Task<IActionResult> GetParksAsync([FromQuery] PaginationRequestDto pagination, [FromQuery] string? name = null, [FromQuery] bool visibleOnly = false, CancellationToken cancellationToken = default)
    {
        bool includeNonVisible = !visibleOnly && this.UserCanSeeNonVisible();
        PagedQuery paging = pagination.ToApplication();

        ApplicationResult<PagedResult<Park>> result = string.IsNullOrWhiteSpace(name)
            ? await this.getParksPageQueryHandler.HandleAsync(new GetParksPageQuery(paging, includeNonVisible), cancellationToken)
            : await this.searchParksByNameQueryHandler.HandleAsync(new SearchParksByNameQuery(name.Trim(), paging, includeNonVisible), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<ParkDto> response = result.Value.ToPagedResponse(static park => park.ToHttp());

        return this.Ok(response);
    }

    [HttpGet("geo-search")]
    [ProducesResponseType(typeof(PagedResponseDto<ParkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchParksByLocationAsync(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radius,
        [FromQuery] PaginationRequestDto pagination,
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

        PagedResponseDto<ParkDto> response = pagination.ToPagedResponse(result.Value, static park => park.ToHttp());
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
