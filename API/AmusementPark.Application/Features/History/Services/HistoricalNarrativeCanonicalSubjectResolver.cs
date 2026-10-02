using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Services;

internal sealed class HistoricalNarrativeCanonicalSubjectResolver
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IStandaloneAttractionRepository? standaloneAttractionRepository;

    public HistoricalNarrativeCanonicalSubjectResolver(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IStandaloneAttractionRepository? standaloneAttractionRepository = null)
    {
        this.parkRepository = parkRepository ?? throw new ArgumentNullException(nameof(parkRepository));
        this.parkItemRepository = parkItemRepository ?? throw new ArgumentNullException(nameof(parkItemRepository));
        this.standaloneAttractionRepository = standaloneAttractionRepository;
    }

    internal async Task<HistoricalSubjectResolution?> ResolveAsync(
        HistoryEvent historyEvent,
        CancellationToken cancellationToken)
    {
        if (historyEvent.EntityType == HistoryEntityType.Park)
        {
            Park? park = await this.parkRepository.GetByIdAsync(
                historyEvent.OwnerId,
                true,
                cancellationToken);
            return park is null
                ? null
                : new HistoricalSubjectResolution(
                    HistoricalSubjectType.Park,
                    park.Id,
                    park.Name ?? historyEvent.OwnerId,
                    park.IsPubliclyDiscoverable()
                        ? HistoricalSubjectPublicationPolicy.FollowCurrentSubject
                        : HistoricalSubjectPublicationPolicy.Suppressed,
                    park.Id);
        }

        if (historyEvent.EntityType == HistoryEntityType.ParkItem)
        {
            ParkItem? item = await this.parkItemRepository.GetByIdAsync(
                historyEvent.OwnerId,
                true,
                cancellationToken);
            if (item is null)
            {
                return null;
            }

            Park? park = await this.parkRepository.GetByIdAsync(
                item.ParkId,
                true,
                cancellationToken);
            bool isPublic = item.IsVisible
                && item.AdminReviewStatus != AdminReviewStatus.NotRelevant
                && park?.IsPubliclyDiscoverable() == true;
            return new HistoricalSubjectResolution(
                HistoricalSubjectType.ParkItem,
                item.Id,
                item.Name,
                isPublic
                    ? HistoricalSubjectPublicationPolicy.FollowCurrentSubject
                    : HistoricalSubjectPublicationPolicy.Suppressed,
                item.ParkId);
        }

        if (historyEvent.EntityType == HistoryEntityType.StandaloneAttraction
            && this.standaloneAttractionRepository is not null)
        {
            StandaloneAttraction? attraction = await this.standaloneAttractionRepository.GetByIdAsync(
                historyEvent.OwnerId,
                true,
                cancellationToken);
            return attraction is null
                ? null
                : new HistoricalSubjectResolution(
                    HistoricalSubjectType.StandaloneAttraction,
                    attraction.Id,
                    attraction.Name,
                    attraction.IsPubliclyPublishable()
                        ? HistoricalSubjectPublicationPolicy.HistoricalOnly
                        : HistoricalSubjectPublicationPolicy.Suppressed,
                    null);
        }

        return null;
    }
}
