using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Countries;
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
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la feature Parks migrée en phase 7.
/// </summary>
[ApiController]
[Route("[controller]")]
[RequireActivatedUnblockedUser]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
public sealed class ParksController : ControllerBase
{
    private readonly ICommandHandler<CreateParkCommand, ApplicationResult<Park>> createParkCommandHandler;
    private readonly IQueryHandler<GetParkByIdQuery, ApplicationResult<Park>> getParkByIdQueryHandler;
    private readonly IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<Park>>> getParksPageQueryHandler;
    private readonly IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<Park>>> searchParksQueryHandler;
    private readonly IQueryHandler<SearchParksByLocationQuery, ApplicationResult<IReadOnlyCollection<Park>>> searchParksByLocationQueryHandler;
    private readonly IQueryHandler<CalculateParkDistancesQuery, ApplicationResult<ParkDistanceResult>> calculateParkDistancesQueryHandler;
    private readonly IQueryHandler<GetNearestParksQuery, ApplicationResult<ParkDistanceResult>> getNearestParksQueryHandler;
    private readonly IQueryHandler<GetVisibleParkMapPointsQuery, ApplicationResult<IReadOnlyCollection<Park>>> getVisibleParkMapPointsQueryHandler;
    private readonly IQueryHandler<GetRandomVisibleParksQuery, ApplicationResult<IReadOnlyCollection<Park>>> getRandomVisibleParksQueryHandler;
    private readonly IQueryHandler<GetHomeFeaturedParksQuery, ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>>> getHomeFeaturedParksQueryHandler;
    private readonly ICommandHandler<UpdateParkCommand, ApplicationResult<Park>> updateParkCommandHandler;
    private readonly ICommandHandler<UpdateParkVisibilityCommand, ApplicationResult<Park>> updateParkVisibilityCommandHandler;
    private readonly ICommandHandler<UpdateParksBulkAdministrationCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateParksBulkAdministrationCommandHandler;

