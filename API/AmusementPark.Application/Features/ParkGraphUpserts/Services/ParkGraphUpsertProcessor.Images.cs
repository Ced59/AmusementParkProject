using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.Search;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task ProcessImagesAsync(JsonElement root, Park park, Dictionary<string, string> itemKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("images", out JsonElement images) || images.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement patch in images.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? imageId = ReadString(patch, "imageId") ?? ReadString(patch, "id");
            if (string.IsNullOrWhiteSpace(imageId))
            {
                result.Warnings.Add("Image ignorée : imageId manquant. Le binaire reste géré par l'upload multipart existant.");
                continue;
            }

            Image? image = await this.imageRepository.GetByIdAsync(imageId, cancellationToken);
            if (image is null)
            {
                result.Warnings.Add($"Image '{imageId}' introuvable : rattachement ignoré.");
                continue;
            }

            bool hasOwnerPatch = HasProperty(patch, "ownerType") || HasProperty(patch, "ownerId") || HasProperty(patch, "ownerKey");
            ImageOwnerType ownerType = image.OwnerType;
            string? resolvedOwnerId = image.OwnerId;
            if (hasOwnerPatch)
            {
                string? ownerTypeText = ReadString(patch, "ownerType");
                string? ownerId = ReadString(patch, "ownerId");
                ResolveImageOwner(patch, park, itemKeys, ownerTypeText, ownerId, out ownerType, out resolvedOwnerId);
            }

            ParkGraphUpsertChange change = BuildEntityChange("Image", image.Id, null, image.OriginalFileName ?? image.Id, "Unchanged", "imageId");
            int ownerFieldStart = change.Fields.Count;
            if (hasOwnerPatch)
            {
                AddChange(change, "ownerType", image.OwnerType.ToString(), ownerType.ToString());
                AddChange(change, "ownerId", image.OwnerId, resolvedOwnerId);
            }

            bool ownerChanged = change.Fields.Count > ownerFieldStart;

            string? description = image.Description;
            ImageCategory category = image.Category;
            bool isPublished = image.IsPublished;
            List<LocalizedText> altTexts = image.AltTexts.ToList();
            List<LocalizedText> captions = image.Captions.ToList();
            List<LocalizedText> credits = image.Credits.ToList();
            List<string> tagIds = image.TagIds.ToList();
            GeoPointValue? geoLocation = image.GeoLocation is null
                ? null
                : new GeoPointValue(image.GeoLocation.Latitude, image.GeoLocation.Longitude);

            int metadataFieldStart = change.Fields.Count;
            PatchString(patch, "description", image.Description, value => description = value, change);
            PatchEnum(patch, "category", image.Category, value => category = value, change);
            PatchBool(patch, "isPublished", image.IsPublished, value => isPublished = value, change);

            if (HasProperty(patch, "altTexts"))
            {
                altTexts = PatchLocalizedTexts(image.AltTexts, GetArray(patch, "altTexts"), false, change, "altTexts");
            }

            if (HasProperty(patch, "captions"))
            {
                captions = PatchLocalizedTexts(image.Captions, GetArray(patch, "captions"), false, change, "captions");
            }

            if (HasProperty(patch, "credits"))
            {
                credits = PatchLocalizedTexts(image.Credits, GetArray(patch, "credits"), false, change, "credits");
            }

            if (HasProperty(patch, "tagIds"))
            {
                tagIds = ReadStringArray(GetArray(patch, "tagIds"));
                AddChange(change, "tagIds", DescribeStringCollection(image.TagIds), DescribeStringCollection(tagIds));
            }

            if (HasProperty(patch, "geoLocation"))
            {
                geoLocation = ReadGeoPointValue(patch, "geoLocation");
                AddChange(change, "geoLocation", FormatGeoPointValue(image.GeoLocation is null ? null : new GeoPointValue(image.GeoLocation.Latitude, image.GeoLocation.Longitude)), FormatGeoPointValue(geoLocation));
            }

            bool metadataChanged = change.Fields.Count > metadataFieldStart;

            bool setAsCurrent = ReadBool(patch, "setAsCurrent") == true;
            bool shouldSetCurrent = setAsCurrent && (ownerChanged || !image.IsCurrent);
            if (shouldSetCurrent)
            {
                AddChange(change, "isCurrent", image.IsCurrent, true);
            }

            ImageMetadataUpdate metadata = new ImageMetadataUpdate
            {
                Description = description,
                AltTexts = ToLocalizedTextValues(altTexts),
                Captions = ToLocalizedTextValues(captions),
                Credits = ToLocalizedTextValues(credits),
                TagIds = tagIds,
                GeoLocation = geoLocation,
                Category = category,
                IsPublished = isPublished,
            };

            if (change.Fields.Count > 0)
            {
                change.ChangeType = "Updated";
            }

            if (apply)
            {
                if (ownerChanged && resolvedOwnerId is not null && !setAsCurrent)
                {
                    Image? linked = await this.imageRepository.LinkAsync(image.Id, ownerType, resolvedOwnerId, cancellationToken);
                    image = linked ?? image;
                }

                if (metadataChanged)
                {
                    await this.imageRepository.UpdateMetadataAsync(image.Id, metadata, cancellationToken);
                }

                if (shouldSetCurrent && resolvedOwnerId is not null)
                {
                    await this.imageRepository.SetCurrentAsync(image.Id, ownerType, resolvedOwnerId, cancellationToken);
                }
            }

            result.Changes.Add(change);
        }
    }

    private static string DescribeStringCollection(IReadOnlyCollection<string> values)
    {
        return string.Join(", ", values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static value => value, StringComparer.Ordinal));
    }

    private static GeoPointValue? ReadGeoPointValue(JsonElement patch, string propertyName)
    {
        if (HasNull(patch, propertyName))
        {
            return null;
        }

        JsonElement? point = GetObject(patch, propertyName);
        if (point is null)
        {
            return null;
        }

        double? latitude = ReadDouble(point, "latitude");
        double? longitude = ReadDouble(point, "longitude");
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return null;
        }

        return new GeoPointValue(latitude.Value, longitude.Value);
    }
}
