using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Services;

internal static class SitemapPublicCandidateLoader
{
    internal const int PageSize = 500;

    public static async Task<IReadOnlyCollection<Park>> LoadPublicParksAsync(
        IParkRepository parkRepository,
        CancellationToken cancellationToken)
    {
        List<Park> parks = new List<Park>();
        int pageNumber = 1;

        while (true)
        {
            PagedResult<Park> page = await parkRepository.GetPageAsync(
                pageNumber,
                PageSize,
                includeHidden: false,
                isVisible: true,
                adminReviewStatus: null,
                type: null,
                countryCode: null,
                hasValidCoordinates: null,
                closedFilter: ClosedEntityFilter.OpenOnly,
                cancellationToken);

            parks.AddRange(page.Items.Where(ParksSitemapSectionProvider.IsPublicPark));

            if (page.Items.Count == 0 || page.Page >= page.TotalPages)
            {
                break;
            }

            pageNumber++;
        }

        return parks;
    }

    public static async Task<IReadOnlyCollection<ParkItem>> LoadPublicItemsAsync(
        IParkItemRepository parkItemRepository,
        CancellationToken cancellationToken)
    {
        List<ParkItem> items = new List<ParkItem>();
        int pageNumber = 1;

        while (true)
        {
            PagedResult<ParkItem> page = await parkItemRepository.GetPageAsync(
                pageNumber,
                PageSize,
                parkId: null,
                search: null,
                includeHidden: false,
                isVisible: true,
                adminReviewStatus: null,
                category: null,
                type: null,
                zoneId: null,
                manufacturerId: null,
                contentBacklogFilter: null,
                cancellationToken: cancellationToken,
                sortField: ParkItemAdminSortField.ParkId);

            items.AddRange(page.Items.Where(ParkItemsSitemapSectionProvider.IsPublicItem));

            if (page.Items.Count == 0 || page.Page >= page.TotalPages)
            {
                break;
            }

            pageNumber++;
        }

        return items;
    }
}
