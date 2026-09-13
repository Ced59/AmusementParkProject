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
internal static class ParkGraphUpsertProcessorMergesExtensions
{
    internal static async Task<ParkGraphUpsertMergeSummary> ProcessMergesAsync(this ParkGraphUpsertProcessor processorContext, JsonElement root, Dictionary<string, string> manufacturerKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        ParkGraphUpsertMergeSummary summary = new ParkGraphUpsertMergeSummary();
        JsonElement? merges = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(root, "merges") ?? ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(root, "mergeResolutions");
        if (merges is null)
        {
            return summary;
        }

        foreach (JsonElement merge in merges.Value.EnumerateArray())
        {
            if (merge.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string entityType = ParkGraphUpsertProcessorMergeHelpersExtensions.NormalizeMergeEntityType(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "entityType") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "type"));
            string? sourceId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "sourceId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "duplicateId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "fromId");
            string? targetId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "targetId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "keepId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "toId");
            JsonElement? sections = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(merge, "sections");
            if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(targetId))
            {
                result.Errors.Add("Merge ignored: entityType, sourceId and targetId are required.");
                continue;
            }

            if (string.Equals(sourceId, targetId, StringComparison.Ordinal))
            {
                result.Errors.Add($"Merge ignored for {entityType}: sourceId and targetId must be different.");
                continue;
            }

            if (string.Equals(entityType, "AttractionManufacturer", StringComparison.Ordinal))
            {
                await processorContext.MergeManufacturerAsync(sourceId, targetId, sections, manufacturerKeys, summary, result, apply, cancellationToken);
            }
            else if (string.Equals(entityType, "Park", StringComparison.Ordinal))
            {
                await processorContext.MergeParkAsync(sourceId, targetId, sections, summary, result, apply, cancellationToken);
            }
            else if (string.Equals(entityType, "ParkItem", StringComparison.Ordinal))
            {
                await processorContext.MergeParkItemAsync(sourceId, targetId, sections, summary, result, apply, cancellationToken);
            }
            else
            {
                result.Errors.Add($"Merge ignored: entityType '{entityType}' is not supported.");
            }
        }

        ParkGraphUpsertProcessorMergeHelpersExtensions.ApplyManufacturerIdRemaps(manufacturerKeys, summary.ManufacturerIdRemaps);
        return summary;
    }

    internal static async Task MergeManufacturerAsync(this ParkGraphUpsertProcessor processorContext, string sourceId, string targetId, JsonElement? sections, Dictionary<string, string> manufacturerKeys, ParkGraphUpsertMergeSummary summary, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        AttractionManufacturer? source = await processorContext.attractionManufacturerRepository.GetByIdAsync(sourceId, cancellationToken);
        AttractionManufacturer? target = await processorContext.attractionManufacturerRepository.GetByIdAsync(targetId, cancellationToken);
        if (source is null || target is null)
        {
            result.Errors.Add($"Manufacturer merge ignored: source '{sourceId}' or target '{targetId}' was not found.");
            return;
        }

        AttractionManufacturer merged = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneManufacturer(target);
        ParkGraphUpsertChange targetChange = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("AttractionManufacturer", target.Id, null, target.Name, "Unchanged", $"merge:{source.Id}");
        ParkGraphUpsertProcessorMergeSectionsExtensions.ApplyManufacturerMergeSections(source, merged, sections, targetChange);
        IReadOnlyCollection<ParkItem> sourceItems = await processorContext.parkItemRepository.GetByManufacturerIdAsync(source.Id, true, cancellationToken);
        IReadOnlyCollection<Image> sourceImages = await processorContext.imageRepository.GetByOwnerAsync(ImageOwnerType.AttractionManufacturer, source.Id, null, cancellationToken);
        string? copiedSourceLogoImageId = ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "logo") ? ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(source.CurrentLogoImageId) : null;
        ParkGraphUpsertProcessorMergeHelpersExtensions.AddAttachmentCountChange(targetChange, "attachments.parkItemsMoved", sourceItems.Count);
        ParkGraphUpsertProcessorMergeHelpersExtensions.AddAttachmentCountChange(targetChange, "attachments.imagesMoved", sourceImages.Count);
        if (targetChange.Fields.Count > 0)
        {
            targetChange.ChangeType = "Updated";
        }

        ParkGraphUpsertChange sourceChange = ParkGraphUpsertProcessorMergeHelpersExtensions.BuildDeletedMergeSourceChange("AttractionManufacturer", source.Id, source.Name, target.Id);
        if (apply)
        {
            foreach (ParkItem item in sourceItems)
            {
                PublicSeoParkItemSnapshot? previousSnapshot = PublicSeoParkItemSnapshot.FromParkItem(item);
                item.AttractionDetails ??= new AttractionDetails();
                item.AttractionDetails.ManufacturerId = merged.Id;
                ParkItem? updatedItem = await processorContext.parkItemRepository.UpdateAsync(item.Id, item, cancellationToken);
                ParkItem currentItem = updatedItem ?? item;
                summary.ChangedParkItemIds.Add(currentItem.Id);
                if (!string.IsNullOrWhiteSpace(currentItem.ParkId))
                {
                    summary.ChangedParkIds.Add(currentItem.ParkId);
                }

                if (previousSnapshot is not null)
                {
                    summary.PreviousParkItems.Add(previousSnapshot);
                }

                PublicSeoParkItemSnapshot? currentSnapshot = PublicSeoParkItemSnapshot.FromParkItem(currentItem);
                if (currentSnapshot is not null)
                {
                    summary.CurrentParkItems.Add(currentSnapshot);
                }
            }

            foreach (Image image in sourceImages)
            {
                if (ParkGraphUpsertProcessorMergesExtensions.IsCopiedSourceLogoImage(image, copiedSourceLogoImageId))
                {
                    await processorContext.imageRepository.SetCurrentAsync(image.Id, ImageOwnerType.AttractionManufacturer, merged.Id, cancellationToken);
                    continue;
                }

                await processorContext.imageRepository.LinkAsync(image.Id, ImageOwnerType.AttractionManufacturer, merged.Id, cancellationToken);
            }

            bool sourceDeleted = await processorContext.attractionManufacturerRepository.DeleteAsync(source.Id, cancellationToken);
            if (!sourceDeleted)
            {
                result.Errors.Add($"Manufacturer merge failed: source '{source.Id}' could not be deleted before updating target '{target.Id}'.");
                return;
            }

            if (targetChange.Fields.Count > 0)
            {
                AttractionManufacturer? updated = await processorContext.attractionManufacturerRepository.UpdateAsync(merged.Id, merged, cancellationToken);
                merged = updated ?? merged;
            }

            await processorContext.searchProjectionWriter.DeleteAsync(SearchProjectionResourceTypes.Manufacturers, source.Id, cancellationToken);
            await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, merged.Id, cancellationToken);
            if (sourceItems.Count > 0)
            {
                await processorContext.searchProjectionWriter.UpsertManyAsync(SearchProjectionResourceTypes.ParkItems, sourceItems.Select(static item => item.Id).ToList(), cancellationToken);
            }
        }

        summary.ManufacturerIdRemaps[source.Id] = target.Id;
        ParkGraphUpsertProcessorMergeHelpersExtensions.AddManufacturerKeyRemaps(manufacturerKeys, source.Id, target.Id);
        result.Changes.Add(targetChange);
        result.Changes.Add(sourceChange);
    }

    internal static bool IsCopiedSourceLogoImage(Image image, string? copiedSourceLogoImageId)
    {
        return !string.IsNullOrWhiteSpace(copiedSourceLogoImageId) && string.Equals(image.Id, copiedSourceLogoImageId, StringComparison.Ordinal) && image.Category == ImageCategory.Logo;
    }

    internal static async Task MergeParkAsync(this ParkGraphUpsertProcessor processorContext, string sourceId, string targetId, JsonElement? sections, ParkGraphUpsertMergeSummary summary, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        Park? source = await processorContext.parkRepository.GetByIdAsync(sourceId, true, cancellationToken);
        Park? target = await processorContext.parkRepository.GetByIdAsync(targetId, true, cancellationToken);
        if (source is null || target is null)
        {
            result.Errors.Add($"Park merge ignored: source '{sourceId}' or target '{targetId}' was not found.");
            return;
        }

        PublicSeoParkSnapshot? previousSourcePark = PublicSeoParkSnapshot.FromPark(source);
        PublicSeoParkSnapshot? previousTargetPark = PublicSeoParkSnapshot.FromPark(target);
        Park merged = ParkGraphUpsertProcessorMergeHelpersExtensions.ClonePark(target);
        ParkGraphUpsertChange targetChange = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("Park", target.Id, null, target.Name ?? target.Id, "Unchanged", $"merge:{source.Id}");
        int errorCountBeforeSections = result.Errors.Count;
        ParkGraphUpsertProcessorMergeSectionsExtensions.ApplyParkMergeSections(source, merged, sections, targetChange, result);
        if (result.Errors.Count > errorCountBeforeSections)
        {
            return;
        }

        IReadOnlyCollection<ParkZone> sourceZones = await processorContext.parkZoneRepository.GetByParkIdAsync(source.Id, cancellationToken);
        IReadOnlyCollection<ParkItem> sourceItems = await processorContext.parkItemRepository.GetByParkIdAsync(source.Id, true, cancellationToken);
        IReadOnlyCollection<Image> sourceImages = await processorContext.imageRepository.GetByOwnerAsync(ImageOwnerType.Park, source.Id, null, cancellationToken);
        ParkPricingEntity? sourcePricing = processorContext.parkPricingRepository is null ? null : await processorContext.parkPricingRepository.GetByParkIdAsync(source.Id, cancellationToken);
        ParkPricingEntity? targetPricing = sourcePricing is null || processorContext.parkPricingRepository is null ? null : await processorContext.parkPricingRepository.GetByParkIdAsync(target.Id, cancellationToken);
        bool shouldMoveSourcePricing = sourcePricing is not null && merged.Status.IsOpenToVisitors() && (targetPricing is null || ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "pricing"));
        ParkGraphUpsertProcessorMergeHelpersExtensions.AddAttachmentCountChange(targetChange, "attachments.zonesMoved", sourceZones.Count);
        ParkGraphUpsertProcessorMergeHelpersExtensions.AddAttachmentCountChange(targetChange, "attachments.parkItemsMoved", sourceItems.Count);
        ParkGraphUpsertProcessorMergeHelpersExtensions.AddAttachmentCountChange(targetChange, "attachments.imagesMoved", sourceImages.Count);
        int officialMapFileCount = ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "officialMaps") ? source.OfficialMaps.Count(static officialMap => !string.IsNullOrWhiteSpace(officialMap.StorageKey)) : 0;
        ParkGraphUpsertProcessorMergeHelpersExtensions.AddAttachmentCountChange(targetChange, "attachments.officialMapFilesCopied", officialMapFileCount);
        if (shouldMoveSourcePricing && sourcePricing is not null)
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(targetChange, "attachments.pricingMoved", targetPricing?.ParkId, sourcePricing.ParkId);
        }
        else if (sourcePricing is not null && !merged.Status.IsOpenToVisitors())
        {
            result.Warnings.Add($"Park merge removed source pricing '{source.Id}' because target park '{target.Id}' is not open to visitors.");
        }
        else if (sourcePricing is not null && targetPricing is not null)
        {
            result.Warnings.Add($"Park merge kept target pricing '{target.Id}' and removed source pricing '{source.Id}'. Set sections.pricing to 'source' to replace it.");
        }

        if (targetChange.Fields.Count > 0)
        {
            targetChange.ChangeType = "Updated";
        }

        ParkGraphUpsertChange sourceChange = ParkGraphUpsertProcessorMergeHelpersExtensions.BuildDeletedMergeSourceChange("Park", source.Id, source.Name ?? source.Id, target.Id);
        if (apply)
        {
            bool officialMapFilesCopied = await processorContext.CopyOfficialMapFilesForMergeAsync(source, merged, sections, result, cancellationToken);
            if (!officialMapFilesCopied)
            {
                return;
            }

            Park? updatedPark = targetChange.Fields.Count > 0 ? await processorContext.parkRepository.UpdateAsync(merged.Id, merged, cancellationToken) : merged;
            merged = updatedPark ?? merged;
            foreach (ParkZone zone in sourceZones)
            {
                zone.ParkId = merged.Id;
                await processorContext.parkZoneRepository.UpdateAsync(zone.Id, zone, cancellationToken);
            }

            foreach (ParkItem item in sourceItems)
            {
                PublicSeoParkItemSnapshot? previousSnapshot = PublicSeoParkItemSnapshot.FromParkItem(item);
                item.ParkId = merged.Id;
                ParkItem? updatedItem = await processorContext.parkItemRepository.UpdateAsync(item.Id, item, cancellationToken);
                ParkItem currentItem = updatedItem ?? item;
                summary.ChangedParkItemIds.Add(currentItem.Id);
                if (previousSnapshot is not null)
                {
                    summary.PreviousParkItems.Add(previousSnapshot);
                }

                PublicSeoParkItemSnapshot? currentSnapshot = PublicSeoParkItemSnapshot.FromParkItem(currentItem);
                if (currentSnapshot is not null)
                {
                    summary.CurrentParkItems.Add(currentSnapshot);
                }
            }

            foreach (Image image in sourceImages)
            {
                await processorContext.imageRepository.LinkAsync(image.Id, ImageOwnerType.Park, merged.Id, cancellationToken);
            }

            if (sourcePricing is not null && processorContext.parkPricingRepository is not null)
            {
                if (shouldMoveSourcePricing)
                {
                    sourcePricing.ParkId = merged.Id;
                    await processorContext.parkPricingRepository.UpsertAsync(sourcePricing, cancellationToken);
                }

                await processorContext.parkPricingRepository.DeleteByParkIdAsync(source.Id, cancellationToken);
            }

            await processorContext.parkRepository.DeleteAsync(source.Id, cancellationToken);
            await processorContext.searchProjectionWriter.DeleteAsync(SearchProjectionResourceTypes.Parks, source.Id, cancellationToken);
            await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, merged.Id, cancellationToken);
            if (sourceItems.Count > 0)
            {
                await processorContext.searchProjectionWriter.UpsertManyAsync(SearchProjectionResourceTypes.ParkItems, sourceItems.Select(static item => item.Id).ToList(), cancellationToken);
            }
        }

        if (previousSourcePark is not null)
        {
            summary.PreviousParks.Add(previousSourcePark);
        }

        if (previousTargetPark is not null)
        {
            summary.PreviousParks.Add(previousTargetPark);
        }

        PublicSeoParkSnapshot? currentPark = PublicSeoParkSnapshot.FromPark(merged);
        if (currentPark is not null)
        {
            summary.CurrentParks.Add(currentPark);
        }

        summary.ChangedParkIds.Add(merged.Id);
        summary.ChangedParkIds.Add(source.Id);
        summary.ParkIdRemaps[source.Id] = merged.Id;
        result.Changes.Add(targetChange);
        result.Changes.Add(sourceChange);
    }

    internal static async Task<bool> CopyOfficialMapFilesForMergeAsync(this ParkGraphUpsertProcessor processorContext, Park source, Park merged, JsonElement? sections, ParkGraphUpsertResult result, CancellationToken cancellationToken)
    {
        if (!ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "officialMaps"))
        {
            return true;
        }

        IReadOnlyCollection<ParkOfficialMap> storedMaps = source.OfficialMaps.Where(static officialMap => !string.IsNullOrWhiteSpace(officialMap.StorageKey)).ToList();
        if (storedMaps.Count == 0)
        {
            return true;
        }

        if (processorContext.parkOfficialMapBinaryStorage is null)
        {
            result.Errors.Add("La fusion des cartes officielles stockées est indisponible car le stockage de fichiers n'est pas configuré.");
            return false;
        }

        foreach (ParkOfficialMap sourceMap in storedMaps)
        {
            ParkOfficialMap? targetMap = merged.OfficialMaps.FirstOrDefault(officialMap => string.Equals(officialMap.Id, sourceMap.Id, StringComparison.OrdinalIgnoreCase));
            if (targetMap is null || string.IsNullOrWhiteSpace(targetMap.StorageKey))
            {
                result.Errors.Add($"La clé cible de la carte officielle '{sourceMap.Id}' n'a pas pu être préparée pour la fusion.");
                return false;
            }

            bool copied = await processorContext.parkOfficialMapBinaryStorage.CopyAsync(sourceMap.StorageKey!, targetMap.StorageKey, cancellationToken);
            if (!copied)
            {
                result.Errors.Add($"Le fichier de la carte officielle '{sourceMap.Id}' est introuvable dans le stockage et la fusion a été annulée.");
                return false;
            }
        }

        return true;
    }

    internal static async Task MergeParkItemAsync(this ParkGraphUpsertProcessor processorContext, string sourceId, string targetId, JsonElement? sections, ParkGraphUpsertMergeSummary summary, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        ParkItem? source = await processorContext.parkItemRepository.GetByIdAsync(sourceId, true, cancellationToken);
        ParkItem? target = await processorContext.parkItemRepository.GetByIdAsync(targetId, true, cancellationToken);
        if (source is null || target is null)
        {
            result.Errors.Add($"ParkItem merge ignored: source '{sourceId}' or target '{targetId}' was not found.");
            return;
        }

        PublicSeoParkItemSnapshot? previousSourceItem = PublicSeoParkItemSnapshot.FromParkItem(source);
        PublicSeoParkItemSnapshot? previousTargetItem = PublicSeoParkItemSnapshot.FromParkItem(target);
        ParkItem merged = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneParkItem(target);
        ParkGraphUpsertChange targetChange = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("ParkItem", target.Id, null, target.Name, "Unchanged", $"merge:{source.Id}");
        ParkGraphUpsertProcessorMergeSectionsExtensions.ApplyParkItemMergeSections(source, merged, sections, targetChange);
        IReadOnlyCollection<Image> sourceImages = await processorContext.imageRepository.GetByOwnerAsync(ImageOwnerType.ParkItem, source.Id, null, cancellationToken);
        ParkGraphUpsertProcessorMergeHelpersExtensions.AddAttachmentCountChange(targetChange, "attachments.imagesMoved", sourceImages.Count);
        if (targetChange.Fields.Count > 0)
        {
            targetChange.ChangeType = "Updated";
        }

        ParkGraphUpsertChange sourceChange = ParkGraphUpsertProcessorMergeHelpersExtensions.BuildDeletedMergeSourceChange("ParkItem", source.Id, source.Name, target.Id);
        if (apply)
        {
            ParkItem? updatedItem = targetChange.Fields.Count > 0 ? await processorContext.parkItemRepository.UpdateAsync(merged.Id, merged, cancellationToken) : merged;
            merged = updatedItem ?? merged;
            foreach (Image image in sourceImages)
            {
                await processorContext.imageRepository.LinkAsync(image.Id, ImageOwnerType.ParkItem, merged.Id, cancellationToken);
            }

            await processorContext.parkItemRepository.DeleteAsync(source.Id, cancellationToken);
            await processorContext.searchProjectionWriter.DeleteAsync(SearchProjectionResourceTypes.ParkItems, source.Id, cancellationToken);
            await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, merged.Id, cancellationToken);
            if (!string.IsNullOrWhiteSpace(source.ParkId))
            {
                await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, source.ParkId, cancellationToken);
                summary.ChangedParkIds.Add(source.ParkId);
            }

            if (!string.IsNullOrWhiteSpace(merged.ParkId))
            {
                await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, merged.ParkId, cancellationToken);
                summary.ChangedParkIds.Add(merged.ParkId);
            }
        }

        if (previousSourceItem is not null)
        {
            summary.PreviousParkItems.Add(previousSourceItem);
        }

        if (previousTargetItem is not null)
        {
            summary.PreviousParkItems.Add(previousTargetItem);
        }

        PublicSeoParkItemSnapshot? currentItem = PublicSeoParkItemSnapshot.FromParkItem(merged);
        if (currentItem is not null)
        {
            summary.CurrentParkItems.Add(currentItem);
        }

        summary.ChangedParkItemIds.Add(merged.Id);
        result.Changes.Add(targetChange);
        result.Changes.Add(sourceChange);
    }
}
