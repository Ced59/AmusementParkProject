using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.StandaloneAttractions.Commands;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.AdminPublicView;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Contracts.StandaloneAttractions;
using AmusementPark.WebAPI.Filters;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace AmusementPark.WebAPI.Controllers;

[ApiController]
[Route("standalone-attractions")]
[RequireActivatedUnblockedUser]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[InvalidatesPublicCache(PublicCacheScope.Data)]
public sealed class StandaloneAttractionsController : ControllerBase
{
    private readonly IQueryHandler<GetStandaloneAttractionsPageQuery, ApplicationResult<PagedResult<StandaloneAttraction>>> getPageQueryHandler;
    private readonly IQueryHandler<GetStandaloneAttractionByIdQuery, ApplicationResult<StandaloneAttraction>> getByIdQueryHandler;
    private readonly ICommandHandler<CreateStandaloneAttractionCommand, ApplicationResult<StandaloneAttraction>> createCommandHandler;
    private readonly ICommandHandler<UpdateStandaloneAttractionCommand, ApplicationResult<StandaloneAttraction>> updateCommandHandler;
    private readonly ICommandHandler<UpdateStandaloneAttractionsBulkAdministrationCommand, ApplicationResult<BulkAdministrationUpdateResult>> bulkAdministrationCommandHandler;
    private readonly ICommandHandler<MigrateParkToStandaloneAttractionCommand, ApplicationResult<StandaloneAttraction>> migrateCommandHandler;

    public StandaloneAttractionsController(
        IQueryHandler<GetStandaloneAttractionsPageQuery, ApplicationResult<PagedResult<StandaloneAttraction>>> getPageQueryHandler,
        IQueryHandler<GetStandaloneAttractionByIdQuery, ApplicationResult<StandaloneAttraction>> getByIdQueryHandler,
        ICommandHandler<CreateStandaloneAttractionCommand, ApplicationResult<StandaloneAttraction>> createCommandHandler,
        ICommandHandler<UpdateStandaloneAttractionCommand, ApplicationResult<StandaloneAttraction>> updateCommandHandler,
        ICommandHandler<UpdateStandaloneAttractionsBulkAdministrationCommand, ApplicationResult<BulkAdministrationUpdateResult>> bulkAdministrationCommandHandler,
        ICommandHandler<MigrateParkToStandaloneAttractionCommand, ApplicationResult<StandaloneAttraction>> migrateCommandHandler)
    {
        this.getPageQueryHandler = getPageQueryHandler;
        this.getByIdQueryHandler = getByIdQueryHandler;
        this.createCommandHandler = createCommandHandler;
        this.updateCommandHandler = updateCommandHandler;
        this.bulkAdministrationCommandHandler = bulkAdministrationCommandHandler;
        this.migrateCommandHandler = migrateCommandHandler;
    }

    [HttpGet]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataShort)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<StandaloneAttractionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaginatedAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? search = null,
        [FromQuery] bool? isVisible = null,
        [FromQuery] string? adminReviewStatus = null,
        [FromQuery] string? type = null,
        [FromQuery] string? countryCode = null,
        [FromQuery] string? manufacturerId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        bool canSeeNonVisible = this.UserCanSeeNonVisible();
        PagedQuery paging = pagination.ToApplication();
        ApplicationResult<PagedResult<StandaloneAttraction>> result = await this.getPageQueryHandler.HandleAsync(
            new GetStandaloneAttractionsPageQuery(
                paging,
                search,
                canSeeNonVisible,
                canSeeNonVisible ? isVisible : true,
                canSeeNonVisible ? ParseAdminReviewStatus(adminReviewStatus) : null,
                ParseParkItemType(type),
                countryCode,
                manufacturerId,
                canSeeNonVisible ? ParseSortField(sortBy) : StandaloneAttractionAdminSortField.Default,
                IsSortDescending(sortDirection)),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static item => item.ToHttp()));
    }

    [HttpGet("{id}")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(StandaloneAttractionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        ApplicationResult<StandaloneAttraction> result = await this.getByIdQueryHandler.HandleAsync(
            new GetStandaloneAttractionByIdQuery(id, this.UserCanSeeNonVisible()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost]
    [AdminAudit("standalone-attraction.create", "StandaloneAttraction")]
    [ProducesResponseType(typeof(StandaloneAttractionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateAsync([FromBody] StandaloneAttractionCreateDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<StandaloneAttraction> result = await this.createCommandHandler.HandleAsync(
            new CreateStandaloneAttractionCommand(dto.ToDomain()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPut("{id}")]
    [AdminAudit("standalone-attraction.update", "StandaloneAttraction", TargetIdRouteKey = "id")]
    [ProducesResponseType(typeof(StandaloneAttractionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateAsync([FromRoute] string id, [FromBody] StandaloneAttractionUpdateDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationResult<StandaloneAttraction> result = await this.updateCommandHandler.HandleAsync(
            new UpdateStandaloneAttractionCommand(id, dto.ToDomain()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPatch("bulk-administration")]
    [AdminAudit("standalone-attraction.bulk-administration.update", "StandaloneAttraction", StaticTargetId = "bulk")]
    [ProducesResponseType(typeof(BulkAdministrationUpdateResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateBulkAdministrationAsync([FromBody] BulkAdministrationUpdateDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<BulkAdministrationUpdateResult> result = await this.bulkAdministrationCommandHandler.HandleAsync(
            new UpdateStandaloneAttractionsBulkAdministrationCommand(request.Ids, request.IsVisible, request.AdminReviewStatus.ToOptionalDomain()),
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

    [HttpPost("migrate-from-park")]
    [AdminAudit("standalone-attraction.migrate-from-park", "StandaloneAttraction")]
    [InvalidatesPublicCache(PublicCacheScope.Data, PublicCacheScope.Seo, EvictOutputCache = false)]
    [ProducesResponseType(typeof(StandaloneAttractionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> MigrateFromParkAsync([FromBody] StandaloneAttractionMigrationDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<StandaloneAttraction> result = await this.migrateCommandHandler.HandleAsync(
            new MigrateParkToStandaloneAttractionCommand(request.ToApplication()),
            cancellationToken);

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

    private static ParkItemType? ParseParkItemType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse(value, true, out ParkItemType parsed)
            ? parsed
            : Enum.TryParse(value.Trim(), true, out ParkItemTypeDto dto) ? dto.ToDomainForStandaloneController() : null;
    }

    private static StandaloneAttractionAdminSortField ParseSortField(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "name" => StandaloneAttractionAdminSortField.Name,
            "type" => StandaloneAttractionAdminSortField.Type,
            "countrycode" or "country" => StandaloneAttractionAdminSortField.CountryCode,
            "isvisible" or "visibility" => StandaloneAttractionAdminSortField.IsVisible,
            "adminreviewstatus" or "status" => StandaloneAttractionAdminSortField.AdminReviewStatus,
            _ => StandaloneAttractionAdminSortField.Default,
        };
    }

    private static bool IsSortDescending(string? value)
    {
        return string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "descending", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "-1", StringComparison.OrdinalIgnoreCase);
    }

    private bool UserCanSeeNonVisible()
    {
        return this.HttpContext.UserCanSeeNonVisibleInPublicView();
    }
}

file static class StandaloneAttractionsControllerMappings
{
    public static ParkItemType ToDomainForStandaloneController(this ParkItemTypeDto value)
    {
        return Enum.TryParse(value.ToString(), out ParkItemType parsed) ? parsed : ParkItemType.Attraction;
    }
}
