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
internal static class ParkGraphUpsertProcessorTargetParkExtensions
{
    internal static async Task<(Park Park, IReadOnlyDictionary<string, string> StorageLookupKeys)> ProjectOfficialMapTargetAfterMergesAsync(this ParkGraphUpsertProcessor processorContext, JsonElement root, Park selectedPark, ParkGraphUpsertResult result, CancellationToken cancellationToken)
    {
        Dictionary<string, Park> projectedParks = new Dictionary<string, Park>(StringComparer.Ordinal)
        {
            [selectedPark.Id] = ParkGraphUpsertProcessorMergeHelpersExtensions.ClonePark(selectedPark),
        };
        Dictionary<string, string> storageLookupKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        string projectedTargetId = selectedPark.Id;
        JsonElement? merges = ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(root, "merges") ?? ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(root, "mergeResolutions");
        if (merges is null)
        {
            return (projectedParks[projectedTargetId], storageLookupKeys);
        }

        foreach (JsonElement merge in merges.Value.EnumerateArray())
        {
            if (merge.ValueKind != JsonValueKind.Object || !string.Equals(ParkGraphUpsertProcessorMergeHelpersExtensions.NormalizeMergeEntityType(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "entityType") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "type")), "Park", StringComparison.Ordinal))
            {
                continue;
            }

            string? sourceId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "sourceId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "duplicateId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "fromId");
            string? targetId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "targetId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "keepId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(merge, "toId");
            if (string.IsNullOrWhiteSpace(sourceId) || string.IsNullOrWhiteSpace(targetId) || string.Equals(sourceId, targetId, StringComparison.Ordinal))
            {
                continue;
            }

            Park? source = projectedParks.TryGetValue(sourceId, out Park? projectedSource) ? projectedSource : await processorContext.parkRepository.GetByIdAsync(sourceId, true, cancellationToken);
            Park? target = projectedParks.TryGetValue(targetId, out Park? projectedTarget) ? projectedTarget : await processorContext.parkRepository.GetByIdAsync(targetId, true, cancellationToken);
            if (source is null || target is null)
            {
                result.Errors.Add($"Park merge ignored: source '{sourceId}' or target '{targetId}' was not found.");
                return (projectedParks[projectedTargetId], storageLookupKeys);
            }

            Park merged = ParkGraphUpsertProcessorMergeHelpersExtensions.ClonePark(target);
            ParkGraphUpsertResult projectionResult = new ParkGraphUpsertResult();
            ParkGraphUpsertChange projectionChange = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("Park", target.Id, null, target.Name ?? target.Id, "Unchanged", $"merge:{source.Id}");
            JsonElement? sections = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(merge, "sections");
            ParkGraphUpsertProcessorMergeSectionsExtensions.ApplyParkMergeSections(source, merged, sections, projectionChange, projectionResult);
            if (projectionResult.Errors.Count > 0)
            {
                result.Errors.AddRange(projectionResult.Errors);
                return (projectedParks[projectedTargetId], storageLookupKeys);
            }

            if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "officialMaps"))
            {
                foreach (ParkOfficialMap sourceMap in source.OfficialMaps)
                {
                    if (string.IsNullOrWhiteSpace(sourceMap.StorageKey))
                    {
                        continue;
                    }

                    ParkOfficialMap? mergedMap = merged.OfficialMaps.FirstOrDefault(officialMap => string.Equals(officialMap.Id, sourceMap.Id, StringComparison.OrdinalIgnoreCase));
                    if (mergedMap is null || string.IsNullOrWhiteSpace(mergedMap.StorageKey))
                    {
                        continue;
                    }

                    storageLookupKeys[mergedMap.StorageKey] = storageLookupKeys.TryGetValue(sourceMap.StorageKey, out string? originalStorageKey) ? originalStorageKey : sourceMap.StorageKey;
                }
            }

            projectedParks.Remove(sourceId);
            projectedParks[targetId] = merged;
            if (string.Equals(projectedTargetId, sourceId, StringComparison.Ordinal))
            {
                projectedTargetId = targetId;
            }
        }

        return (projectedParks[projectedTargetId], storageLookupKeys);
    }

    internal static async Task<Park?> RefreshTargetParkAfterAppliedMergesAsync(this ParkGraphUpsertProcessor processorContext, Park targetPark, ParkGraphUpsertMergeSummary mergeSummary, bool apply, CancellationToken cancellationToken)
    {
        if (!apply)
        {
            return targetPark;
        }

        string refreshedParkId = mergeSummary.ParkIdRemaps.TryGetValue(targetPark.Id, out string? remappedParkId) ? remappedParkId : targetPark.Id;
        if (string.Equals(refreshedParkId, targetPark.Id, StringComparison.Ordinal) && !mergeSummary.ChangedParkIds.Contains(targetPark.Id))
        {
            return targetPark;
        }

        return await processorContext.parkRepository.GetByIdAsync(refreshedParkId, true, cancellationToken);
    }
}
