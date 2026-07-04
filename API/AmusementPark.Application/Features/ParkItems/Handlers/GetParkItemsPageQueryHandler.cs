using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Queries;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Services;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class GetParkItemsPageQueryHandler : IQueryHandler<GetParkItemsPageQuery, ApplicationResult<PagedResult<ParkItemAdminListResult>>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkRepository parkRepository;
    private readonly PagedQueryValidator pagedQueryValidator;
    private readonly ParkItemContentQualityService contentQualityService;
    private readonly IParkZoneRepository? parkZoneRepository;
    private readonly IImageRepository? imageRepository;
    private readonly IHistoryEventRepository? historyEventRepository;

    public GetParkItemsPageQueryHandler(
        IParkItemRepository parkItemRepository,
        IParkRepository parkRepository,
        PagedQueryValidator pagedQueryValidator,
        ParkItemContentQualityService contentQualityService,
        IParkZoneRepository? parkZoneRepository = null,
        IImageRepository? imageRepository = null,
        IHistoryEventRepository? historyEventRepository = null)
    {
        this.parkItemRepository = parkItemRepository;
        this.parkRepository = parkRepository;
        this.pagedQueryValidator = pagedQueryValidator;
        this.contentQualityService = contentQualityService;
        this.parkZoneRepository = parkZoneRepository;
        this.imageRepository = imageRepository;
        this.historyEventRepository = historyEventRepository;
    }

    public async Task<ApplicationResult<PagedResult<ParkItemAdminListResult>>> HandleAsync(GetParkItemsPageQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<ParkItemAdminListResult>>.Failure(errors);
        }

        if (RequiresApplicationLevelPage(query.SortField, query.IncludeHidden))
        {
            PagedResult<ParkItemAdminListResult> sortedResult = await this.LoadApplicationLevelPageAsync(query, cancellationToken);
            return ApplicationResult<PagedResult<ParkItemAdminListResult>>.Success(sortedResult);
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

        PagedResult<ParkItemAdminListResult> result = await this.EnrichAsync(page, query.IncludeHidden, cancellationToken);
        return ApplicationResult<PagedResult<ParkItemAdminListResult>>.Success(result);
    }

    private async Task<PagedResult<ParkItemAdminListResult>> LoadApplicationLevelPageAsync(GetParkItemsPageQuery query, CancellationToken cancellationToken)
    {
        PagedResult<ParkItem> countProbe = await this.parkItemRepository.GetPageAsync(
            1,
            1,
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
            cancellationToken);

        if (countProbe.TotalItems == 0)
        {
            return new PagedResult<ParkItemAdminListResult>(new List<ParkItemAdminListResult>(), query.Paging.Page, query.Paging.PageSize, 0);
        }

        int allPageSize = checked((int)Math.Min(countProbe.TotalItems, int.MaxValue));
        PagedResult<ParkItem> allItems = await this.parkItemRepository.GetPageAsync(
            1,
            allPageSize,
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
            ParkItemAdminSortField.Default,
            false);

        PagedResult<ParkItemAdminListResult> enrichedPage = await this.EnrichAsync(allItems, true, cancellationToken);
        List<ParkItemAdminListResult> sortedItems = SortApplicationLevel(enrichedPage.Items, query.SortField, query.SortDescending);
        List<ParkItemAdminListResult> pagedItems = sortedItems
            .Skip((query.Paging.Page - 1) * query.Paging.PageSize)
            .Take(query.Paging.PageSize)
            .ToList();

        return new PagedResult<ParkItemAdminListResult>(pagedItems, query.Paging.Page, query.Paging.PageSize, enrichedPage.TotalItems);
    }

    private async Task<PagedResult<ParkItemAdminListResult>> EnrichAsync(PagedResult<ParkItem> page, bool includeDataCompleteness, CancellationToken cancellationToken)
    {
        List<string> parkIds = page.Items
            .Where(static item => !string.IsNullOrWhiteSpace(item.ParkId))
            .Select(static item => item.ParkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        IReadOnlyCollection<Park> parks = await this.parkRepository.GetByIdsAsync(parkIds, cancellationToken);
        Dictionary<string, string> parkNamesById = parks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .ToDictionary(static park => park.Id, static park => park.Name ?? string.Empty, StringComparer.Ordinal);
        Dictionary<string, Park> parksById = parks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .ToDictionary(static park => park.Id!.Trim(), static park => park, StringComparer.Ordinal);
        IReadOnlyDictionary<string, ParkItemDataCompletenessContext> dataCompletenessContexts = includeDataCompleteness
            ? await DataCompletenessContextFactory.BuildParkItemContextsAsync(
                page.Items,
                parksById,
                this.parkZoneRepository,
                this.imageRepository,
                this.historyEventRepository,
                cancellationToken)
            : new Dictionary<string, ParkItemDataCompletenessContext>(StringComparer.Ordinal);

        List<ParkItemAdminListResult> items = page.Items
            .Select(item =>
            {
                ParkItemDataCompletenessContext? dataCompletenessContext = !string.IsNullOrWhiteSpace(item.Id) && dataCompletenessContexts.TryGetValue(item.Id, out ParkItemDataCompletenessContext? resolvedContext)
                    ? resolvedContext
                    : null;

                return new ParkItemAdminListResult
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
                    DataCompleteness = includeDataCompleteness ? item.CalculateDataCompletenessScore(dataCompletenessContext) : null,
                };
            })
            .ToList();

        return new PagedResult<ParkItemAdminListResult>(items, page.Page, page.PageSize, page.TotalItems);
    }

    private static bool RequiresApplicationLevelPage(ParkItemAdminSortField sortField, bool includeHidden)
    {
        return includeHidden && RequiresApplicationLevelSort(sortField);
    }

    private static bool RequiresApplicationLevelSort(ParkItemAdminSortField sortField)
    {
        return sortField == ParkItemAdminSortField.DataCompletenessScore;
    }

    private static List<ParkItemAdminListResult> SortApplicationLevel(IReadOnlyCollection<ParkItemAdminListResult> items, ParkItemAdminSortField sortField, bool sortDescending)
    {
        Func<ParkItemAdminListResult, int> keySelector = sortField switch
        {
            ParkItemAdminSortField.DataCompletenessScore => static item => item.DataCompleteness?.CompletenessScore ?? 0,
            _ => static item => 0,
        };

        IOrderedEnumerable<ParkItemAdminListResult> orderedItems = sortDescending
            ? items.OrderByDescending(keySelector)
            : items.OrderBy(keySelector);

        return orderedItems
            .ThenBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static item => item.Id, StringComparer.Ordinal)
            .ToList();
    }
}
