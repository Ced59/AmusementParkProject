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
            or ImageOwnerType.ParkFounder;
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
            _ => ImageCategory.Park,
        };
    }
}
