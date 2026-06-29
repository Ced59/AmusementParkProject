using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Contracts;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de recuperation paginee des parcs.
/// </summary>
public sealed class GetParksPageQueryHandler : IQueryHandler<GetParksPageQuery, ApplicationResult<PagedResult<ParkListResult>>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkOpeningHoursRepository parkOpeningHoursRepository;
    private readonly ParkOpeningHoursAdminStatusResolver openingHoursStatusResolver;
    private readonly PagedQueryValidator pagedQueryValidator;

    public GetParksPageQueryHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkOpeningHoursRepository parkOpeningHoursRepository,
        ParkOpeningHoursAdminStatusResolver openingHoursStatusResolver,
        PagedQueryValidator pagedQueryValidator)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkOpeningHoursRepository = parkOpeningHoursRepository;
        this.openingHoursStatusResolver = openingHoursStatusResolver;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<ParkListResult>>> HandleAsync(GetParksPageQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<ParkListResult>>.Failure(errors);
        }

        if (RequiresApplicationLevelPage(query.SortField, query.OpeningHoursFilter))
        {
            PagedResult<ParkListResult> sortedResult = await this.LoadApplicationLevelPageAsync(query, cancellationToken);
            return ApplicationResult<PagedResult<ParkListResult>>.Success(sortedResult);
        }

        PagedResult<Park> page = await this.parkRepository.GetPageAsync(
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

        PagedResult<ParkListResult> result = await this.EnrichAsync(page, query.IncludeHidden, query.IncludeHidden, cancellationToken);
        return ApplicationResult<PagedResult<ParkListResult>>.Success(result);
    }

    private async Task<PagedResult<ParkListResult>> LoadApplicationLevelPageAsync(GetParksPageQuery query, CancellationToken cancellationToken)
    {
        PagedResult<Park> countProbe = await this.parkRepository.GetPageAsync(
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

        ParkAdminSortField repositorySortField = RequiresApplicationLevelSort(query.SortField)
            ? ParkAdminSortField.Default
            : query.SortField;
        int allPageSize = checked((int)Math.Min(countProbe.TotalItems, int.MaxValue));
        PagedResult<Park> allParks = await this.parkRepository.GetPageAsync(
            1,
            allPageSize,
            query.IncludeHidden,
            query.IsVisible,
            query.AdminReviewStatus,
            query.Type,
            query.CountryCode,
            query.HasValidCoordinates,
            query.ClosedFilter,
            cancellationToken,
            repositorySortField,
            query.SortDescending);
        PagedResult<ParkListResult> enrichedPage = await this.EnrichAsync(allParks, true, true, cancellationToken);
        List<ParkListResult> filteredItems = ApplyOpeningHoursFilter(enrichedPage.Items, query.OpeningHoursFilter);
        List<ParkListResult> sortedItems = RequiresApplicationLevelSort(query.SortField)
            ? SortApplicationLevel(filteredItems, query.SortField, query.SortDescending)
            : filteredItems;
        List<ParkListResult> pagedItems = sortedItems
            .Skip((query.Paging.Page - 1) * query.Paging.PageSize)
            .Take(query.Paging.PageSize)
            .ToList();

        return new PagedResult<ParkListResult>(pagedItems, query.Paging.Page, query.Paging.PageSize, filteredItems.Count);
    }

    private async Task<PagedResult<ParkListResult>> EnrichAsync(PagedResult<Park> page, bool includeCounts, bool includeOpeningHours, CancellationToken cancellationToken)
    {
        if (!includeCounts && !includeOpeningHours)
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

        IReadOnlyDictionary<string, ParkItemVisibilityCounts> counts = includeCounts
            ? await this.parkItemRepository.GetVisibilityCountsByParkIdsAsync(parkIds, cancellationToken)
            : new Dictionary<string, ParkItemVisibilityCounts>(StringComparer.Ordinal);
        IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary> openingHoursSummaries = includeOpeningHours
            ? await this.parkOpeningHoursRepository.GetSummariesByParkIdsAsync(parkIds, cancellationToken)
            : new Dictionary<string, ParkOpeningHoursScheduleSummary>(StringComparer.Ordinal);
        List<ParkListResult> items = page.Items
            .Select(park =>
            {
                ParkItemVisibilityCounts? itemCounts = !string.IsNullOrWhiteSpace(park.Id) && counts.TryGetValue(park.Id, out ParkItemVisibilityCounts? resolvedCounts)
                    ? resolvedCounts
                    : null;
                ParkOpeningHoursScheduleSummary? openingHoursSummary = !string.IsNullOrWhiteSpace(park.Id) && openingHoursSummaries.TryGetValue(park.Id, out ParkOpeningHoursScheduleSummary? resolvedSummary)
                    ? resolvedSummary
                    : null;

                return new ParkListResult
                {
                    Park = park,
                    ParkItemsTotalCount = itemCounts?.TotalCount ?? 0,
                    ParkItemsVisibleCount = itemCounts?.VisibleCount ?? 0,
                    OpeningHours = includeOpeningHours ? this.ToOpeningHoursSummaryResult(openingHoursSummary) : null,
                };
            })
            .ToList();

        return new PagedResult<ParkListResult>(items, page.Page, page.PageSize, page.TotalItems);
    }

    private ParkOpeningHoursAdminSummaryResult ToOpeningHoursSummaryResult(ParkOpeningHoursScheduleSummary? summary)
    {
        ParkOpeningHoursAdminCoverage coverage = this.openingHoursStatusResolver.ResolveCoverage(summary);
        return new ParkOpeningHoursAdminSummaryResult
        {
            HasOpeningHours = summary is not null && summary.HasScheduleData,
            Status = coverage.Status,
            TimeZoneId = summary?.TimeZoneId,
            FirstDate = summary?.FirstDate,
            LastDate = summary?.LastDate,
            CompleteUntilDate = coverage.CompleteUntilDate,
            CompleteForDays = coverage.CompleteForDays,
            WarningThresholdDays = coverage.WarningThresholdDays,
            LastVerifiedAtUtc = summary?.LastVerifiedAtUtc,
            UpdatedAtUtc = summary?.UpdatedAtUtc,
        };
    }

    private static bool RequiresApplicationLevelPage(ParkAdminSortField sortField, ParkOpeningHoursAdminFilter openingHoursFilter)
    {
        return RequiresApplicationLevelSort(sortField) || openingHoursFilter != ParkOpeningHoursAdminFilter.All;
    }

    private static bool RequiresApplicationLevelSort(ParkAdminSortField sortField)
    {
        return sortField == ParkAdminSortField.ParkItemsTotalCount
            || sortField == ParkAdminSortField.ParkItemsVisibleCount
            || sortField == ParkAdminSortField.OpeningHoursStatus;
    }

    private static List<ParkListResult> SortApplicationLevel(IReadOnlyCollection<ParkListResult> items, ParkAdminSortField sortField, bool sortDescending)
    {
        Func<ParkListResult, int> keySelector = sortField switch
        {
            ParkAdminSortField.ParkItemsVisibleCount => static item => item.ParkItemsVisibleCount ?? 0,
            ParkAdminSortField.OpeningHoursStatus => static item => (int)(item.OpeningHours?.Status ?? ParkOpeningHoursAdminStatus.NotConfigured),
            _ => static item => item.ParkItemsTotalCount ?? 0,
        };

        IOrderedEnumerable<ParkListResult> orderedItems = sortDescending
            ? items.OrderByDescending(keySelector)
            : items.OrderBy(keySelector);

        return orderedItems
            .ThenBy(static item => item.Park.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static item => item.Park.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static List<ParkListResult> ApplyOpeningHoursFilter(IReadOnlyCollection<ParkListResult> items, ParkOpeningHoursAdminFilter filter)
    {
        if (filter == ParkOpeningHoursAdminFilter.All)
        {
            return items.ToList();
        }

        return items
            .Where(item => MatchesOpeningHoursFilter(item.OpeningHours, filter))
            .ToList();
    }

    private static bool MatchesOpeningHoursFilter(ParkOpeningHoursAdminSummaryResult? summary, ParkOpeningHoursAdminFilter filter)
    {
        bool hasOpeningHours = summary?.HasOpeningHours == true;
        return filter switch
        {
            ParkOpeningHoursAdminFilter.Configured => hasOpeningHours,
            ParkOpeningHoursAdminFilter.NotConfigured => !hasOpeningHours,
            ParkOpeningHoursAdminFilter.UpToDate => summary?.Status == ParkOpeningHoursAdminStatus.UpToDate,
            ParkOpeningHoursAdminFilter.NeedsUpdate => summary?.Status == ParkOpeningHoursAdminStatus.NeedsUpdate,
            ParkOpeningHoursAdminFilter.Expired => summary?.Status == ParkOpeningHoursAdminStatus.Expired,
            _ => true,
        };
    }
}
