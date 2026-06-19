using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Queries;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkItems;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using AmusementPark.WebAPI.Authorization;
using AmusementPark.WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la feature ParkItems migrée en phase 8.
/// </summary>
[ApiController]
[Route("park-items")]
[RequireActivatedUnblockedUser]
[Authorize(Roles = AuthorizationRoleGroups.Admin)]
[InvalidatesPublicCache(PublicCacheScope.Data)]
public sealed class ParkItemsController : ControllerBase
{
    private readonly IQueryHandler<GetParkItemsByParkIdQuery, ApplicationResult<IReadOnlyCollection<ParkItem>>> getParkItemsByParkIdQueryHandler;
    private readonly IQueryHandler<GetParkItemsPageQuery, ApplicationResult<PagedResult<ParkItemAdminListResult>>> getParkItemsPageQueryHandler;
    private readonly IQueryHandler<GetParkItemByIdQuery, ApplicationResult<ParkItem>> getParkItemByIdQueryHandler;
    private readonly IQueryHandler<GetParkItemSiblingNavigationQuery, ApplicationResult<ParkItemSiblingNavigationResult>> getParkItemSiblingNavigationQueryHandler;
    private readonly IQueryHandler<GetRelatedParkItemsQuery, ApplicationResult<IReadOnlyCollection<ParkItem>>> getRelatedParkItemsQueryHandler;
    private readonly IQueryHandler<GetRatingSummaryQuery, ApplicationResult<RatingSummaryResult>> getRatingSummaryQueryHandler;
    private readonly ICommandHandler<CreateParkItemCommand, ApplicationResult<ParkItem>> createParkItemCommandHandler;
    private readonly ICommandHandler<UpdateParkItemCommand, ApplicationResult<ParkItem>> updateParkItemCommandHandler;
    private readonly ICommandHandler<DeleteParkItemCommand, ApplicationResult> deleteParkItemCommandHandler;
    private readonly ICommandHandler<UpdateParkItemsBulkAdministrationCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateParkItemsBulkAdministrationCommandHandler;
    private readonly ICommandHandler<UpdateParkItemsBulkFieldsCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateParkItemsBulkFieldsCommandHandler;
    private readonly ICommandHandler<PreviewParkItemsBulkCreateCommand, ApplicationResult<ParkItemsBulkCreatePreviewResult>> previewParkItemsBulkCreateCommandHandler;
    private readonly ICommandHandler<ApplyParkItemsBulkCreateCommand, ApplicationResult<ParkItemsBulkCreateApplyResult>> applyParkItemsBulkCreateCommandHandler;

