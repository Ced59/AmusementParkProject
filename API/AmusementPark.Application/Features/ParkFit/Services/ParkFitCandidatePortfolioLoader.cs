using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Services;

public sealed class ParkFitCandidatePortfolioLoader
{
    private readonly IParkRepository parkRepository;
    private readonly IParkFitOperationalStatusRepository operationalStatusRepository;

    public ParkFitCandidatePortfolioLoader(
        IParkRepository parkRepository,
        IParkFitOperationalStatusRepository operationalStatusRepository)
    {
        this.parkRepository = parkRepository;
        this.operationalStatusRepository = operationalStatusRepository;
    }

    public async Task<ParkFitCandidatePortfolio> LoadAsync(
        string? countryCode,
        CancellationToken cancellationToken)
    {
        List<Park> activeCandidates = new List<Park>();
        long totalCandidateCount = 0;
        int inspectedCandidateCount = 0;
        int suspendedCandidateCount = 0;
        int notActivatedCandidateCount = 0;
        int pageNumber = 1;
        bool hasAnotherPage;

        do
        {
            PagedResult<Park> page = await this.parkRepository.GetPageAsync(
                pageNumber,
                ParkFitSearchLimits.CandidatePageSize,
                includeHidden: false,
                isVisible: true,
                adminReviewStatus: null,
                type: null,
                countryCode: countryCode,
                hasValidCoordinates: true,
                closedFilter: ClosedEntityFilter.OpenOnly,
                cancellationToken: cancellationToken,
                sortField: ParkAdminSortField.Name);
            if (pageNumber == 1)
            {
                totalCandidateCount = page.TotalItems;
            }

            List<string> parkIds = page.Items
                .Select(static park => park.Id)
                .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            IReadOnlyDictionary<string, ParkFitOperationalStatus> statuses =
                parkIds.Count == 0
                    ? new Dictionary<string, ParkFitOperationalStatus>(StringComparer.Ordinal)
                    : await this.operationalStatusRepository.GetByParkIdsAsync(
                        parkIds,
                        cancellationToken);

            foreach (Park park in page.Items)
            {
                if (!statuses.TryGetValue(park.Id, out ParkFitOperationalStatus? status)
                    || status.State == ParkFitRecommendationState.NotActivated)
                {
                    notActivatedCandidateCount++;
                    continue;
                }

                if (status.State == ParkFitRecommendationState.Suspended)
                {
                    suspendedCandidateCount++;
                    continue;
                }

                if (activeCandidates.Count < ParkFitSearchLimits.MaximumActiveCandidateCount)
                {
                    activeCandidates.Add(park);
                }
            }

            inspectedCandidateCount += page.Items.Count;
            hasAnotherPage = page.Items.Count == ParkFitSearchLimits.CandidatePageSize
                && inspectedCandidateCount < totalCandidateCount
                && activeCandidates.Count < ParkFitSearchLimits.MaximumActiveCandidateCount;
            pageNumber++;
        }
        while (hasAnotherPage);

        return new ParkFitCandidatePortfolio
        {
            ActiveCandidates = activeCandidates,
            TotalCandidateCount = totalCandidateCount,
            InspectedCandidateCount = inspectedCandidateCount,
            OperationallySuspendedCandidateCount = suspendedCandidateCount,
            NotActivatedCandidateCount = notActivatedCandidateCount,
            CandidatePoolTruncated = inspectedCandidateCount < totalCandidateCount,
        };
    }
}
