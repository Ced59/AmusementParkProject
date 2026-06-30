using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private static void RegisterImageKey(JsonElement patch, string? imageId, Dictionary<string, string> imageKeys)
    {
        if (string.IsNullOrWhiteSpace(imageId))
        {
            return;
        }

        string? imageKey = NormalizeString(ReadString(patch, "key") ?? ReadString(patch, "imageKey") ?? ReadString(patch, "alias"));
        if (string.IsNullOrWhiteSpace(imageKey))
        {
            return;
        }

        imageKeys[imageKey] = imageId;
        imageKeys[NormalizeKey(imageKey)] = imageId;
    }

    private static List<string> ReadHistoryImageIds(
        JsonElement element,
        Dictionary<string, string> imageKeys,
        ParkGraphUpsertResult result,
        bool apply,
        string context)
    {
        List<string> imageIds = ReadStringArray(GetArray(element, "imageIds"));
        JsonElement? imageKeyArray = GetArray(element, "imageKeys");
        if (imageKeyArray is null)
        {
            return imageIds;
        }

        foreach (JsonElement item in imageKeyArray.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            string? imageKey = NormalizeString(item.GetString());
            if (string.IsNullOrWhiteSpace(imageKey))
            {
                continue;
            }

            if (TryResolveImageKey(imageKey, imageKeys, out string? resolvedImageId) && !string.IsNullOrWhiteSpace(resolvedImageId))
            {
                imageIds.Add(resolvedImageId);
                continue;
            }

            if (apply)
            {
                result.Warnings.Add($"La clé image '{imageKey}' est introuvable pour {context}.");
            }
        }

        return imageIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static bool TryReadHistoryImageIdPatch(
        JsonElement element,
        string idPropertyName,
        string keyPropertyName,
        string fallbackKeyPropertyName,
        Dictionary<string, string> imageKeys,
        ParkGraphUpsertResult result,
        bool apply,
        string context,
        out string? imageId)
    {
        imageId = null;
        if (HasProperty(element, idPropertyName))
        {
            imageId = NormalizeString(ReadString(element, idPropertyName));
            return true;
        }

        string? imageKey = NormalizeString(ReadString(element, keyPropertyName) ?? ReadString(element, fallbackKeyPropertyName));
        if (string.IsNullOrWhiteSpace(imageKey))
        {
            return false;
        }

        if (TryResolveImageKey(imageKey, imageKeys, out string? resolvedImageId))
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

    private static bool TryResolveImageKey(string imageKey, Dictionary<string, string> imageKeys, out string? imageId)
    {
        if (imageKeys.TryGetValue(imageKey, out imageId) && !string.IsNullOrWhiteSpace(imageId))
        {
            return true;
        }

        return imageKeys.TryGetValue(NormalizeKey(imageKey), out imageId) && !string.IsNullOrWhiteSpace(imageId);
    }
}
