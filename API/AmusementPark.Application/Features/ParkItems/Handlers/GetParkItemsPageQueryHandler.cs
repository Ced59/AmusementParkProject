using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Queries;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class GetParkItemsPageQueryHandler : IQueryHandler<GetParkItemsPageQuery, ApplicationResult<PagedResult<ParkItemAdminListResult>>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkRepository parkRepository;
    private readonly PagedQueryValidator pagedQueryValidator;
    private readonly ParkItemContentQualityService contentQualityService;

    public GetParkItemsPageQueryHandler(IParkItemRepository parkItemRepository, IParkRepository parkRepository, PagedQueryValidator pagedQueryValidator, ParkItemContentQualityService contentQualityService)
    {
        this.parkItemRepository = parkItemRepository;
        this.parkRepository = parkRepository;
        this.pagedQueryValidator = pagedQueryValidator;
        this.contentQualityService = contentQualityService;
    }

    public async Task<ApplicationResult<PagedResult<ParkItemAdminListResult>>> HandleAsync(GetParkItemsPageQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<ParkItemAdminListResult>>.Failure(errors);
        }

        PagedResult<ParkItem> page = await this.parkItemRepository.GetPageAsync(
            query.Paging.Page,
            query.Paging.PageSize,
            query.ParkId,
            query.Search,
            query.IncludeHidden,
            query.IsVisible,
            query.AdminReviewStatus,
            query.Category,
            query.Type,
            query.ZoneId,
            query.ManufacturerId,
            query.ContentBacklogFilter,
            cancellationToken,
            query.SortField,
            query.SortDescending);

        List<string> parkIds = page.Items
            .Where(static item => !string.IsNullOrWhiteSpace(item.ParkId))
            .Select(static item => item.ParkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        IReadOnlyCollection<Park> parks = await this.parkRepository.GetByIdsAsync(parkIds, cancellationToken);
        Dictionary<string, string> parkNamesById = parks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .ToDictionary(static park => park.Id, static park => park.Name ?? string.Empty, StringComparer.Ordinal);

        List<ParkItemAdminListResult> items = page.Items
            .Select(item => new ParkItemAdminListResult
            {
                Id = item.Id,
                ParkId = item.ParkId,
                ParkName = parkNamesById.TryGetValue(item.ParkId, out string? parkName) ? parkName : string.Empty,
                ZoneId = item.ZoneId,
                Name = item.Name,
                Category = item.Category,
                Type = item.Type,
                IsVisible = item.IsVisible,
                AdminReviewStatus = item.AdminReviewStatus,
                ContentQuality = this.contentQualityService.Evaluate(item),
                PublicationSignals = this.contentQualityService.BuildPublicationSignals(item),
            })
            .ToList();

        PagedResult<ParkItemAdminListResult> result = new PagedResult<ParkItemAdminListResult>(items, page.Page, page.PageSize, page.TotalItems);
        return ApplicationResult<PagedResult<ParkItemAdminListResult>>.Success(result);
    }
}
