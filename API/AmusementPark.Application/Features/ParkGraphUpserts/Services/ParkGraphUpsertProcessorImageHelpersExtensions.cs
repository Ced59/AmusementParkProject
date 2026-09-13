using System.Text.Json;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using System.Globalization;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Localization;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Core.Geo;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Parks.Services;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using System.Text;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;
internal static class ParkGraphUpsertProcessorImageHelpersExtensions
{
    internal static bool IsRemoteImportOwnerSupported(ImageOwnerType ownerType)
    {
        return ownerType is ImageOwnerType.Park or ImageOwnerType.ParkItem or ImageOwnerType.ParkOperator or ImageOwnerType.AttractionManufacturer or ImageOwnerType.ParkFounder or ImageOwnerType.StandaloneAttraction;
    }

    internal static ImageCategory ResolveDefaultImageCategory(ImageOwnerType ownerType)
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

    internal static void AddSkippedUnresolvedImageOwnerChange(Image image, ImageOwnerType requestedOwnerType, string? resolvedOwnerId, ParkGraphUpsertResult result)
    {
        ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("Image", image.Id, null, image.OriginalFileName ?? image.Id, "Skipped", "ownerKey");
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "ownerType", image.OwnerType.ToString(), requestedOwnerType.ToString());
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "ownerId", image.OwnerId, resolvedOwnerId);
        result.Warnings.Add($"Image '{image.Id}' ignored: owner could not be resolved.");
        result.Changes.Add(change);
    }
}
