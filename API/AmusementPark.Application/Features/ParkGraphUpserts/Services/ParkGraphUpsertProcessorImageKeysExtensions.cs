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
internal static class ParkGraphUpsertProcessorImageKeysExtensions
{
    internal static void RegisterImageKey(JsonElement patch, string? imageId, Dictionary<string, string> imageKeys)
    {
        if (string.IsNullOrWhiteSpace(imageId))
        {
            return;
        }

        string? imageKey = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "key") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "imageKey") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "alias"));
        if (string.IsNullOrWhiteSpace(imageKey))
        {
            return;
        }

        imageKeys[imageKey] = imageId;
        imageKeys[ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(imageKey)] = imageId;
    }

    internal static List<string> ReadHistoryImageIds(JsonElement element, Dictionary<string, string> imageKeys, ParkGraphUpsertResult result, bool apply, string context, IReadOnlyCollection<string> previousImageIds)
    {
        List<string> imageIds = ParkGraphUpsertProcessorLocalizedTextExtensions.ReadStringArray(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(element, "imageIds"));
        JsonElement? imageKeyArray = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(element, "imageKeys");
        if (imageKeyArray is null)
        {
            return imageIds;
        }

        bool hasUnresolvedImageKey = false;
        foreach (JsonElement item in imageKeyArray.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string? imageKey = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(item.GetString());
            if (string.IsNullOrWhiteSpace(imageKey))
            {
                continue;
            }

            if (ParkGraphUpsertProcessorImageKeysExtensions.TryResolveImageKey(imageKey, imageKeys, out string? resolvedImageId) && !string.IsNullOrWhiteSpace(resolvedImageId))
            {
                imageIds.Add(resolvedImageId);
                continue;
            }

            if (apply)
            {
                result.Warnings.Add($"La clé image '{imageKey}' est introuvable pour {context}.");
            }

            hasUnresolvedImageKey = true;
        }

        if (apply && hasUnresolvedImageKey)
        {
            imageIds.AddRange(previousImageIds);
        }

        return imageIds.Where(static id => !string.IsNullOrWhiteSpace(id)).Select(static id => id.Trim()).Distinct(StringComparer.Ordinal).ToList();
    }

    internal static bool HasHistoryImageIdPatch(JsonElement element, string idPropertyName, string keyPropertyName, string fallbackKeyPropertyName)
    {
        return ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(element, idPropertyName) || ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(element, keyPropertyName) || ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(element, fallbackKeyPropertyName);
    }

    internal static bool TryReadHistoryImageIdPatch(JsonElement element, string idPropertyName, string keyPropertyName, string fallbackKeyPropertyName, Dictionary<string, string> imageKeys, ParkGraphUpsertResult result, bool apply, string context, out string? imageId)
    {
        imageId = null;
        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(element, idPropertyName))
        {
            imageId = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, idPropertyName));
            return true;
        }

        string? imageKey = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, keyPropertyName) ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, fallbackKeyPropertyName));
        if (string.IsNullOrWhiteSpace(imageKey))
        {
            return false;
        }

        if (ParkGraphUpsertProcessorImageKeysExtensions.TryResolveImageKey(imageKey, imageKeys, out string? resolvedImageId))
        {
            imageId = resolvedImageId;
            return true;
        }

        if (apply)
        {
            result.Warnings.Add($"La clé image '{imageKey}' est introuvable pour {context}.");
        }

        return false;
    }

    internal static bool TryResolveImageKey(string imageKey, Dictionary<string, string> imageKeys, out string? imageId)
    {
        if (imageKeys.TryGetValue(imageKey, out imageId) && !string.IsNullOrWhiteSpace(imageId))
        {
            return true;
        }

        return imageKeys.TryGetValue(ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(imageKey), out imageId) && !string.IsNullOrWhiteSpace(imageId);
    }
}
