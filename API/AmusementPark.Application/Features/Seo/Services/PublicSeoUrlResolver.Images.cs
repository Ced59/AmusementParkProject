using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Seo.Services;

public sealed partial class PublicSeoUrlResolver
{
    private async Task AddImageImpactUrlsAsync(
        HashSet<string> relativePaths,
        IReadOnlyCollection<string> languages,
        IReadOnlyCollection<PublicSeoImageSnapshot> imageSnapshots,
        PublicSeoUpdate update,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PublicSeoImageSnapshot> publicImages = imageSnapshots
            .Where(static image => image.IsPublished)
            .ToList();
        if (publicImages.Count == 0)
        {
            return;
        }

        IReadOnlyCollection<string> parkOwnerIds = publicImages
            .Where(static image => image.OwnerType == ImageOwnerType.Park)
            .Select(static image => image.OwnerId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        IReadOnlyDictionary<string, PublicSeoParkSnapshot> parksById = await this.LoadParentParksAsync(parkOwnerIds, update, cancellationToken);
        foreach (string parkOwnerId in parkOwnerIds)
        {
            if (!parksById.TryGetValue(parkOwnerId, out PublicSeoParkSnapshot? park) || !IsPublicPark(park))
            {
                continue;
            }

            AddParkDetailUrls(relativePaths, languages, park);
            AddParkImageUrls(relativePaths, languages, park);
        }

        IReadOnlyCollection<string> itemOwnerIds = publicImages
            .Where(static image => image.OwnerType == ImageOwnerType.ParkItem)
            .Select(static image => image.OwnerId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        IReadOnlyDictionary<string, PublicSeoParkItemSnapshot> itemsById = await this.LoadVideoOwnerItemsAsync(itemOwnerIds, cancellationToken);
        IReadOnlyCollection<string> itemParkIds = itemsById.Values
            .Where(IsPublicItem)
            .Select(static item => item.ParkId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        IReadOnlyDictionary<string, PublicSeoParkSnapshot> itemParksById = await this.LoadParentParksAsync(itemParkIds, update, cancellationToken);
        foreach (string itemOwnerId in itemOwnerIds)
        {
            if (!itemsById.TryGetValue(itemOwnerId, out PublicSeoParkItemSnapshot? item) || !IsPublicItem(item))
            {
                continue;
            }

            if (!itemParksById.TryGetValue(item.ParkId, out PublicSeoParkSnapshot? park) || !IsPublicPark(park))
            {
                continue;
            }

            AddParkDetailUrls(relativePaths, languages, park);
            AddParkImageUrls(relativePaths, languages, park);
            AddParkItemDetailUrls(relativePaths, languages, park, item);
            AddParkItemImageUrls(relativePaths, languages, park, item);
        }
    }

    private async Task<int> GetPublishedImageCountAsync(
        ImageOwnerType ownerType,
        ImageCategory category,
        string ownerId,
        Dictionary<string, int> imageCountByKey,
        CancellationToken cancellationToken)
    {
        string key = $"{ownerType}:{category}:{ownerId}";
        if (imageCountByKey.TryGetValue(key, out int cachedValue))
        {
            return cachedValue;
        }

        ImageSearchCriteria criteria = new ImageSearchCriteria(
            Category: category,
            OwnerType: ownerType,
            OwnerId: ownerId,
            IsPublished: true,
            HasOwner: true);
        PagedResult<Image> page = await this.imageRepository.GetPageAsync(1, 1, criteria, cancellationToken);
        int imageCount = page.TotalItems > int.MaxValue ? int.MaxValue : (int)page.TotalItems;
        imageCountByKey[key] = imageCount;
        return imageCount;
    }

    private async Task<bool> HasMinimumPublishedParkOrItemImagesAsync(
        string parkId,
        IReadOnlyCollection<PublicSeoParkItemSnapshot> currentPublicItems,
        Dictionary<string, int> imageCountByKey,
        CancellationToken cancellationToken)
    {
        int imageCount = await this.GetPublishedImageCountAsync(ImageOwnerType.Park, ImageCategory.Park, parkId, imageCountByKey, cancellationToken);
        imageCount += await this.GetPublishedImageCountAsync(ImageOwnerType.Park, ImageCategory.Logo, parkId, imageCountByKey, cancellationToken);
        if (SeoPageValuePolicy.IsImageGalleryIndexable(imageCount))
        {
            return true;
        }

        foreach (PublicSeoParkItemSnapshot item in currentPublicItems)
        {
            imageCount += await this.GetPublishedImageCountAsync(ImageOwnerType.ParkItem, ImageCategory.ParkItem, item.Id, imageCountByKey, cancellationToken);
            if (SeoPageValuePolicy.IsImageGalleryIndexable(imageCount))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyCollection<PublicSeoImageSnapshot> MergeImageSnapshots(
        IReadOnlyCollection<PublicSeoImageSnapshot> previousImages,
        IReadOnlyCollection<PublicSeoImageSnapshot> currentImages)
    {
        return previousImages
            .Concat(currentImages)
            .Where(static image => !string.IsNullOrWhiteSpace(image.Id) && !string.IsNullOrWhiteSpace(image.OwnerId))
            .GroupBy(
                static image => $"{image.Id}:{image.OwnerType}:{image.OwnerId}:{image.Category}:{image.IsPublished}",
                StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .ToList();
    }
}