    public ParkItemsController(
        IQueryHandler<GetParkItemsByParkIdQuery, ApplicationResult<IReadOnlyCollection<ParkItem>>> getParkItemsByParkIdQueryHandler,
        IQueryHandler<GetParkItemsPageQuery, ApplicationResult<PagedResult<ParkItemAdminListResult>>> getParkItemsPageQueryHandler,
        IQueryHandler<GetParkItemByIdQuery, ApplicationResult<ParkItem>> getParkItemByIdQueryHandler,
        IQueryHandler<GetParkItemSiblingNavigationQuery, ApplicationResult<ParkItemSiblingNavigationResult>> getParkItemSiblingNavigationQueryHandler,
        IQueryHandler<GetRelatedParkItemsQuery, ApplicationResult<IReadOnlyCollection<ParkItem>>> getRelatedParkItemsQueryHandler,
        IQueryHandler<GetRatingSummaryQuery, ApplicationResult<RatingSummaryResult>> getRatingSummaryQueryHandler,
        ICommandHandler<CreateParkItemCommand, ApplicationResult<ParkItem>> createParkItemCommandHandler,
        ICommandHandler<UpdateParkItemCommand, ApplicationResult<ParkItem>> updateParkItemCommandHandler,
        ICommandHandler<DeleteParkItemCommand, ApplicationResult> deleteParkItemCommandHandler,
        ICommandHandler<UpdateParkItemsBulkAdministrationCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateParkItemsBulkAdministrationCommandHandler,
        ICommandHandler<UpdateParkItemsBulkFieldsCommand, ApplicationResult<BulkAdministrationUpdateResult>> updateParkItemsBulkFieldsCommandHandler,
        ICommandHandler<PreviewParkItemsBulkCreateCommand, ApplicationResult<ParkItemsBulkCreatePreviewResult>> previewParkItemsBulkCreateCommandHandler,
        ICommandHandler<ApplyParkItemsBulkCreateCommand, ApplicationResult<ParkItemsBulkCreateApplyResult>> applyParkItemsBulkCreateCommandHandler)
    {
        this.getParkItemsByParkIdQueryHandler = getParkItemsByParkIdQueryHandler;
        this.getParkItemsPageQueryHandler = getParkItemsPageQueryHandler;
        this.getParkItemByIdQueryHandler = getParkItemByIdQueryHandler;
        this.getParkItemSiblingNavigationQueryHandler = getParkItemSiblingNavigationQueryHandler;
        this.getRelatedParkItemsQueryHandler = getRelatedParkItemsQueryHandler;
        this.getRatingSummaryQueryHandler = getRatingSummaryQueryHandler;
        this.createParkItemCommandHandler = createParkItemCommandHandler;
        this.updateParkItemCommandHandler = updateParkItemCommandHandler;
        this.deleteParkItemCommandHandler = deleteParkItemCommandHandler;
        this.updateParkItemsBulkAdministrationCommandHandler = updateParkItemsBulkAdministrationCommandHandler;
        this.updateParkItemsBulkFieldsCommandHandler = updateParkItemsBulkFieldsCommandHandler;
        this.previewParkItemsBulkCreateCommandHandler = previewParkItemsBulkCreateCommandHandler;
        this.applyParkItemsBulkCreateCommandHandler = applyParkItemsBulkCreateCommandHandler;
    }

    [HttpGet("park/{parkId}")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [AllowAnonymous]
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
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataShort)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<ParkItemAdminListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaginatedAsync(
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery] string? parkId = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? isVisible = null,
        [FromQuery] string? adminReviewStatus = null,
        [FromQuery] string? category = null,
        [FromQuery] string? type = null,
        [FromQuery] string? zoneId = null,
        [FromQuery] string? manufacturerId = null,
        [FromQuery] string? contentBacklogFilter = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        bool canSeeNonVisible = this.UserCanSeeNonVisible();
        bool? effectiveIsVisible = canSeeNonVisible ? isVisible : true;
        AdminReviewStatus? effectiveAdminReviewStatus = canSeeNonVisible ? ParseAdminReviewStatus(adminReviewStatus) : null;
        PagedQuery paging = pagination.ToApplication();
        ApplicationResult<PagedResult<ParkItemAdminListResult>> result = await this.getParkItemsPageQueryHandler.HandleAsync(
            new GetParkItemsPageQuery(
                paging,
                parkId,
                search,
                canSeeNonVisible,
                effectiveIsVisible,
                effectiveAdminReviewStatus,
                ParseParkItemCategory(category),
                ParseParkItemType(type),
                zoneId,
                manufacturerId,
                ParseParkItemContentBacklogFilter(contentBacklogFilter),
                ParseParkItemAdminSortField(sortBy),
                IsSortDescending(sortDirection)),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        PagedResponseDto<ParkItemAdminListDto> response = result.Value.ToPagedResponse(static item => item.ToHttp());

        return this.Ok(response);
    }

