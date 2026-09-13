using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Services;
internal static class PublicSeoUrlResolverImagesExtensions
{
    internal static async Task AddImageImpactUrlsAsync(this PublicSeoUrlResolver processorContext, HashSet<string> relativePaths, IReadOnlyCollection<string> languages, IReadOnlyCollection<PublicSeoImageSnapshot> imageSnapshots, PublicSeoUpdate update, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<PublicSeoImageSnapshot> publicImages = imageSnapshots.Where(static image => image.IsPublished).ToList();
        if (publicImages.Count == 0)
        {
            return;
        }

        IReadOnlyCollection<string> parkOwnerIds = publicImages.Where(static image => image.OwnerType == ImageOwnerType.Park).Select(static image => image.OwnerId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        IReadOnlyDictionary<string, PublicSeoParkSnapshot> parksById = await processorContext.LoadParentParksAsync(parkOwnerIds, update, cancellationToken);
        foreach (string parkOwnerId in parkOwnerIds)
        {
            if (!parksById.TryGetValue(parkOwnerId, out PublicSeoParkSnapshot? park) || !PublicSeoUrlResolver.IsPublicPark(park))
            {
                continue;
            }

            PublicSeoUrlResolverRoutesExtensions.AddParkDetailUrls(relativePaths, languages, park);
            PublicSeoUrlResolverRoutesExtensions.AddParkImageUrls(relativePaths, languages, park);
        }

        IReadOnlyCollection<string> itemOwnerIds = publicImages.Where(static image => image.OwnerType == ImageOwnerType.ParkItem).Select(static image => image.OwnerId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        IReadOnlyDictionary<string, PublicSeoParkItemSnapshot> itemsById = await processorContext.LoadVideoOwnerItemsAsync(itemOwnerIds, cancellationToken);
        IReadOnlyCollection<string> itemParkIds = itemsById.Values.Where(PublicSeoUrlResolver.IsPublicItem).Select(static item => item.ParkId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        IReadOnlyDictionary<string, PublicSeoParkSnapshot> itemParksById = await processorContext.LoadParentParksAsync(itemParkIds, update, cancellationToken);
        foreach (string itemOwnerId in itemOwnerIds)
        {
            if (!itemsById.TryGetValue(itemOwnerId, out PublicSeoParkItemSnapshot? item) || !PublicSeoUrlResolver.IsPublicItem(item))
            {
                continue;
            }

            if (!itemParksById.TryGetValue(item.ParkId, out PublicSeoParkSnapshot? park) || !PublicSeoUrlResolver.IsPublicPark(park))
            {
                continue;
            }

            PublicSeoUrlResolverRoutesExtensions.AddParkDetailUrls(relativePaths, languages, park);
            PublicSeoUrlResolverRoutesExtensions.AddParkImageUrls(relativePaths, languages, park);
            PublicSeoUrlResolverRoutesExtensions.AddParkItemDetailUrls(relativePaths, languages, park, item);
            PublicSeoUrlResolverRoutesExtensions.AddParkItemImageUrls(relativePaths, languages, park, item);
        }
    }

    internal static async Task<int> GetPublishedImageCountAsync(this PublicSeoUrlResolver processorContext, ImageOwnerType ownerType, ImageCategory category, string ownerId, Dictionary<string, int> imageCountByKey, CancellationToken cancellationToken)
    {
        string key = $"{ownerType}:{category}:{ownerId}";
        if (imageCountByKey.TryGetValue(key, out int cachedValue))
        {
            return cachedValue;
        }

        ImageSearchCriteria criteria = new ImageSearchCriteria(Category: category, OwnerType: ownerType, OwnerId: ownerId, IsPublished: true, HasOwner: true);
        PagedResult<Image> page = await processorContext.imageRepository.GetPageAsync(1, 1, criteria, cancellationToken);
        int imageCount = page.TotalItems > int.MaxValue ? int.MaxValue : (int)page.TotalItems;
        imageCountByKey[key] = imageCount;
        return imageCount;
    }

    internal static async Task<bool> HasMinimumPublishedParkOrItemImagesAsync(this PublicSeoUrlResolver processorContext, string parkId, IReadOnlyCollection<PublicSeoParkItemSnapshot> currentPublicItems, Dictionary<string, int> imageCountByKey, CancellationToken cancellationToken)
    {
        int imageCount = await processorContext.GetPublishedImageCountAsync(ImageOwnerType.Park, ImageCategory.Park, parkId, imageCountByKey, cancellationToken);
        imageCount += await processorContext.GetPublishedImageCountAsync(ImageOwnerType.Park, ImageCategory.Logo, parkId, imageCountByKey, cancellationToken);
        if (SeoPageValuePolicy.IsImageGalleryIndexable(imageCount))
        {
            return true;
        }

        foreach (PublicSeoParkItemSnapshot item in currentPublicItems)
        {
            imageCount += await processorContext.GetPublishedImageCountAsync(ImageOwnerType.ParkItem, ImageCategory.ParkItem, item.Id, imageCountByKey, cancellationToken);
            if (SeoPageValuePolicy.IsImageGalleryIndexable(imageCount))
            {
                return true;
            }
        }

        return false;
    }

    internal static IReadOnlyCollection<PublicSeoImageSnapshot> MergeImageSnapshots(IReadOnlyCollection<PublicSeoImageSnapshot> previousImages, IReadOnlyCollection<PublicSeoImageSnapshot> currentImages)
    {
        return previousImages.Concat(currentImages).Where(static image => !string.IsNullOrWhiteSpace(image.Id) && !string.IsNullOrWhiteSpace(image.OwnerId)).GroupBy(static image => $"{image.Id}:{image.OwnerType}:{image.OwnerId}:{image.Category}:{image.IsPublished}", StringComparer.OrdinalIgnoreCase).Select(static group => group.First()).ToList();
    }
}
