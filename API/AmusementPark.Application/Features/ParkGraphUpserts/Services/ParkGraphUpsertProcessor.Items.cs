using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task<ParkGraphUpsertItemSeoChanges> ProcessItemsAsync(JsonElement root, Park park, Dictionary<string, string> zoneKeys, Dictionary<string, string> manufacturerKeys, Dictionary<string, string> itemKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        ParkGraphUpsertItemSeoChanges seoChanges = new ParkGraphUpsertItemSeoChanges();
        if (!root.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array)
        {
            return seoChanges;
        }

        IReadOnlyCollection<ParkItem> existingItems = await this.parkItemRepository.GetByParkIdAsync(park.Id, true, cancellationToken);
        List<ParkItem> mutableItems = existingItems.ToList();
        foreach (JsonElement patch in items.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ReadString(patch, "key");
            string? id = ReadString(patch, "id");
            string? name = ReadString(GetObject(patch, "identity"), "name") ?? ReadString(patch, "name");
            string? externalSource = ReadString(GetObject(patch, "identity"), "externalSource") ?? ReadString(GetObject(patch, "attractionDetails"), "externalSource");
            string? externalId = ReadString(GetObject(patch, "identity"), "externalId") ?? ReadString(GetObject(patch, "attractionDetails"), "externalId");
            ParkItem? item = FindItem(mutableItems, id, name, externalSource, externalId);
            bool isNew = item is null;
            item ??= new ParkItem
            {
                ParkId = park.Id,
                Name = name ?? string.Empty,
                Category = ParkItemAdministrationDefaults.QuickCreateCategory,
                Type = ParkItemAdministrationDefaults.QuickCreateType,
                IsVisible = ParkItemAdministrationDefaults.QuickCreateIsVisible,
                AdminReviewStatus = ParkItemAdministrationDefaults.QuickCreateAdminReviewStatus,
            };
            PublicSeoParkItemSnapshot? previousItemSnapshot = isNew ? null : PublicSeoParkItemSnapshot.FromParkItem(item);

            ParkGraphUpsertChange change = BuildEntityChange("ParkItem", item.Id, key, item.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : MatchMode(id, name));
            PatchItem(item, patch, zoneKeys, manufacturerKeys, change, result, isNew);
            item.ParkId = park.Id;

            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                item = isNew
                    ? await this.parkItemRepository.CreateAsync(item, cancellationToken)
                    : await this.parkItemRepository.UpdateAsync(item.Id, item, cancellationToken) ?? item;
                change.EntityId = item.Id;
                seoChanges.ChangedItemIds.Add(item.Id);
                if (previousItemSnapshot is not null)
                {
                    seoChanges.PreviousItems.Add(previousItemSnapshot);
                }

                PublicSeoParkItemSnapshot? currentItemSnapshot = PublicSeoParkItemSnapshot.FromParkItem(item);
                if (currentItemSnapshot is not null)
                {
                    seoChanges.CurrentItems.Add(currentItemSnapshot);
                }
            }
            else if (!apply && (change.Fields.Count > 0 || isNew))
            {
                seoChanges.ChangedItemIds.Add(item.Id);
            }

            if (isNew)
            {
                mutableItems.Add(item);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                itemKeys[key] = item.Id;
            }

            if (!string.IsNullOrWhiteSpace(item.Name))
            {
                itemKeys[$"item:{NormalizeKey(item.Name)}"] = item.Id;
            }

            result.Changes.Add(change);
        }

        return seoChanges;
    }

    private sealed class ParkGraphUpsertItemSeoChanges
    {
        public List<string> ChangedItemIds { get; } = new List<string>();

        public List<PublicSeoParkItemSnapshot> PreviousItems { get; } = new List<PublicSeoParkItemSnapshot>();

        public List<PublicSeoParkItemSnapshot> CurrentItems { get; } = new List<PublicSeoParkItemSnapshot>();
    }
}
