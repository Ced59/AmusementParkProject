using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task<ParkGraphUpsertMergeSummary> ProcessMergesAsync(
        JsonElement root,
        Dictionary<string, string> manufacturerKeys,
        ParkGraphUpsertResult result,
        bool apply,
        CancellationToken cancellationToken)
    {
        ParkGraphUpsertMergeSummary summary = new ParkGraphUpsertMergeSummary();
        JsonElement? merges = GetArray(root, "merges") ?? GetArray(root, "mergeResolutions");
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

            string entityType = NormalizeMergeEntityType(ReadString(merge, "entityType") ?? ReadString(merge, "type"));
            string? sourceId = ReadString(merge, "sourceId") ?? ReadString(merge, "duplicateId") ?? ReadString(merge, "fromId");
            string? targetId = ReadString(merge, "targetId") ?? ReadString(merge, "keepId") ?? ReadString(merge, "toId");
            JsonElement? sections = GetObject(merge, "sections");

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
                await this.MergeManufacturerAsync(sourceId, targetId, sections, manufacturerKeys, summary, result, apply, cancellationToken);
            }
            else if (string.Equals(entityType, "Park", StringComparison.Ordinal))
            {
                await this.MergeParkAsync(sourceId, targetId, sections, summary, result, apply, cancellationToken);
            }
            else if (string.Equals(entityType, "ParkItem", StringComparison.Ordinal))
            {
                await this.MergeParkItemAsync(sourceId, targetId, sections, summary, result, apply, cancellationToken);
            }
            else
            {
                result.Errors.Add($"Merge ignored: entityType '{entityType}' is not supported.");
            }
        }

        ApplyManufacturerIdRemaps(manufacturerKeys, summary.ManufacturerIdRemaps);
        return summary;
    }

    private async Task MergeManufacturerAsync(
        string sourceId,
        string targetId,
        JsonElement? sections,
        Dictionary<string, string> manufacturerKeys,
        ParkGraphUpsertMergeSummary summary,
        ParkGraphUpsertResult result,
        bool apply,
        CancellationToken cancellationToken)
    {
        AttractionManufacturer? source = await this.attractionManufacturerRepository.GetByIdAsync(sourceId, cancellationToken);
        AttractionManufacturer? target = await this.attractionManufacturerRepository.GetByIdAsync(targetId, cancellationToken);
        if (source is null || target is null)
        {
            result.Errors.Add($"Manufacturer merge ignored: source '{sourceId}' or target '{targetId}' was not found.");
            return;
        }

        AttractionManufacturer merged = CloneManufacturer(target);
        ParkGraphUpsertChange targetChange = BuildEntityChange(
            "AttractionManufacturer",
            target.Id,
            null,
            target.Name,
            "Unchanged",
            $"merge:{source.Id}");

        ApplyManufacturerMergeSections(source, merged, sections, targetChange);

        IReadOnlyCollection<ParkItem> sourceItems = await this.parkItemRepository.GetByManufacturerIdAsync(source.Id, true, cancellationToken);
        IReadOnlyCollection<Image> sourceImages = await this.imageRepository.GetByOwnerAsync(ImageOwnerType.AttractionManufacturer, source.Id, null, cancellationToken);
        AddAttachmentCountChange(targetChange, "attachments.parkItemsMoved", sourceItems.Count);
        AddAttachmentCountChange(targetChange, "attachments.imagesMoved", sourceImages.Count);

        if (targetChange.Fields.Count > 0)
        {
            targetChange.ChangeType = "Updated";
        }

        ParkGraphUpsertChange sourceChange = BuildDeletedMergeSourceChange("AttractionManufacturer", source.Id, source.Name, target.Id);
        if (apply)
        {
            if (targetChange.Fields.Count > 0)
            {
                AttractionManufacturer? updated = await this.attractionManufacturerRepository.UpdateAsync(merged.Id, merged, cancellationToken);
                merged = updated ?? merged;
            }

            foreach (ParkItem item in sourceItems)
            {
                PublicSeoParkItemSnapshot? previousSnapshot = PublicSeoParkItemSnapshot.FromParkItem(item);
                item.AttractionDetails ??= new AttractionDetails();
                item.AttractionDetails.ManufacturerId = merged.Id;
                ParkItem? updatedItem = await this.parkItemRepository.UpdateAsync(item.Id, item, cancellationToken);
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
                await this.imageRepository.LinkAsync(image.Id, ImageOwnerType.AttractionManufacturer, merged.Id, cancellationToken);
            }

            await this.attractionManufacturerRepository.DeleteAsync(source.Id, cancellationToken);
            await this.searchProjectionWriter.DeleteAsync(SearchProjectionResourceTypes.Manufacturers, source.Id, cancellationToken);
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, merged.Id, cancellationToken);
            if (sourceItems.Count > 0)
            {
                await this.searchProjectionWriter.UpsertManyAsync(SearchProjectionResourceTypes.ParkItems, sourceItems.Select(static item => item.Id).ToList(), cancellationToken);
            }
        }

        summary.ManufacturerIdRemaps[source.Id] = target.Id;
        AddManufacturerKeyRemaps(manufacturerKeys, source.Id, target.Id);
        result.Changes.Add(targetChange);
        result.Changes.Add(sourceChange);
    }

    private async Task MergeParkAsync(
        string sourceId,
        string targetId,
        JsonElement? sections,
        ParkGraphUpsertMergeSummary summary,
        ParkGraphUpsertResult result,
        bool apply,
        CancellationToken cancellationToken)
    {
        Park? source = await this.parkRepository.GetByIdAsync(sourceId, true, cancellationToken);
        Park? target = await this.parkRepository.GetByIdAsync(targetId, true, cancellationToken);
        if (source is null || target is null)
        {
            result.Errors.Add($"Park merge ignored: source '{sourceId}' or target '{targetId}' was not found.");
            return;
        }

        PublicSeoParkSnapshot? previousSourcePark = PublicSeoParkSnapshot.FromPark(source);
        PublicSeoParkSnapshot? previousTargetPark = PublicSeoParkSnapshot.FromPark(target);
        Park merged = ClonePark(target);
        ParkGraphUpsertChange targetChange = BuildEntityChange("Park", target.Id, null, target.Name ?? target.Id, "Unchanged", $"merge:{source.Id}");
        ApplyParkMergeSections(source, merged, sections, targetChange);

        IReadOnlyCollection<ParkZone> sourceZones = await this.parkZoneRepository.GetByParkIdAsync(source.Id, cancellationToken);
        IReadOnlyCollection<ParkItem> sourceItems = await this.parkItemRepository.GetByParkIdAsync(source.Id, true, cancellationToken);
        IReadOnlyCollection<Image> sourceImages = await this.imageRepository.GetByOwnerAsync(ImageOwnerType.Park, source.Id, null, cancellationToken);
        AddAttachmentCountChange(targetChange, "attachments.zonesMoved", sourceZones.Count);
        AddAttachmentCountChange(targetChange, "attachments.parkItemsMoved", sourceItems.Count);
        AddAttachmentCountChange(targetChange, "attachments.imagesMoved", sourceImages.Count);

        if (targetChange.Fields.Count > 0)
        {
            targetChange.ChangeType = "Updated";
        }

        ParkGraphUpsertChange sourceChange = BuildDeletedMergeSourceChange("Park", source.Id, source.Name ?? source.Id, target.Id);
        if (apply)
        {
            Park? updatedPark = targetChange.Fields.Count > 0
                ? await this.parkRepository.UpdateAsync(merged.Id, merged, cancellationToken)
                : merged;
            merged = updatedPark ?? merged;

            foreach (ParkZone zone in sourceZones)
            {
                zone.ParkId = merged.Id;
                await this.parkZoneRepository.UpdateAsync(zone.Id, zone, cancellationToken);
            }

            foreach (ParkItem item in sourceItems)
            {
                PublicSeoParkItemSnapshot? previousSnapshot = PublicSeoParkItemSnapshot.FromParkItem(item);
                item.ParkId = merged.Id;
                ParkItem? updatedItem = await this.parkItemRepository.UpdateAsync(item.Id, item, cancellationToken);
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
                await this.imageRepository.LinkAsync(image.Id, ImageOwnerType.Park, merged.Id, cancellationToken);
            }

            await this.parkRepository.DeleteAsync(source.Id, cancellationToken);
            await this.searchProjectionWriter.DeleteAsync(SearchProjectionResourceTypes.Parks, source.Id, cancellationToken);
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, merged.Id, cancellationToken);
            if (sourceItems.Count > 0)
            {
                await this.searchProjectionWriter.UpsertManyAsync(SearchProjectionResourceTypes.ParkItems, sourceItems.Select(static item => item.Id).ToList(), cancellationToken);
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
        result.Changes.Add(targetChange);
        result.Changes.Add(sourceChange);
    }

    private async Task MergeParkItemAsync(
        string sourceId,
        string targetId,
        JsonElement? sections,
        ParkGraphUpsertMergeSummary summary,
        ParkGraphUpsertResult result,
        bool apply,
        CancellationToken cancellationToken)
    {
        ParkItem? source = await this.parkItemRepository.GetByIdAsync(sourceId, true, cancellationToken);
        ParkItem? target = await this.parkItemRepository.GetByIdAsync(targetId, true, cancellationToken);
        if (source is null || target is null)
        {
            result.Errors.Add($"ParkItem merge ignored: source '{sourceId}' or target '{targetId}' was not found.");
            return;
        }

        PublicSeoParkItemSnapshot? previousSourceItem = PublicSeoParkItemSnapshot.FromParkItem(source);
        PublicSeoParkItemSnapshot? previousTargetItem = PublicSeoParkItemSnapshot.FromParkItem(target);
        ParkItem merged = CloneParkItem(target);
        ParkGraphUpsertChange targetChange = BuildEntityChange("ParkItem", target.Id, null, target.Name, "Unchanged", $"merge:{source.Id}");
        ApplyParkItemMergeSections(source, merged, sections, targetChange);

        IReadOnlyCollection<Image> sourceImages = await this.imageRepository.GetByOwnerAsync(ImageOwnerType.ParkItem, source.Id, null, cancellationToken);
        AddAttachmentCountChange(targetChange, "attachments.imagesMoved", sourceImages.Count);
        if (targetChange.Fields.Count > 0)
        {
            targetChange.ChangeType = "Updated";
        }

        ParkGraphUpsertChange sourceChange = BuildDeletedMergeSourceChange("ParkItem", source.Id, source.Name, target.Id);
        if (apply)
        {
            ParkItem? updatedItem = targetChange.Fields.Count > 0
                ? await this.parkItemRepository.UpdateAsync(merged.Id, merged, cancellationToken)
                : merged;
            merged = updatedItem ?? merged;

            foreach (Image image in sourceImages)
            {
                await this.imageRepository.LinkAsync(image.Id, ImageOwnerType.ParkItem, merged.Id, cancellationToken);
            }

            await this.parkItemRepository.DeleteAsync(source.Id, cancellationToken);
            await this.searchProjectionWriter.DeleteAsync(SearchProjectionResourceTypes.ParkItems, source.Id, cancellationToken);
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, merged.Id, cancellationToken);
            if (!string.IsNullOrWhiteSpace(source.ParkId))
            {
                await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, source.ParkId, cancellationToken);
                summary.ChangedParkIds.Add(source.ParkId);
            }

            if (!string.IsNullOrWhiteSpace(merged.ParkId))
            {
                await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, merged.ParkId, cancellationToken);
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
