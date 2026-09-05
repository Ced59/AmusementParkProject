using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task<(Park Park, IReadOnlyDictionary<string, string> StorageLookupKeys)> ProjectOfficialMapTargetAfterMergesAsync(
        JsonElement root,
        Park selectedPark,
        ParkGraphUpsertResult result,
        CancellationToken cancellationToken)
    {
        Dictionary<string, Park> projectedParks = new Dictionary<string, Park>(StringComparer.Ordinal)
        {
            [selectedPark.Id] = ClonePark(selectedPark),
        };
        Dictionary<string, string> storageLookupKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        string projectedTargetId = selectedPark.Id;
        JsonElement? merges = GetArray(root, "merges") ?? GetArray(root, "mergeResolutions");
        if (merges is null)
        {
            return (projectedParks[projectedTargetId], storageLookupKeys);
        }

        foreach (JsonElement merge in merges.Value.EnumerateArray())
        {
            if (merge.ValueKind != JsonValueKind.Object
                || !string.Equals(
                    NormalizeMergeEntityType(ReadString(merge, "entityType") ?? ReadString(merge, "type")),
                    "Park",
                    StringComparison.Ordinal))
            {
                continue;
            }

            string? sourceId = ReadString(merge, "sourceId") ?? ReadString(merge, "duplicateId") ?? ReadString(merge, "fromId");
            string? targetId = ReadString(merge, "targetId") ?? ReadString(merge, "keepId") ?? ReadString(merge, "toId");
            if (string.IsNullOrWhiteSpace(sourceId)
                || string.IsNullOrWhiteSpace(targetId)
                || string.Equals(sourceId, targetId, StringComparison.Ordinal))
            {
                continue;
            }

            Park? source = projectedParks.TryGetValue(sourceId, out Park? projectedSource)
                ? projectedSource
                : await this.parkRepository.GetByIdAsync(sourceId, true, cancellationToken);
            Park? target = projectedParks.TryGetValue(targetId, out Park? projectedTarget)
                ? projectedTarget
                : await this.parkRepository.GetByIdAsync(targetId, true, cancellationToken);
            if (source is null || target is null)
            {
                result.Errors.Add($"Park merge ignored: source '{sourceId}' or target '{targetId}' was not found.");
                return (projectedParks[projectedTargetId], storageLookupKeys);
            }

            Park merged = ClonePark(target);
            ParkGraphUpsertResult projectionResult = new ParkGraphUpsertResult();
            ParkGraphUpsertChange projectionChange = BuildEntityChange(
                "Park",
                target.Id,
                null,
                target.Name ?? target.Id,
                "Unchanged",
                $"merge:{source.Id}");
            JsonElement? sections = GetObject(merge, "sections");
            ApplyParkMergeSections(source, merged, sections, projectionChange, projectionResult);
            if (projectionResult.Errors.Count > 0)
            {
                result.Errors.AddRange(projectionResult.Errors);
                return (projectedParks[projectedTargetId], storageLookupKeys);
            }

            if (ShouldTakeSourceSection(sections, "officialMaps"))
            {
                foreach (ParkOfficialMap sourceMap in source.OfficialMaps)
                {
                    if (string.IsNullOrWhiteSpace(sourceMap.StorageKey))
                    {
                        continue;
                    }

                    ParkOfficialMap? mergedMap = merged.OfficialMaps.FirstOrDefault(officialMap =>
                        string.Equals(officialMap.Id, sourceMap.Id, StringComparison.OrdinalIgnoreCase));
                    if (mergedMap is null || string.IsNullOrWhiteSpace(mergedMap.StorageKey))
                    {
                        continue;
                    }

                    storageLookupKeys[mergedMap.StorageKey] = storageLookupKeys.TryGetValue(sourceMap.StorageKey, out string? originalStorageKey)
                        ? originalStorageKey
                        : sourceMap.StorageKey;
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

    private async Task<Park?> RefreshTargetParkAfterAppliedMergesAsync(
        Park targetPark,
        ParkGraphUpsertMergeSummary mergeSummary,
        bool apply,
        CancellationToken cancellationToken)
    {
        if (!apply)
        {
            return targetPark;
        }

        string refreshedParkId = mergeSummary.ParkIdRemaps.TryGetValue(targetPark.Id, out string? remappedParkId)
            ? remappedParkId
            : targetPark.Id;
        if (string.Equals(refreshedParkId, targetPark.Id, StringComparison.Ordinal)
            && !mergeSummary.ChangedParkIds.Contains(targetPark.Id))
        {
            return targetPark;
        }

        return await this.parkRepository.GetByIdAsync(refreshedParkId, true, cancellationToken);
    }
}