    public ParksController(
        ICommandHandler<CreateParkCommand, ApplicationResult<Park>> createParkCommandHandler,
        IQueryHandler<GetParkByIdQuery, ApplicationResult<Park>> getParkByIdQueryHandler,
        IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<Park>>> getParksPageQueryHandler,
        IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<Park>>> searchParksQueryHandler,
        IQueryHandler<SearchParksByLocationQuery, ApplicationResult<IReadOnlyCollection<Park>>> searchParksByLocationQueryHandler,
        IQueryHandler<CalculateParkDistancesQuery, ApplicationResult<ParkDistanceResult>> calculateParkDistancesQueryHandler,
        IQueryHandler<GetNearestParksQuery, ApplicationResult<ParkDistanceResult>> getNearestParksQueryHandler,
        IQueryHandler<GetVisibleParkMapPointsQuery, ApplicationResult<IReadOnlyCollection<Park>>> getVisibleParkMapPointsQueryHandler,
        IQueryHandler<GetRandomVisibleParksQuery, ApplicationResult<IReadOnlyCollection<Park>>> getRandomVisibleParksQueryHandler,
        IQueryHandler<GetHomeFeaturedParksQuery, ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>>> getHomeFeaturedParksQueryHandler,
        ICommandHandler<UpdateParkCommand, ApplicationResult<Park>> updateParkCommandHandler,
        ICommandHandler<UpdateParkVisibilityCommand, ApplicationResult<Park>> updateParkVisibilityCommandHandler,
        ICommandHandler<UpdateParksBulkAdministrationCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateParksBulkAdministrationCommandHandler)
    {
        this.createParkCommandHandler = createParkCommandHandler;
        this.getParkByIdQueryHandler = getParkByIdQueryHandler;
        this.getParksPageQueryHandler = getParksPageQueryHandler;
        this.searchParksQueryHandler = searchParksQueryHandler;
        this.searchParksByLocationQueryHandler = searchParksByLocationQueryHandler;
        this.calculateParkDistancesQueryHandler = calculateParkDistancesQueryHandler;
        this.getNearestParksQueryHandler = getNearestParksQueryHandler;
        this.getVisibleParkMapPointsQueryHandler = getVisibleParkMapPointsQueryHandler;
        this.getRandomVisibleParksQueryHandler = getRandomVisibleParksQueryHandler;
        this.getHomeFeaturedParksQueryHandler = getHomeFeaturedParksQueryHandler;
        this.updateParkCommandHandler = updateParkCommandHandler;
        this.updateParkVisibilityCommandHandler = updateParkVisibilityCommandHandler;
        this.updateParksBulkAdministrationCommandHandler = updateParksBulkAdministrationCommandHandler;
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyCollection<ParkMapPointDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVisibleParkMapPointsAsync([FromQuery] string? query = null, [FromQuery] string? name = null, [FromQuery] string? region = null, CancellationToken cancellationToken = default)
    {
        string? effectiveQuery = string.IsNullOrWhiteSpace(query) ? name : query;
        WorldRegionFilter? regionFilter = WorldRegionFilterParser.Parse(region);

        ApplicationResult<IReadOnlyCollection<Park>> result = await this.getVisibleParkMapPointsQueryHandler.HandleAsync(
            new GetVisibleParkMapPointsQuery(effectiveQuery, regionFilter),
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
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParkDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParkById([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        ApplicationResult<Park> result = await this.getParkByIdQueryHandler.HandleAsync(new GetParkByIdQuery(id, this.UserCanSeeNonVisible()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<ParkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParksAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? query = null,
        [FromQuery] string? name = null,
        [FromQuery] string? region = null,
        [FromQuery] bool visibleOnly = false,
        [FromQuery] bool? isVisible = null,
        [FromQuery] string? adminReviewStatus = null,
        [FromQuery] string? type = null,
        [FromQuery] string? countryCode = null,
        CancellationToken cancellationToken = default)
    {
        bool canSeeNonVisible = this.UserCanSeeNonVisible();
        bool includeNonVisible = !visibleOnly && canSeeNonVisible;
        PagedQuery paging = pagination.ToApplication();
        string? effectiveQuery = string.IsNullOrWhiteSpace(query) ? name : query;
        WorldRegionFilter? regionFilter = WorldRegionFilterParser.Parse(region);

        bool? effectiveIsVisible = canSeeNonVisible ? isVisible : true;
        AdminReviewStatus? parsedAdminReviewStatus = canSeeNonVisible ? ParseAdminReviewStatus(adminReviewStatus) : null;
        ParkType? parsedType = ParseParkType(type);

        ApplicationResult<PagedResult<Park>> result = string.IsNullOrWhiteSpace(effectiveQuery) && regionFilter is null
            ? await this.getParksPageQueryHandler.HandleAsync(new GetParksPageQuery(paging, includeNonVisible, effectiveIsVisible, parsedAdminReviewStatus, parsedType, countryCode), cancellationToken)
            : await this.searchParksQueryHandler.HandleAsync(new SearchParksQuery(effectiveQuery, regionFilter, paging, includeNonVisible, effectiveIsVisible, parsedAdminReviewStatus, parsedType, countryCode), cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<ParkDto> response = result.Value.ToPagedResponse(static park => park.ToHttp());

        return this.Ok(response);
    }

    [HttpGet("geo-search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<ParkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchParksByLocationAsync(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] double? radiusMeters = null,
        [FromQuery] double? radiusKilometers = null,
        [FromQuery] double? radius = null,
        CancellationToken cancellationToken = default)
    {
        double radiusInKilometers = ResolveRadiusInKilometers(radiusMeters, radiusKilometers, radius);
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

    [HttpGet("{id}/distances")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParkDistanceResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParkDistancesAsync(
        [FromRoute] string id,
        [FromQuery] string[] targetParkIds,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkDistanceResult> result = await this.calculateParkDistancesQueryHandler.HandleAsync(
            new CalculateParkDistancesQuery(id, targetParkIds, this.UserCanSeeNonVisible()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToDistanceHttp());
    }

    [HttpGet("{id}/nearby")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParkDistanceResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNearestParksAsync(
        [FromRoute] string id,
        [FromQuery] int limit = 4,
        [FromQuery] double? maxDistanceKilometers = null,
        CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkDistanceResult> result = await this.getNearestParksQueryHandler.HandleAsync(
            new GetNearestParksQuery(id, limit, maxDistanceKilometers, this.UserCanSeeNonVisible()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToDistanceHttp());
    }

    [HttpPatch("bulk-administration")]
    [ProducesResponseType(typeof(BulkAdministrationUpdateResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateParksBulkAdministrationAsync([FromBody] BulkAdministrationUpdateDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<BulkAdministrationUpdateResult> result = await this.updateParksBulkAdministrationCommandHandler.HandleAsync(
            new UpdateParksBulkAdministrationCommand(request.Ids, request.IsVisible, request.AdminReviewStatus.ToOptionalDomain()),
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

    private static AdminReviewStatus? ParseAdminReviewStatus(string? value)
    {
        return Enum.TryParse(value, true, out AdminReviewStatus parsed) ? parsed : null;
    }

    private static ParkType? ParseParkType(string? value)
    {
        return Enum.TryParse(value, true, out ParkType parsed) ? parsed : null;
    }

    private static double ResolveRadiusInKilometers(double? radiusMeters, double? radiusKilometers, double? legacyRadiusMeters)
    {
        if (radiusKilometers.HasValue)
        {
            return Math.Max(0d, radiusKilometers.Value);
        }

        if (radiusMeters.HasValue)
        {
            return Math.Max(0d, radiusMeters.Value) / 1000d;
        }

        if (legacyRadiusMeters.HasValue)
        {
            return Math.Max(0d, legacyRadiusMeters.Value) / 1000d;
        }

        return 50d;
    }

    private bool UserCanSeeNonVisible()
    {
        return this.User?.IsInRole("ADMIN") == true;
    }
}
