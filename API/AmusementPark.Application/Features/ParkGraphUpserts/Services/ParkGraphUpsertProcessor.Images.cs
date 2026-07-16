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
    private async Task ProcessImagesAsync(
        JsonElement root,
        Park? park,
        Dictionary<string, string> itemKeys,
        Dictionary<string, string> founderKeys,
        Dictionary<string, string> operatorKeys,
        Dictionary<string, string> manufacturerKeys,
        Dictionary<string, string> manufacturerIdRemaps,
        Dictionary<string, string> imageKeys,
        ParkGraphUpsertResult result,
        bool apply,
        CancellationToken cancellationToken)
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
                string? importedImageId = await this.ProcessRemoteImageAsync(patch, park, itemKeys, founderKeys, operatorKeys, manufacturerKeys, manufacturerIdRemaps, result, apply, cancellationToken);
                RegisterImageKey(patch, importedImageId, imageKeys);
                continue;
            }

            Image? image = await this.imageRepository.GetByIdAsync(imageId, cancellationToken);
            if (image is null)
            {
                result.Warnings.Add($"Image '{imageId}' introuvable : rattachement ignoré.");
                continue;
            }

            RegisterImageKey(patch, image.Id, imageKeys);

            ImageOwnerType previousOwnerType = image.OwnerType;
            string? previousOwnerId = image.OwnerId;
            ImageCategory previousCategory = image.Category;
            bool wasCurrent = image.IsCurrent;

            bool hasOwnerPatch = HasProperty(patch, "ownerType") || HasProperty(patch, "ownerId") || HasProperty(patch, "ownerKey");
            ImageOwnerType ownerType = image.OwnerType;
            string? resolvedOwnerId = image.OwnerId;
            bool ownerResolved = true;
            if (hasOwnerPatch)
            {
                string? ownerId = ReadString(patch, "ownerId");
                ownerResolved = ResolveGraphImageOwner(
                    patch,
                    park,
                    itemKeys,
                    founderKeys,
                    operatorKeys,
                    manufacturerKeys,
                    ResolveRequestedImageOwnerType(patch),
                    ownerId,
                    out ownerType,
                    out resolvedOwnerId);
                if (ownerType == ImageOwnerType.AttractionManufacturer)
                {
                    resolvedOwnerId = RemapId(manufacturerIdRemaps, resolvedOwnerId);
                }

                if (!ownerResolved || string.IsNullOrWhiteSpace(resolvedOwnerId))
                {
                    AddSkippedUnresolvedImageOwnerChange(image, ownerType, resolvedOwnerId, result);
                    continue;
                }
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
            string? sourceUrl = image.SourceUrl;
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
            PatchString(patch, "sourceUrl", image.SourceUrl, value => sourceUrl = value, change);
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
            bool categoryChanged = image.Category != category;
            bool currentScopeChanged = ownerChanged || categoryChanged;

            bool setAsCurrent = ReadBool(patch, "setAsCurrent") == true;
            bool shouldSetCurrent = setAsCurrent && (currentScopeChanged || !image.IsCurrent);
            bool shouldClearCurrent = currentScopeChanged && image.IsCurrent && !setAsCurrent;
            if (shouldSetCurrent)
            {
                AddChange(change, "isCurrent", image.IsCurrent, true);
            }
            else if (shouldClearCurrent)
            {
                AddChange(change, "isCurrent", image.IsCurrent, false);
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
                SourceUrl = sourceUrl,
                OwnerType = ownerType,
                OwnerId = resolvedOwnerId,
                IsCurrent = shouldClearCurrent ? false : null,
            };

            if (change.Fields.Count > 0)
            {
                change.ChangeType = "Updated";
            }

            if (apply)
            {
                bool shouldUpdateMetadata = metadataChanged || (ownerChanged && !shouldSetCurrent) || shouldClearCurrent;
                if (shouldUpdateMetadata)
                {
                    Image? metadataUpdated = await this.imageRepository.UpdateMetadataAsync(image.Id, metadata, cancellationToken);
                    image = metadataUpdated ?? image;
                }

                if (currentScopeChanged && wasCurrent)
                {
                    await this.SynchronizeCurrentImageOwnerScopeAsync(
                        previousOwnerType,
                        previousOwnerId,
                        previousCategory,
                        park,
                        cancellationToken);
                }

                if (shouldSetCurrent && resolvedOwnerId is not null)
                {
                    Image? current = await this.imageRepository.SetCurrentAsync(image.Id, ownerType, resolvedOwnerId, cancellationToken);
                    await this.SynchronizeCurrentImageOwnerAsync(current, park, cancellationToken);
                }
            }

            result.Changes.Add(change);
        }
    }

    private async Task<string?> ProcessRemoteImageAsync(
        JsonElement patch,
        Park? park,
        Dictionary<string, string> itemKeys,
        Dictionary<string, string> founderKeys,
        Dictionary<string, string> operatorKeys,
        Dictionary<string, string> manufacturerKeys,
        Dictionary<string, string> manufacturerIdRemaps,
        ParkGraphUpsertResult result,
        bool apply,
        CancellationToken cancellationToken)
    {
        string? sourceUrl = ReadRemoteImageSourceUrl(patch);
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            result.Warnings.Add("Image ignored: imageId or sourceUrl is required.");
            return null;
        }

        ImageOwnerType requestedOwnerType = ResolveRequestedImageOwnerType(patch);
        string? ownerId = ReadString(patch, "ownerId");
        string? ownerKey = ReadString(patch, "ownerKey");
        bool ownerResolved = ResolveGraphImageOwner(
            patch,
            park,
            itemKeys,
            founderKeys,
            operatorKeys,
            manufacturerKeys,
            requestedOwnerType,
            ownerId,
            out ImageOwnerType resolvedOwnerType,
            out string? resolvedOwnerId);
        if (resolvedOwnerType == ImageOwnerType.AttractionManufacturer)
        {
            resolvedOwnerId = RemapId(manufacturerIdRemaps, resolvedOwnerId);
        }

        ImageCategory category = ReadEnumNullable<ImageCategory>(patch, "category") ?? ResolveDefaultImageCategory(resolvedOwnerType);
        string displayName = ReadString(patch, "description") ?? ownerKey ?? sourceUrl;
        ParkGraphUpsertChange change = BuildEntityChange("Image", null, ownerKey, displayName, "Created", "sourceUrl");
        AddChange(change, "sourceUrl", null, sourceUrl);
        AddChange(change, "ownerType", null, resolvedOwnerType);
        AddChange(change, "ownerId", null, resolvedOwnerId);
        AddChange(change, "category", null, category);
        AddChange(change, "isPublished", null, ReadBool(patch, "isPublished") ?? true);

        bool setAsCurrent = ReadBool(patch, "setAsCurrent") ?? category == ImageCategory.Logo;
        bool withWatermark = ShouldApplyRemoteImageWatermark(category, ReadBool(patch, "withWatermark"));
        AddChange(change, "setAsCurrent", null, setAsCurrent);
        AddChange(change, "withWatermark", null, withWatermark);

        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out Uri? sourceUri)
            || (sourceUri.Scheme != Uri.UriSchemeHttp && sourceUri.Scheme != Uri.UriSchemeHttps))
        {
            result.Errors.Add($"Remote image sourceUrl is invalid: '{sourceUrl}'.");
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            return null;
        }

        if (!ownerResolved || string.IsNullOrWhiteSpace(resolvedOwnerId))
        {
            result.Warnings.Add($"Remote image ignored: owner could not be resolved for '{sourceUrl}'.");
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            return null;
        }

        if (!IsRemoteImportOwnerSupported(resolvedOwnerType))
        {
            result.Warnings.Add($"Remote image ignored: ownerType '{resolvedOwnerType}' is not supported by JSON upsert.");
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            return null;
        }

        Image? duplicateImage = await this.imageRepository.GetByOwnerAndSourceUrlAsync(resolvedOwnerType, resolvedOwnerId, sourceUrl, cancellationToken);
        if (duplicateImage is not null)
        {
            result.Warnings.Add($"Remote image skipped: sourceUrl already exists for {resolvedOwnerType} '{resolvedOwnerId}' as image '{duplicateImage.Id}': '{sourceUrl}'.");
            AddChange(change, "duplicateImageId", null, duplicateImage.Id);
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            return duplicateImage.Id;
        }

        string? importedImageId = null;
        if (apply)
        {
            RemoteImageImportRequest request = new RemoteImageImportRequest
            {
                SourceUrl = sourceUrl,
                Category = category,
                OwnerType = resolvedOwnerType,
                OwnerId = resolvedOwnerId,
                Description = ReadString(patch, "description"),
                WithWatermark = withWatermark,
                SetAsCurrent = false,
            };

            Image? image = await this.remoteImageImporter.ImportAsync(request, cancellationToken);
            if (image is null)
            {
                result.Errors.Add($"Remote image was not imported: '{sourceUrl}'.");
                change.ChangeType = "Skipped";
                result.Changes.Add(change);
                return null;
            }

            importedImageId = image.Id;
            change.EntityId = image.Id;
            AddChange(change, "imageId", null, image.Id);
            AddChange(change, "internalUrl", null, BuildInternalImageUrl(image.Id));

            ImageMetadataUpdate metadata = new ImageMetadataUpdate
            {
                Description = image.Description,
                AltTexts = HasProperty(patch, "altTexts")
                    ? ToLocalizedTextValues(ReadLocalizedTexts(GetArray(patch, "altTexts")))
                    : ToLocalizedTextValues(image.AltTexts),
                Captions = HasProperty(patch, "captions")
                    ? ToLocalizedTextValues(ReadLocalizedTexts(GetArray(patch, "captions")))
                    : ToLocalizedTextValues(image.Captions),
                Credits = HasProperty(patch, "credits")
                    ? ToLocalizedTextValues(ReadLocalizedTexts(GetArray(patch, "credits")))
                    : ToLocalizedTextValues(image.Credits),
                TagIds = HasProperty(patch, "tagIds") ? ReadStringArray(GetArray(patch, "tagIds")) : image.TagIds,
                GeoLocation = HasProperty(patch, "geoLocation")
                    ? ReadGeoPointValue(patch, "geoLocation")
                    : image.GeoLocation is null
                        ? null
                        : new GeoPointValue(image.GeoLocation.Latitude, image.GeoLocation.Longitude),
                Category = category,
                IsPublished = ReadBool(patch, "isPublished") ?? true,
                SourceUrl = sourceUrl,
            };

            if (HasRemoteMetadataPatch(patch))
            {
                await this.imageRepository.UpdateMetadataAsync(image.Id, metadata, cancellationToken);
            }

            if (setAsCurrent)
            {
                Image? current = await this.imageRepository.SetCurrentAsync(image.Id, resolvedOwnerType, resolvedOwnerId, cancellationToken);
                await this.SynchronizeCurrentImageOwnerAsync(current, park, cancellationToken);
            }
        }

        result.Changes.Add(change);
        return importedImageId;
    }

    private async Task SynchronizeCurrentImageOwnerAsync(Image? current, Park? targetPark, CancellationToken cancellationToken)
    {
        if (current is null
            || string.IsNullOrWhiteSpace(current.OwnerId))
        {
            return;
        }

        await this.SynchronizeCurrentImageOwnerScopeAsync(current.OwnerType, current.OwnerId, current.Category, targetPark, cancellationToken);
    }

    private async Task SynchronizeCurrentImageOwnerScopeAsync(
        ImageOwnerType ownerType,
        string? ownerId,
        ImageCategory category,
        Park? targetPark,
        CancellationToken cancellationToken)
    {
        if (ownerType == ImageOwnerType.None || string.IsNullOrWhiteSpace(ownerId))
        {
            return;
        }

        if (ownerType == ImageOwnerType.Park && category == ImageCategory.Logo)
        {
            Park? ownerPark = targetPark is not null && string.Equals(ownerId, targetPark.Id, StringComparison.Ordinal)
                ? targetPark
                : await this.parkRepository.GetByIdAsync(ownerId, true, cancellationToken);
            if (ownerPark is null)
            {
                return;
            }

            Image? currentLogo = await this.imageRepository.GetCurrentByOwnerAsync(ImageOwnerType.Park, ownerId, ImageCategory.Logo, cancellationToken);
            ownerPark.CurrentLogoImageId = currentLogo?.Id;
            await this.parkRepository.UpdateAsync(ownerPark.Id, ownerPark, cancellationToken);
            return;
        }

        if (ownerType == ImageOwnerType.AttractionManufacturer && category == ImageCategory.Logo)
        {
            AttractionManufacturer? manufacturer = await this.attractionManufacturerRepository.GetByIdAsync(ownerId, cancellationToken);
            if (manufacturer is null)
            {
                return;
            }

            Image? currentLogo = await this.imageRepository.GetCurrentByOwnerAsync(ImageOwnerType.AttractionManufacturer, ownerId, ImageCategory.Logo, cancellationToken);
            manufacturer.CurrentLogoImageId = currentLogo?.Id;
            await this.attractionManufacturerRepository.UpdateAsync(manufacturer.Id, manufacturer, cancellationToken);
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, manufacturer.Id, cancellationToken);
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

    private static string? ReadRemoteImageSourceUrl(JsonElement patch)
    {
        return ReadString(patch, "sourceUrl")
            ?? ReadString(patch, "remoteUrl")
            ?? ReadString(patch, "externalUrl");
    }

    private static ImageOwnerType ResolveRequestedImageOwnerType(JsonElement patch)
    {
        string? ownerTypeText = ReadString(patch, "ownerType");
        if (!string.IsNullOrWhiteSpace(ownerTypeText))
        {
            return ReadEnumFromText(ownerTypeText, ImageOwnerType.Park);
        }

        string? ownerKey = ReadString(patch, "ownerKey");
        if (!string.IsNullOrWhiteSpace(ownerKey))
        {
            if (ownerKey.StartsWith("operator:", StringComparison.OrdinalIgnoreCase))
            {
                return ImageOwnerType.ParkOperator;
            }

            if (ownerKey.StartsWith("founder:", StringComparison.OrdinalIgnoreCase))
            {
                return ImageOwnerType.ParkFounder;
            }

            if (ownerKey.StartsWith("manufacturer:", StringComparison.OrdinalIgnoreCase))
            {
                return ImageOwnerType.AttractionManufacturer;
            }

            if (ownerKey.StartsWith("standalone-attraction:", StringComparison.OrdinalIgnoreCase)
                || ownerKey.StartsWith("standaloneAttraction:", StringComparison.OrdinalIgnoreCase))
            {
                return ImageOwnerType.StandaloneAttraction;
            }
        }

        return ImageOwnerType.Park;
    }

    private static string? BuildItemNameKey(string? ownerKey)
    {
        return string.IsNullOrWhiteSpace(ownerKey) ? null : $"item:{NormalizeKey(ownerKey)}";
    }

    private static string? BuildStandaloneAttractionNameKey(string? ownerKey)
    {
        return string.IsNullOrWhiteSpace(ownerKey) ? null : $"standalone-attraction:{NormalizeKey(ownerKey)}";
    }

    private static bool TryResolveOwnerKey(string? ownerKey, Dictionary<string, string> ownerKeys, string? fallbackKey, out string? ownerId)
    {
        ownerId = null;
        if (!string.IsNullOrWhiteSpace(ownerKey) && ownerKeys.TryGetValue(ownerKey, out string? directOwnerId) && !string.IsNullOrWhiteSpace(directOwnerId))
        {
            ownerId = directOwnerId;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(fallbackKey) && ownerKeys.TryGetValue(fallbackKey, out string? fallbackOwnerId) && !string.IsNullOrWhiteSpace(fallbackOwnerId))
        {
            ownerId = fallbackOwnerId;
            return true;
        }

        return false;
    }

    private static bool TryResolvePrefixedOwnerKey(string ownerKey, string prefix, Dictionary<string, string> ownerKeys, out string? ownerId)
    {
        ownerId = null;
        if (!ownerKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string key = ownerKey[prefix.Length..].Trim();
        return TryResolveOwnerKey(key, ownerKeys, null, out ownerId);
    }

    private static bool HasRemoteMetadataPatch(JsonElement patch)
    {
        return HasProperty(patch, "altTexts")
            || HasProperty(patch, "captions")
            || HasProperty(patch, "credits")
            || HasProperty(patch, "tagIds")
            || HasProperty(patch, "geoLocation")
            || HasProperty(patch, "isPublished");
    }

    private static bool ShouldApplyRemoteImageWatermark(ImageCategory category, bool? requestedWithWatermark)
    {
        return !IsLogoCategory(category) && requestedWithWatermark == true;
    }

    private static bool IsLogoCategory(ImageCategory category)
    {
        return category is ImageCategory.Logo;
    }

    private static string BuildInternalImageUrl(string imageId)
    {
        return $"/images/{imageId}";
    }
}
