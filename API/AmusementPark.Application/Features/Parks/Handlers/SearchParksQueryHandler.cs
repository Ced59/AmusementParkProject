using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de recherche publique unifiée des parcs.
/// </summary>
public sealed class SearchParksQueryHandler : IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<ParkListResult>>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly ICountryReferenceService countryReferenceService;
    private readonly PagedQueryValidator pagedQueryValidator;

    public SearchParksQueryHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        ICountryReferenceService countryReferenceService,
        PagedQueryValidator pagedQueryValidator)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.countryReferenceService = countryReferenceService;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<ParkListResult>>> HandleAsync(SearchParksQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<ParkListResult>>.Failure(errors);
        }

        ParkSearchCriteria criteria = await this.BuildCriteriaAsync(query, cancellationToken);
        if (RequiresCountSort(query.SortField))
        {
            PagedResult<ParkListResult> countSortedResult = await this.LoadCountSortedPageAsync(query, criteria, cancellationToken);
            return ApplicationResult<PagedResult<ParkListResult>>.Success(countSortedResult);
        }

        PagedResult<Park> page = await this.parkRepository.SearchAsync(
            criteria,
            query.Paging.Page,
            query.Paging.PageSize,
            query.IncludeHidden,
            query.IsVisible,
            query.AdminReviewStatus,
            query.Type,
            query.CountryCode,
            query.HasValidCoordinates,
            query.ClosedFilter,
            cancellationToken,
            query.SortField,
            query.SortDescending);

        PagedResult<ParkListResult> result = await this.EnrichAsync(page, query.IncludeHidden, cancellationToken);
        return ApplicationResult<PagedResult<ParkListResult>>.Success(result);
    }

    private async Task<ParkSearchCriteria> BuildCriteriaAsync(SearchParksQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> matchingCountryCodes = await this.countryReferenceService.FindCountryCodesByLocalizedSearchAsync(query.SearchTerm, cancellationToken);
        IReadOnlyCollection<string> regionCountryCodes = this.countryReferenceService.GetCountryCodesForRegion(query.Region);

        return new ParkSearchCriteria(
            query.SearchTerm,
            matchingCountryCodes,
            regionCountryCodes);
    }

    private async Task<PagedResult<ParkListResult>> LoadCountSortedPageAsync(SearchParksQuery query, ParkSearchCriteria criteria, CancellationToken cancellationToken)
    {
        PagedResult<Park> countProbe = await this.parkRepository.SearchAsync(
            criteria,
            1,
            1,
            query.IncludeHidden,
            query.IsVisible,
            query.AdminReviewStatus,
            query.Type,
            query.CountryCode,
            query.HasValidCoordinates,
            query.ClosedFilter,
            cancellationToken);

        if (countProbe.TotalItems == 0)
        {
            return new PagedResult<ParkListResult>(new List<ParkListResult>(), query.Paging.Page, query.Paging.PageSize, 0);
        }

        int allPageSize = checked((int)Math.Min(countProbe.TotalItems, int.MaxValue));
        PagedResult<Park> allParks = await this.parkRepository.SearchAsync(
            criteria,
            1,
            allPageSize,
            query.IncludeHidden,
            query.IsVisible,
            query.AdminReviewStatus,
            query.Type,
            query.CountryCode,
            query.HasValidCoordinates,
            query.ClosedFilter,
            cancellationToken);
        PagedResult<ParkListResult> enrichedPage = await this.EnrichAsync(allParks, true, cancellationToken);
        List<ParkListResult> sortedItems = SortByCounts(enrichedPage.Items, query.SortField, query.SortDescending);
        List<ParkListResult> pagedItems = sortedItems
            .Skip((query.Paging.Page - 1) * query.Paging.PageSize)
            .Take(query.Paging.PageSize)
            .ToList();

        return new PagedResult<ParkListResult>(pagedItems, query.Paging.Page, query.Paging.PageSize, enrichedPage.TotalItems);
    }

    private async Task<PagedResult<ParkListResult>> EnrichAsync(PagedResult<Park> page, bool includeCounts, CancellationToken cancellationToken)
    {
        if (!includeCounts)
        {
            return new PagedResult<ParkListResult>(
                page.Items.Select(static park => new ParkListResult { Park = park }).ToList(),
                page.Page,
                page.PageSize,
                page.TotalItems);
        }

        List<string> parkIds = page.Items
            .Select(static park => park.Id)
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId!)
            .ToList();

        IReadOnlyDictionary<string, ParkItemVisibilityCounts> counts = await this.parkItemRepository.GetVisibilityCountsByParkIdsAsync(parkIds, cancellationToken);
        List<ParkListResult> items = page.Items
            .Select(park =>
            {
                ParkItemVisibilityCounts? itemCounts = !string.IsNullOrWhiteSpace(park.Id) && counts.TryGetValue(park.Id, out ParkItemVisibilityCounts? resolvedCounts)
                    ? resolvedCounts
                    : null;

                return new ParkListResult
                {
                    Park = park,
                    ParkItemsTotalCount = itemCounts?.TotalCount ?? 0,
                    ParkItemsVisibleCount = itemCounts?.VisibleCount ?? 0,
                };
            })
            .ToList();

        return new PagedResult<ParkListResult>(items, page.Page, page.PageSize, page.TotalItems);
    }

    private static bool RequiresCountSort(ParkAdminSortField sortField)
    {
        return sortField == ParkAdminSortField.ParkItemsTotalCount
            || sortField == ParkAdminSortField.ParkItemsVisibleCount;
    }

    private static List<ParkListResult> SortByCounts(IReadOnlyCollection<ParkListResult> items, ParkAdminSortField sortField, bool sortDescending)
    {
        Func<ParkListResult, int> keySelector = sortField == ParkAdminSortField.ParkItemsVisibleCount
            ? static item => item.ParkItemsVisibleCount ?? 0
            : static item => item.ParkItemsTotalCount ?? 0;

        IOrderedEnumerable<ParkListResult> orderedItems = sortDescending
            ? items.OrderByDescending(keySelector)
            : items.OrderBy(keySelector);

        return orderedItems
            .ThenBy(static item => item.Park.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static item => item.Park.Id, StringComparer.Ordinal)
            .ToList();
    }
}