    [HttpGet("{id}/siblings")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParkItemSiblingNavigationDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSiblingNavigationAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkItemSiblingNavigationResult> result = await this.getParkItemSiblingNavigationQueryHandler.HandleAsync(
            new GetParkItemSiblingNavigationQuery(id, this.UserCanSeeNonVisible()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpGet("{id}/related")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyCollection<ParkItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRelatedAsync([FromRoute] string id, [FromQuery] int limit = 3, CancellationToken cancellationToken = default)
    {
        ApplicationResult<IReadOnlyCollection<ParkItem>> result = await this.getRelatedParkItemsQueryHandler.HandleAsync(
            new GetRelatedParkItemsQuery(id, limit, this.UserCanSeeNonVisible()),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.Select(static item => item.ToHttp()).ToList());
    }

    [HttpGet("{id}")]
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataMedium)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ParkItemDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByIdAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkItem> result = await this.getParkItemByIdQueryHandler.HandleAsync(new GetParkItemByIdQuery(id, this.UserCanSeeNonVisible()), cancellationToken);
        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        ApplicationResult<RatingSummaryResult> ratingResult = await this.getRatingSummaryQueryHandler.HandleAsync(
            new GetRatingSummaryQuery(RatingTargetType.ParkItem, result.Value.Id),
            cancellationToken);

        return this.Ok(result.Value.ToHttp(ratingResult.IsSuccess ? ratingResult.Value : null));
    }

    [HttpPost]
    [AdminAudit("park-item.create", "ParkItem")]
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
    [AdminAudit("park-item.update", "ParkItem", TargetIdRouteKey = "id")]
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
    [AdminAudit("park-item.bulk-administration.update", "ParkItem", StaticTargetId = "bulk")]
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

    [HttpPatch("bulk-fields")]
    [AdminAudit("park-item.bulk-fields.update", "ParkItem", StaticTargetId = "bulk")]
    [ProducesResponseType(typeof(BulkAdministrationUpdateResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateParkItemsBulkFieldsAsync([FromBody] ParkItemBulkFieldsUpdateDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<BulkAdministrationUpdateResult> result = await this.updateParkItemsBulkFieldsCommandHandler.HandleAsync(
            request.ToApplication(),
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

    [HttpPost("bulk-create/preview")]
    [AdminAudit("park-item.bulk-create.preview", "ParkItem", StaticTargetId = "bulk")]
    [ProducesResponseType(typeof(ParkItemsBulkCreatePreviewResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewParkItemsBulkCreateAsync([FromBody] ParkItemsBulkCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkItemsBulkCreatePreviewResult> result = await this.previewParkItemsBulkCreateCommandHandler.HandleAsync(
            request.ToPreviewApplication(),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpPost("bulk-create/apply")]
    [AdminAudit("park-item.bulk-create.apply", "ParkItem", StaticTargetId = "bulk")]
    [ProducesResponseType(typeof(ParkItemsBulkCreateApplyResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyParkItemsBulkCreateAsync([FromBody] ParkItemsBulkCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        ApplicationResult<ParkItemsBulkCreateApplyResult> result = await this.applyParkItemsBulkCreateCommandHandler.HandleAsync(
            request.ToApplyApplication(),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }

    [HttpDelete("{id}")]
    [AdminAudit("park-item.delete", "ParkItem", TargetIdRouteKey = "id")]
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


    private static ParkItemAdminSortField ParseParkItemAdminSortField(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ParkItemAdminSortField.Default;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "name" => ParkItemAdminSortField.Name,
            "category" => ParkItemAdminSortField.Category,
            "type" => ParkItemAdminSortField.Type,
            "isvisible" => ParkItemAdminSortField.IsVisible,
            "visibility" => ParkItemAdminSortField.IsVisible,
            "adminreviewstatus" => ParkItemAdminSortField.AdminReviewStatus,
            "status" => ParkItemAdminSortField.AdminReviewStatus,
            "parkid" => ParkItemAdminSortField.ParkId,
            "zoneid" => ParkItemAdminSortField.ZoneId,
            _ => ParkItemAdminSortField.Default,
        };
    }

    private static bool IsSortDescending(string? value)
    {
        return string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "descending", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "-1", StringComparison.OrdinalIgnoreCase);
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

    private static ParkItemContentBacklogFilter? ParseParkItemContentBacklogFilter(string? value)
    {
        return Enum.TryParse(value, true, out ParkItemContentBacklogFilter parsed) ? parsed : null;
    }

    private bool UserCanSeeNonVisible()
    {
        return this.User?.IsInRole("ADMIN") == true;
    }
}
