using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private static bool IsRemoteImportOwnerSupported(ImageOwnerType ownerType)
    {
        return ownerType is ImageOwnerType.Park
            or ImageOwnerType.ParkItem
            or ImageOwnerType.ParkOperator
            or ImageOwnerType.AttractionManufacturer
            or ImageOwnerType.ParkFounder
            or ImageOwnerType.StandaloneAttraction;
    }

    private static ImageCategory ResolveDefaultImageCategory(ImageOwnerType ownerType)
    {
        return ownerType switch
        {
            ImageOwnerType.Park => ImageCategory.Park,
            ImageOwnerType.ParkItem => ImageCategory.ParkItem,
            ImageOwnerType.ParkOperator => ImageCategory.Operator,
            ImageOwnerType.AttractionManufacturer => ImageCategory.Manufacturer,
            ImageOwnerType.ParkFounder => ImageCategory.Founder,
            ImageOwnerType.StandaloneAttraction => ImageCategory.StandaloneAttraction,
            _ => ImageCategory.Park,
        };
    }

    private static void AddSkippedUnresolvedImageOwnerChange(Image image, ImageOwnerType requestedOwnerType, string? resolvedOwnerId, ParkGraphUpsertResult result)
    {
        ParkGraphUpsertChange change = BuildEntityChange("Image", image.Id, null, image.OriginalFileName ?? image.Id, "Skipped", "ownerKey");
        AddChange(change, "ownerType", image.OwnerType.ToString(), requestedOwnerType.ToString());
        AddChange(change, "ownerId", image.OwnerId, resolvedOwnerId);
        result.Warnings.Add($"Image '{image.Id}' ignored: owner could not be resolved.");
        result.Changes.Add(change);
    }
}
