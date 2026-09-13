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
internal static class ParkGraphUpsertProcessorImageOwnersExtensions
{
    internal static bool ResolveGraphImageOwner(JsonElement patch, Park? park, Dictionary<string, string> itemKeys, Dictionary<string, string> founderKeys, Dictionary<string, string> operatorKeys, Dictionary<string, string> manufacturerKeys, ImageOwnerType requestedOwnerType, string? ownerId, out ImageOwnerType ownerType, out string? resolvedOwnerId)
    {
        ownerType = requestedOwnerType;
        resolvedOwnerId = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ownerId);
        string? ownerKey = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "ownerKey");
        if (string.Equals(ownerKey, "park", StringComparison.OrdinalIgnoreCase))
        {
            if (park is null)
            {
                return false;
            }

            ownerType = ImageOwnerType.Park;
            resolvedOwnerId = park.Id;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(ownerKey))
        {
            if (ParkGraphUpsertProcessorImagesExtensions.TryResolvePrefixedOwnerKey(ownerKey, "operator:", operatorKeys, out string? operatorId))
            {
                ownerType = ImageOwnerType.ParkOperator;
                resolvedOwnerId = operatorId;
                return true;
            }

            if (ParkGraphUpsertProcessorImagesExtensions.TryResolvePrefixedOwnerKey(ownerKey, "founder:", founderKeys, out string? founderId))
            {
                ownerType = ImageOwnerType.ParkFounder;
                resolvedOwnerId = founderId;
                return true;
            }

            if (ParkGraphUpsertProcessorImagesExtensions.TryResolvePrefixedOwnerKey(ownerKey, "manufacturer:", manufacturerKeys, out string? manufacturerId))
            {
                ownerType = ImageOwnerType.AttractionManufacturer;
                resolvedOwnerId = manufacturerId;
                return true;
            }

            if (ParkGraphUpsertProcessorImagesExtensions.TryResolvePrefixedOwnerKey(ownerKey, "standalone-attraction:", itemKeys, out string? standaloneAttractionId) || ParkGraphUpsertProcessorImagesExtensions.TryResolvePrefixedOwnerKey(ownerKey, "standaloneAttraction:", itemKeys, out standaloneAttractionId))
            {
                ownerType = ImageOwnerType.StandaloneAttraction;
                resolvedOwnerId = standaloneAttractionId;
                return true;
            }
        }

        if (requestedOwnerType == ImageOwnerType.StandaloneAttraction)
        {
            if (!string.IsNullOrWhiteSpace(resolvedOwnerId))
            {
                return true;
            }

            return ParkGraphUpsertProcessorImagesExtensions.TryResolveOwnerKey(ownerKey, itemKeys, ParkGraphUpsertProcessorImagesExtensions.BuildStandaloneAttractionNameKey(ownerKey), out resolvedOwnerId);
        }

        if (requestedOwnerType == ImageOwnerType.ParkItem)
        {
            return ParkGraphUpsertProcessorImagesExtensions.TryResolveOwnerKey(ownerKey, itemKeys, ParkGraphUpsertProcessorImagesExtensions.BuildItemNameKey(ownerKey), out resolvedOwnerId);
        }

        if (requestedOwnerType == ImageOwnerType.ParkOperator)
        {
            return ParkGraphUpsertProcessorImagesExtensions.TryResolveOwnerKey(ownerKey, operatorKeys, null, out resolvedOwnerId);
        }

        if (requestedOwnerType == ImageOwnerType.ParkFounder)
        {
            return ParkGraphUpsertProcessorImagesExtensions.TryResolveOwnerKey(ownerKey, founderKeys, null, out resolvedOwnerId);
        }

        if (requestedOwnerType == ImageOwnerType.AttractionManufacturer)
        {
            return ParkGraphUpsertProcessorImagesExtensions.TryResolveOwnerKey(ownerKey, manufacturerKeys, null, out resolvedOwnerId);
        }

        if (!string.IsNullOrWhiteSpace(ownerKey) && ParkGraphUpsertProcessorImagesExtensions.TryResolveOwnerKey(ownerKey, itemKeys, ParkGraphUpsertProcessorImagesExtensions.BuildItemNameKey(ownerKey), out string? itemId))
        {
            ownerType = ImageOwnerType.ParkItem;
            resolvedOwnerId = itemId;
            return true;
        }

        if (string.IsNullOrWhiteSpace(resolvedOwnerId))
        {
            if (park is null)
            {
                return false;
            }

            ownerType = ImageOwnerType.Park;
            resolvedOwnerId = park.Id;
        }

        return !string.IsNullOrWhiteSpace(resolvedOwnerId);
    }
}
