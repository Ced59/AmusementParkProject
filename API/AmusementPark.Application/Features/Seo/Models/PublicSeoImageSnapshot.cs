using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.Seo.Models;

public sealed record PublicSeoImageSnapshot(
    string Id,
    ImageOwnerType OwnerType,
    string OwnerId,
    ImageCategory Category,
    bool IsPublished)
{
    public static PublicSeoImageSnapshot? FromImage(Image? image)
    {
        if (image is null ||
            string.IsNullOrWhiteSpace(image.Id) ||
            string.IsNullOrWhiteSpace(image.OwnerId) ||
            !IsPublicGalleryImage(image.OwnerType, image.Category))
        {
            return null;
        }

        return new PublicSeoImageSnapshot(
            image.Id.Trim(),
            image.OwnerType,
            image.OwnerId.Trim(),
            image.Category,
            image.IsPublished);
    }

    public static IReadOnlyCollection<PublicSeoImageSnapshot> FromImages(IEnumerable<Image?> images)
    {
        ArgumentNullException.ThrowIfNull(images);

        List<PublicSeoImageSnapshot> snapshots = new List<PublicSeoImageSnapshot>();
        foreach (Image? image in images)
        {
            PublicSeoImageSnapshot? snapshot = FromImage(image);
            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
        }

        return snapshots;
    }

    private static bool IsPublicGalleryImage(ImageOwnerType ownerType, ImageCategory category)
    {
        return (ownerType == ImageOwnerType.Park && (category == ImageCategory.Park || category == ImageCategory.Logo)) ||
               (ownerType == ImageOwnerType.ParkItem && category == ImageCategory.ParkItem);
    }
}
