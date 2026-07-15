using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

internal sealed class HistoryTimelineHydration
{
    private readonly IReadOnlyDictionary<string, Park> parksById;
    private readonly IReadOnlyDictionary<string, ParkItem> parkItemsById;
    private readonly IReadOnlyDictionary<string, Image> imagesById;

    private HistoryTimelineHydration(
        IReadOnlyDictionary<string, Park> parksById,
        IReadOnlyDictionary<string, ParkItem> parkItemsById,
        IReadOnlyDictionary<string, Image> imagesById)
    {
        this.parksById = parksById;
        this.parkItemsById = parkItemsById;
        this.imagesById = imagesById;
    }

    public static Task<HistoryTimelineHydration> LoadAsync(
        IReadOnlyCollection<HistoryEvent> events,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IImageRepository imageRepository,
        CancellationToken cancellationToken)
    {
        return LoadAsync(events, parkRepository, parkItemRepository, imageRepository, true, cancellationToken);
    }

    public static async Task<HistoryTimelineHydration> LoadAsync(
        IReadOnlyCollection<HistoryEvent> events,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IImageRepository imageRepository,
        bool includeImages,
        CancellationToken cancellationToken)
    {
        List<string> parkIds = events
            .SelectMany(static item => new[] { item.ParkId, item.ContextParkId }.Concat(item.RelatedParkIds))
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        List<string> parkItemIds = events
            .SelectMany(static item => new[] { item.ParkItemId }.Concat(item.RelatedParkItemIds))
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        IReadOnlyCollection<Park> parks = parkIds.Count == 0
            ? Array.Empty<Park>()
            : await parkRepository.GetByIdsAsync(parkIds, cancellationToken);

        IReadOnlyCollection<ParkItem> parkItems = parkItemIds.Count == 0
            ? Array.Empty<ParkItem>()
            : await parkItemRepository.GetByIdsAsync(parkItemIds, cancellationToken);

        Dictionary<string, Image> imagesById = includeImages
            ? await LoadImagesAsync(events, imageRepository, cancellationToken)
            : new Dictionary<string, Image>(StringComparer.Ordinal);

        return new HistoryTimelineHydration(
            parks
                .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
                .ToDictionary(static item => item.Id, StringComparer.Ordinal),
            parkItems
                .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
                .ToDictionary(static item => item.Id, StringComparer.Ordinal),
            imagesById);
    }

    public HistoryTimelineEventResult ToTimelineEvent(HistoryEvent historyEvent)
    {
        string? contextParkId = historyEvent.ContextParkId ?? historyEvent.ParkId;
        Park? contextPark = !string.IsNullOrWhiteSpace(contextParkId) && this.parksById.TryGetValue(contextParkId, out Park? resolvedPark)
            ? resolvedPark
            : null;

        string? parkItemId = historyEvent.ParkItemId;
        ParkItem? parkItem = !string.IsNullOrWhiteSpace(parkItemId) && this.parkItemsById.TryGetValue(parkItemId, out ParkItem? resolvedParkItem)
            ? resolvedParkItem
            : null;

        string? mainImageId = historyEvent.Article?.MainImageId ?? historyEvent.MainImageId;
        Image? mainImage = !string.IsNullOrWhiteSpace(mainImageId) && this.imagesById.TryGetValue(mainImageId, out Image? resolvedImage)
            ? resolvedImage
            : null;

        return new HistoryTimelineEventResult
        {
            Event = historyEvent,
            ContextPark = contextPark,
            ParkItem = parkItem,
            MainImage = mainImage,
        };
    }

    private static async Task<Dictionary<string, Image>> LoadImagesAsync(
        IReadOnlyCollection<HistoryEvent> events,
        IImageRepository imageRepository,
        CancellationToken cancellationToken)
    {
        List<string> imageIds = events
            .Select(static item => item.Article?.MainImageId ?? item.MainImageId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Dictionary<string, Image> imagesById = new Dictionary<string, Image>(StringComparer.Ordinal);
        foreach (string imageId in imageIds)
        {
            Image? image = await imageRepository.GetByIdAsync(imageId, cancellationToken);
            if (image is not null)
            {
                imagesById[image.Id] = image;
            }
        }

        return imagesById;
    }
}
