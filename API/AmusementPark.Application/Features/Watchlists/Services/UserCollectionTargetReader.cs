using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class UserCollectionTargetReader
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IImageRepository imageRepository;

    public UserCollectionTargetReader(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IImageRepository imageRepository)
    {
        this.parkRepository = parkRepository ?? throw new ArgumentNullException(nameof(parkRepository));
        this.parkItemRepository = parkItemRepository ?? throw new ArgumentNullException(nameof(parkItemRepository));
        this.imageRepository = imageRepository ?? throw new ArgumentNullException(nameof(imageRepository));
    }

    internal async Task<UserCollectionTargetSnapshot> ResolveAsync(
        CollectionTargetType targetType,
        string targetId,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, UserCollectionTargetSnapshot> snapshots = await this.ResolveAsync(
            targetType,
            new[] { targetId },
            cancellationToken);
        return snapshots.TryGetValue(targetId, out UserCollectionTargetSnapshot? snapshot)
            ? snapshot
            : Unavailable(targetType, targetId);
    }

    internal async Task<IReadOnlyDictionary<string, UserCollectionTargetSnapshot>> ResolveAsync(
        CollectionTargetType targetType,
        IReadOnlyCollection<string> targetIds,
        CancellationToken cancellationToken)
    {
        string[] normalizedIds = targetIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedIds.Length == 0)
        {
            return new Dictionary<string, UserCollectionTargetSnapshot>(StringComparer.Ordinal);
        }

        return targetType == CollectionTargetType.Park
            ? await this.ResolveParksAsync(normalizedIds, cancellationToken)
            : await this.ResolveParkItemsAsync(normalizedIds, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, UserCollectionTargetSnapshot>> ResolveParksAsync(
        IReadOnlyCollection<string> targetIds,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Park> parks = await this.parkRepository.GetByIdsAsync(
            targetIds,
            cancellationToken);
        Park[] publicParks = parks.Where(static park => park.IsPubliclyDiscoverable()).ToArray();
        string[] publicIds = publicParks.Select(static park => park.Id).ToArray();
        IReadOnlyDictionary<string, string> images = await this.imageRepository
            .GetMainImageIdsByOwnersAsync(
                ImageOwnerType.Park,
                publicIds,
                ImageCategory.Park,
                true,
                cancellationToken);
        Dictionary<string, UserCollectionTargetSnapshot> result = targetIds.ToDictionary(
            static id => id,
            static id => Unavailable(CollectionTargetType.Park, id),
            StringComparer.Ordinal);
        foreach (Park park in publicParks)
        {
            _ = images.TryGetValue(park.Id, out string? imageId);
            result[park.Id] = new UserCollectionTargetSnapshot(
                CollectionTargetType.Park,
                park.Id,
                CollectionTargetStatusResolver.Resolve(park.Status),
                park.Name,
                null,
                null,
                imageId);
        }

        return result;
    }

    private async Task<IReadOnlyDictionary<string, UserCollectionTargetSnapshot>> ResolveParkItemsAsync(
        IReadOnlyCollection<string> targetIds,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetByIdsAsync(
            targetIds,
            cancellationToken);
        ParkItem[] visibleItems = items
            .Where(static item => item.IsVisible && !string.IsNullOrWhiteSpace(item.ParkId))
            .ToArray();
        string[] parkIds = visibleItems
            .Select(static item => item.ParkId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<Park> parks = await this.parkRepository.GetByIdsAsync(
            parkIds,
            cancellationToken);
        Dictionary<string, Park> publicParks = parks
            .Where(static park => park.IsPubliclyDiscoverable())
            .ToDictionary(static park => park.Id, StringComparer.Ordinal);
        ParkItem[] publicItems = visibleItems
            .Where(item => publicParks.ContainsKey(item.ParkId))
            .ToArray();
        string[] publicItemIds = publicItems.Select(static item => item.Id).ToArray();
        IReadOnlyDictionary<string, string> images = await this.imageRepository
            .GetMainImageIdsByOwnersAsync(
                ImageOwnerType.ParkItem,
                publicItemIds,
                ImageCategory.ParkItem,
                true,
                cancellationToken);
        Dictionary<string, UserCollectionTargetSnapshot> result = targetIds.ToDictionary(
            static id => id,
            static id => Unavailable(CollectionTargetType.ParkItem, id),
            StringComparer.Ordinal);
        foreach (ParkItem item in publicItems)
        {
            Park parentPark = publicParks[item.ParkId];
            _ = images.TryGetValue(item.Id, out string? imageId);
            result[item.Id] = new UserCollectionTargetSnapshot(
                CollectionTargetType.ParkItem,
                item.Id,
                CollectionTargetStatusResolver.Resolve(item, parentPark.Status),
                item.Name,
                parentPark.Id,
                parentPark.Name,
                imageId);
        }

        return result;
    }

    private static UserCollectionTargetSnapshot Unavailable(
        CollectionTargetType targetType,
        string targetId)
    {
        return new UserCollectionTargetSnapshot(
            targetType,
            targetId,
            CollectionTargetStatus.Unknown,
            null,
            null,
            null,
            null);
    }

}
