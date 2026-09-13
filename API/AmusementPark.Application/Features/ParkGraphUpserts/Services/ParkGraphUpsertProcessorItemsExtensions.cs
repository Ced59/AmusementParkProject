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
internal static class ParkGraphUpsertProcessorItemsExtensions
{
    internal static async Task<ParkGraphUpsertItemSeoChanges> ProcessItemsAsync(this ParkGraphUpsertProcessor processorContext, JsonElement root, Park park, Dictionary<string, string> zoneKeys, Dictionary<string, string> manufacturerKeys, Dictionary<string, string> manufacturerIdRemaps, Dictionary<string, string> itemKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        ParkGraphUpsertItemSeoChanges seoChanges = new ParkGraphUpsertItemSeoChanges();
        if (!root.TryGetProperty("items", out JsonElement items) || items.ValueKind != JsonValueKind.Array)
        {
            return seoChanges;
        }

        IReadOnlyCollection<ParkItem> existingItems = await processorContext.parkItemRepository.GetByParkIdAsync(park.Id, true, cancellationToken);
        List<ParkItem> mutableItems = existingItems.ToList();
        foreach (JsonElement patch in items.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "key");
            string? id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "id");
            string? name = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, "identity"), "name") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "name");
            string? externalSource = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, "identity"), "externalSource") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, "attractionDetails"), "externalSource");
            string? externalId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, "identity"), "externalId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, "attractionDetails"), "externalId");
            ParkItem? item = ParkGraphUpsertProcessorResolutionExtensions.FindItem(mutableItems, id, name, externalSource, externalId);
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
            ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("ParkItem", item.Id, key, item.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : ParkGraphUpsertProcessorJsonReadingExtensions.MatchMode(id, name));
            processorContext.PatchItem(item, patch, zoneKeys, manufacturerKeys, manufacturerIdRemaps, change, result, isNew);
            item.ParkId = park.Id;
            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                item = isNew ? await processorContext.parkItemRepository.CreateAsync(item, cancellationToken) : await processorContext.parkItemRepository.UpdateAsync(item.Id, item, cancellationToken) ?? item;
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
                itemKeys[$"item:{ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(item.Name)}"] = item.Id;
            }

            result.Changes.Add(change);
        }

        return seoChanges;
    }
}
