using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionManufacturers.Handlers;

internal static class AttractionManufacturerImageSelection
{
    public static Dictionary<string, HashSet<string>> BuildPublishedLogoImageIdsByOwner(IReadOnlyCollection<Image> logoImages)
    {
        Dictionary<string, HashSet<string>> imageIdsByOwner = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (Image image in logoImages)
        {
            if (!image.IsPublished || string.IsNullOrWhiteSpace(image.Id) || string.IsNullOrWhiteSpace(image.OwnerId))
            {
                continue;
            }

            if (!imageIdsByOwner.TryGetValue(image.OwnerId, out HashSet<string>? imageIds))
            {
                imageIds = new HashSet<string>(StringComparer.Ordinal);
                imageIdsByOwner[image.OwnerId] = imageIds;
            }

            imageIds.Add(image.Id);
        }

        return imageIdsByOwner;
    }

    public static string? ResolveLogoImageId(
        AttractionManufacturer entity,
        IReadOnlyDictionary<string, string> logoImageIds,
        IReadOnlyDictionary<string, HashSet<string>> publishedLogoImageIdsByOwner)
    {
        if (!string.IsNullOrWhiteSpace(entity.Id)
            && !string.IsNullOrWhiteSpace(entity.CurrentLogoImageId)
            && publishedLogoImageIdsByOwner.TryGetValue(entity.Id, out HashSet<string>? publishedLogoImageIds)
            && publishedLogoImageIds.Contains(entity.CurrentLogoImageId))
        {
            return entity.CurrentLogoImageId;
        }

        return !string.IsNullOrWhiteSpace(entity.Id) && logoImageIds.TryGetValue(entity.Id, out string? logoImageId)
            ? logoImageId
            : null;
    }

    public static string? ResolveMainImageId(
        AttractionManufacturer entity,
        IReadOnlyDictionary<string, string> logoImageIds,
        IReadOnlyDictionary<string, string> manufacturerImageIds,
        IReadOnlyDictionary<string, HashSet<string>> publishedLogoImageIdsByOwner)
    {
        string? logoImageId = ResolveLogoImageId(entity, logoImageIds, publishedLogoImageIdsByOwner);
        if (!string.IsNullOrWhiteSpace(logoImageId))
        {
            return logoImageId;
        }

        return !string.IsNullOrWhiteSpace(entity.Id) && manufacturerImageIds.TryGetValue(entity.Id, out string? imageId)
            ? imageId
            : null;
    }
}
