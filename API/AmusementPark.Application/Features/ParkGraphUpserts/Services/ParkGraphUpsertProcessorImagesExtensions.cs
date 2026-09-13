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
internal static class ParkGraphUpsertProcessorImagesExtensions
{
    internal static async Task ProcessImagesAsync(this ParkGraphUpsertProcessor processorContext, JsonElement root, Park? park, Dictionary<string, string> itemKeys, Dictionary<string, string> founderKeys, Dictionary<string, string> operatorKeys, Dictionary<string, string> manufacturerKeys, Dictionary<string, string> manufacturerIdRemaps, Dictionary<string, string> imageKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
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

            string? imageId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "imageId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "id");
            if (string.IsNullOrWhiteSpace(imageId))
            {
                string? importedImageId = await processorContext.ProcessRemoteImageAsync(patch, park, itemKeys, founderKeys, operatorKeys, manufacturerKeys, manufacturerIdRemaps, result, apply, cancellationToken);
                ParkGraphUpsertProcessorImageKeysExtensions.RegisterImageKey(patch, importedImageId, imageKeys);
                continue;
            }

            Image? image = await processorContext.imageRepository.GetByIdAsync(imageId, cancellationToken);
            if (image is null)
            {
                result.Warnings.Add($"Image '{imageId}' introuvable : rattachement ignoré.");
                continue;
            }

            ParkGraphUpsertProcessorImageKeysExtensions.RegisterImageKey(patch, image.Id, imageKeys);
            ImageOwnerType previousOwnerType = image.OwnerType;
            string? previousOwnerId = image.OwnerId;
            ImageCategory previousCategory = image.Category;
            bool wasCurrent = image.IsCurrent;
            bool hasOwnerPatch = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "ownerType") || ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "ownerId") || ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "ownerKey");
            ImageOwnerType ownerType = image.OwnerType;
            string? resolvedOwnerId = image.OwnerId;
            bool ownerResolved = true;
            if (hasOwnerPatch)
            {
                string? ownerId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "ownerId");
                ownerResolved = ParkGraphUpsertProcessorImageOwnersExtensions.ResolveGraphImageOwner(patch, park, itemKeys, founderKeys, operatorKeys, manufacturerKeys, ParkGraphUpsertProcessorImagesExtensions.ResolveRequestedImageOwnerType(patch), ownerId, out ownerType, out resolvedOwnerId);
                if (ownerType == ImageOwnerType.AttractionManufacturer)
                {
                    resolvedOwnerId = ParkGraphUpsertProcessorMergeHelpersExtensions.RemapId(manufacturerIdRemaps, resolvedOwnerId);
                }

                if (!ownerResolved || string.IsNullOrWhiteSpace(resolvedOwnerId))
                {
                    ParkGraphUpsertProcessorImageHelpersExtensions.AddSkippedUnresolvedImageOwnerChange(image, ownerType, resolvedOwnerId, result);
                    continue;
                }
            }

            ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("Image", image.Id, null, image.OriginalFileName ?? image.Id, "Unchanged", "imageId");
            int ownerFieldStart = change.Fields.Count;
            if (hasOwnerPatch)
            {
                ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "ownerType", image.OwnerType.ToString(), ownerType.ToString());
                ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "ownerId", image.OwnerId, resolvedOwnerId);
            }

            bool ownerChanged = change.Fields.Count > ownerFieldStart;
            string? originalFileName = image.OriginalFileName;
            string? description = image.Description;
            string? sourceUrl = image.SourceUrl;
            ImageCategory category = image.Category;
            bool isPublished = image.IsPublished;
            List<LocalizedText> altTexts = image.AltTexts.ToList();
            List<LocalizedText> captions = image.Captions.ToList();
            List<LocalizedText> credits = image.Credits.ToList();
            List<string> tagIds = image.TagIds.ToList();
            GeoPointValue? geoLocation = image.GeoLocation is null ? null : new GeoPointValue(image.GeoLocation.Latitude, image.GeoLocation.Longitude);
            int metadataFieldStart = change.Fields.Count;
            ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "originalFileName", image.OriginalFileName, value => originalFileName = value, change);
            ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "description", image.Description, value => description = value, change);
            ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "sourceUrl", image.SourceUrl, value => sourceUrl = value, change);
            ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchEnum(patch, "category", image.Category, value => category = value, change);
            ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBool(patch, "isPublished", image.IsPublished, value => isPublished = value, change);
            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "altTexts"))
            {
                altTexts = ParkGraphUpsertProcessorLocalizedTextExtensions.PatchLocalizedTexts(image.AltTexts, ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "altTexts"), false, change, "altTexts");
            }

            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "captions"))
            {
                captions = ParkGraphUpsertProcessorLocalizedTextExtensions.PatchLocalizedTexts(image.Captions, ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "captions"), false, change, "captions");
            }

            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "credits"))
            {
                credits = ParkGraphUpsertProcessorLocalizedTextExtensions.PatchLocalizedTexts(image.Credits, ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "credits"), false, change, "credits");
            }

            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "tagIds"))
            {
                tagIds = ParkGraphUpsertProcessorLocalizedTextExtensions.ReadStringArray(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "tagIds"));
                ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "tagIds", ParkGraphUpsertProcessorImagesExtensions.DescribeStringCollection(image.TagIds), ParkGraphUpsertProcessorImagesExtensions.DescribeStringCollection(tagIds));
            }

            if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "geoLocation"))
            {
                geoLocation = ParkGraphUpsertProcessorImagesExtensions.ReadGeoPointValue(patch, "geoLocation");
                ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "geoLocation", ParkGraphUpsertProcessorLocalizedTextExtensions.FormatGeoPointValue(image.GeoLocation is null ? null : new GeoPointValue(image.GeoLocation.Latitude, image.GeoLocation.Longitude)), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatGeoPointValue(geoLocation));
            }

            bool metadataChanged = change.Fields.Count > metadataFieldStart;
            bool categoryChanged = image.Category != category;
            bool currentScopeChanged = ownerChanged || categoryChanged;
            bool setAsCurrent = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(patch, "setAsCurrent") == true;
            bool shouldSetCurrent = setAsCurrent && (currentScopeChanged || !image.IsCurrent);
            bool shouldClearCurrent = currentScopeChanged && image.IsCurrent && !setAsCurrent;
            if (shouldSetCurrent)
            {
                ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "isCurrent", image.IsCurrent, true);
            }
            else if (shouldClearCurrent)
            {
                ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "isCurrent", image.IsCurrent, false);
            }

            ImageMetadataUpdate metadata = new ImageMetadataUpdate
            {
                OriginalFileName = originalFileName,
                Description = description,
                AltTexts = ParkGraphUpsertProcessorLocalizedTextExtensions.ToLocalizedTextValues(altTexts),
                Captions = ParkGraphUpsertProcessorLocalizedTextExtensions.ToLocalizedTextValues(captions),
                Credits = ParkGraphUpsertProcessorLocalizedTextExtensions.ToLocalizedTextValues(credits),
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
                    Image? metadataUpdated = await processorContext.imageRepository.UpdateMetadataAsync(image.Id, metadata, cancellationToken);
                    image = metadataUpdated ?? image;
                }

                if (currentScopeChanged && wasCurrent)
                {
                    await processorContext.SynchronizeCurrentImageOwnerScopeAsync(previousOwnerType, previousOwnerId, previousCategory, park, cancellationToken);
                }

                if (shouldSetCurrent && resolvedOwnerId is not null)
                {
                    Image? current = await processorContext.imageRepository.SetCurrentAsync(image.Id, ownerType, resolvedOwnerId, cancellationToken);
                    await processorContext.SynchronizeCurrentImageOwnerAsync(current, park, cancellationToken);
                }
            }

            result.Changes.Add(change);
        }
    }

    internal static async Task<string?> ProcessRemoteImageAsync(this ParkGraphUpsertProcessor processorContext, JsonElement patch, Park? park, Dictionary<string, string> itemKeys, Dictionary<string, string> founderKeys, Dictionary<string, string> operatorKeys, Dictionary<string, string> manufacturerKeys, Dictionary<string, string> manufacturerIdRemaps, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        string? sourceUrl = ParkGraphUpsertProcessorImagesExtensions.ReadRemoteImageSourceUrl(patch);
        if (string.IsNullOrWhiteSpace(sourceUrl))
        {
            result.Warnings.Add("Image ignored: imageId or sourceUrl is required.");
            return null;
        }

        ImageOwnerType requestedOwnerType = ParkGraphUpsertProcessorImagesExtensions.ResolveRequestedImageOwnerType(patch);
        string? ownerId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "ownerId");
        string? ownerKey = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "ownerKey");
        bool ownerResolved = ParkGraphUpsertProcessorImageOwnersExtensions.ResolveGraphImageOwner(patch, park, itemKeys, founderKeys, operatorKeys, manufacturerKeys, requestedOwnerType, ownerId, out ImageOwnerType resolvedOwnerType, out string? resolvedOwnerId);
        if (resolvedOwnerType == ImageOwnerType.AttractionManufacturer)
        {
            resolvedOwnerId = ParkGraphUpsertProcessorMergeHelpersExtensions.RemapId(manufacturerIdRemaps, resolvedOwnerId);
        }

        ImageCategory category = ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnumNullable<ImageCategory>(patch, "category") ?? ParkGraphUpsertProcessorImageHelpersExtensions.ResolveDefaultImageCategory(resolvedOwnerType);
        string displayName = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "description") ?? ownerKey ?? sourceUrl;
        ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("Image", null, ownerKey, displayName, "Created", "sourceUrl");
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "sourceUrl", null, sourceUrl);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "ownerType", null, resolvedOwnerType);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "ownerId", null, resolvedOwnerId);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "category", null, category);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "isPublished", null, ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(patch, "isPublished") ?? true);
        bool setAsCurrent = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(patch, "setAsCurrent") ?? category == ImageCategory.Logo;
        bool withWatermark = ParkGraphUpsertProcessorImagesExtensions.ShouldApplyRemoteImageWatermark(category, ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(patch, "withWatermark"));
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "setAsCurrent", null, setAsCurrent);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "withWatermark", null, withWatermark);
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out Uri? sourceUri) || (sourceUri.Scheme != Uri.UriSchemeHttp && sourceUri.Scheme != Uri.UriSchemeHttps))
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

        if (!ParkGraphUpsertProcessorImageHelpersExtensions.IsRemoteImportOwnerSupported(resolvedOwnerType))
        {
            result.Warnings.Add($"Remote image ignored: ownerType '{resolvedOwnerType}' is not supported by JSON upsert.");
            change.ChangeType = "Skipped";
            result.Changes.Add(change);
            return null;
        }

        Image? duplicateImage = await processorContext.imageRepository.GetByOwnerAndSourceUrlAsync(resolvedOwnerType, resolvedOwnerId, sourceUrl, cancellationToken);
        if (duplicateImage is not null)
        {
            result.Warnings.Add($"Remote image skipped: sourceUrl already exists for {resolvedOwnerType} '{resolvedOwnerId}' as image '{duplicateImage.Id}': '{sourceUrl}'.");
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "duplicateImageId", null, duplicateImage.Id);
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
                Description = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "description"),
                WithWatermark = withWatermark,
                SetAsCurrent = false,
            };
            Image? image = await processorContext.remoteImageImporter.ImportAsync(request, cancellationToken);
            if (image is null)
            {
                result.Errors.Add($"Remote image was not imported: '{sourceUrl}'.");
                change.ChangeType = "Skipped";
                result.Changes.Add(change);
                return null;
            }

            importedImageId = image.Id;
            change.EntityId = image.Id;
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "imageId", null, image.Id);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "internalUrl", null, ParkGraphUpsertProcessorImagesExtensions.BuildInternalImageUrl(image.Id));
            ImageMetadataUpdate metadata = new ImageMetadataUpdate
            {
                Description = image.Description,
                AltTexts = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "altTexts") ? ParkGraphUpsertProcessorLocalizedTextExtensions.ToLocalizedTextValues(ParkGraphUpsertProcessorLocalizedTextExtensions.ReadLocalizedTexts(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "altTexts"))) : ParkGraphUpsertProcessorLocalizedTextExtensions.ToLocalizedTextValues(image.AltTexts),
                Captions = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "captions") ? ParkGraphUpsertProcessorLocalizedTextExtensions.ToLocalizedTextValues(ParkGraphUpsertProcessorLocalizedTextExtensions.ReadLocalizedTexts(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "captions"))) : ParkGraphUpsertProcessorLocalizedTextExtensions.ToLocalizedTextValues(image.Captions),
                Credits = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "credits") ? ParkGraphUpsertProcessorLocalizedTextExtensions.ToLocalizedTextValues(ParkGraphUpsertProcessorLocalizedTextExtensions.ReadLocalizedTexts(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "credits"))) : ParkGraphUpsertProcessorLocalizedTextExtensions.ToLocalizedTextValues(image.Credits),
                TagIds = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "tagIds") ? ParkGraphUpsertProcessorLocalizedTextExtensions.ReadStringArray(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "tagIds")) : image.TagIds,
                GeoLocation = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "geoLocation") ? ParkGraphUpsertProcessorImagesExtensions.ReadGeoPointValue(patch, "geoLocation") : image.GeoLocation is null ? null : new GeoPointValue(image.GeoLocation.Latitude, image.GeoLocation.Longitude),
                Category = category,
                IsPublished = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(patch, "isPublished") ?? true,
                SourceUrl = sourceUrl,
            };
            if (ParkGraphUpsertProcessorImagesExtensions.HasRemoteMetadataPatch(patch))
            {
                await processorContext.imageRepository.UpdateMetadataAsync(image.Id, metadata, cancellationToken);
            }

            if (setAsCurrent)
            {
                Image? current = await processorContext.imageRepository.SetCurrentAsync(image.Id, resolvedOwnerType, resolvedOwnerId, cancellationToken);
                await processorContext.SynchronizeCurrentImageOwnerAsync(current, park, cancellationToken);
            }
        }

        result.Changes.Add(change);
        return importedImageId;
    }

    internal static async Task SynchronizeCurrentImageOwnerAsync(this ParkGraphUpsertProcessor processorContext, Image? current, Park? targetPark, CancellationToken cancellationToken)
    {
        if (current is null || string.IsNullOrWhiteSpace(current.OwnerId))
        {
            return;
        }

        await processorContext.SynchronizeCurrentImageOwnerScopeAsync(current.OwnerType, current.OwnerId, current.Category, targetPark, cancellationToken);
    }

    internal static async Task SynchronizeCurrentImageOwnerScopeAsync(this ParkGraphUpsertProcessor processorContext, ImageOwnerType ownerType, string? ownerId, ImageCategory category, Park? targetPark, CancellationToken cancellationToken)
    {
        if (ownerType == ImageOwnerType.None || string.IsNullOrWhiteSpace(ownerId))
        {
            return;
        }

        if (ownerType == ImageOwnerType.Park && category == ImageCategory.Logo)
        {
            Park? ownerPark = targetPark is not null && string.Equals(ownerId, targetPark.Id, StringComparison.Ordinal) ? targetPark : await processorContext.parkRepository.GetByIdAsync(ownerId, true, cancellationToken);
            if (ownerPark is null)
            {
                return;
            }

            Image? currentLogo = await processorContext.imageRepository.GetCurrentByOwnerAsync(ImageOwnerType.Park, ownerId, ImageCategory.Logo, cancellationToken);
            ownerPark.CurrentLogoImageId = currentLogo?.Id;
            await processorContext.parkRepository.UpdateAsync(ownerPark.Id, ownerPark, cancellationToken);
            return;
        }

        if (ownerType == ImageOwnerType.AttractionManufacturer && category == ImageCategory.Logo)
        {
            AttractionManufacturer? manufacturer = await processorContext.attractionManufacturerRepository.GetByIdAsync(ownerId, cancellationToken);
            if (manufacturer is null)
            {
                return;
            }

            Image? currentLogo = await processorContext.imageRepository.GetCurrentByOwnerAsync(ImageOwnerType.AttractionManufacturer, ownerId, ImageCategory.Logo, cancellationToken);
            manufacturer.CurrentLogoImageId = currentLogo?.Id;
            await processorContext.attractionManufacturerRepository.UpdateAsync(manufacturer.Id, manufacturer, cancellationToken);
            await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, manufacturer.Id, cancellationToken);
        }
    }

    internal static string DescribeStringCollection(IReadOnlyCollection<string> values)
    {
        return string.Join(", ", values.Where(static value => !string.IsNullOrWhiteSpace(value)).Select(static value => value.Trim()).Distinct(StringComparer.Ordinal).OrderBy(static value => value, StringComparer.Ordinal));
    }

    internal static GeoPointValue? ReadGeoPointValue(JsonElement patch, string propertyName)
    {
        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasNull(patch, propertyName))
        {
            return null;
        }

        JsonElement? point = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, propertyName);
        if (point is null)
        {
            return null;
        }

        double? latitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(point, "latitude");
        double? longitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(point, "longitude");
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return null;
        }

        return new GeoPointValue(latitude.Value, longitude.Value);
    }

    internal static string? ReadRemoteImageSourceUrl(JsonElement patch)
    {
        return ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "sourceUrl") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "remoteUrl") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "externalUrl");
    }

    internal static ImageOwnerType ResolveRequestedImageOwnerType(JsonElement patch)
    {
        string? ownerTypeText = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "ownerType");
        if (!string.IsNullOrWhiteSpace(ownerTypeText))
        {
            return ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnumFromText(ownerTypeText, ImageOwnerType.Park);
        }

        string? ownerKey = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "ownerKey");
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

            if (ownerKey.StartsWith("standalone-attraction:", StringComparison.OrdinalIgnoreCase) || ownerKey.StartsWith("standaloneAttraction:", StringComparison.OrdinalIgnoreCase))
            {
                return ImageOwnerType.StandaloneAttraction;
            }
        }

        return ImageOwnerType.Park;
    }

    internal static string? BuildItemNameKey(string? ownerKey)
    {
        return string.IsNullOrWhiteSpace(ownerKey) ? null : $"item:{ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(ownerKey)}";
    }

    internal static string? BuildStandaloneAttractionNameKey(string? ownerKey)
    {
        return string.IsNullOrWhiteSpace(ownerKey) ? null : $"standalone-attraction:{ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(ownerKey)}";
    }

    internal static bool TryResolveOwnerKey(string? ownerKey, Dictionary<string, string> ownerKeys, string? fallbackKey, out string? ownerId)
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

    internal static bool TryResolvePrefixedOwnerKey(string ownerKey, string prefix, Dictionary<string, string> ownerKeys, out string? ownerId)
    {
        ownerId = null;
        if (!ownerKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string key = ownerKey[prefix.Length..].Trim();
        return ParkGraphUpsertProcessorImagesExtensions.TryResolveOwnerKey(key, ownerKeys, null, out ownerId);
    }

    internal static bool HasRemoteMetadataPatch(JsonElement patch)
    {
        return ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "altTexts") || ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "captions") || ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "credits") || ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "tagIds") || ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "geoLocation") || ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "isPublished");
    }

    internal static bool ShouldApplyRemoteImageWatermark(ImageCategory category, bool? requestedWithWatermark)
    {
        return !ParkGraphUpsertProcessorImagesExtensions.IsLogoCategory(category) && requestedWithWatermark == true;
    }

    internal static bool IsLogoCategory(ImageCategory category)
    {
        return category is ImageCategory.Logo;
    }

    internal static string BuildInternalImageUrl(string imageId)
    {
        return $"/images/{imageId}";
    }
}
