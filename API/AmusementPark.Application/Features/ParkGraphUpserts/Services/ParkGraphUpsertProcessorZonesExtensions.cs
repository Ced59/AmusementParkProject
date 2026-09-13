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
internal static class ParkGraphUpsertProcessorZonesExtensions
{
    internal static async Task<ParkGraphUpsertZoneSeoChanges> ProcessZonesAsync(this ParkGraphUpsertProcessor processorContext, JsonElement root, Park park, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        ParkGraphUpsertZoneSeoChanges seoChanges = new ParkGraphUpsertZoneSeoChanges();
        if (!root.TryGetProperty("zones", out JsonElement zones) || zones.ValueKind != JsonValueKind.Array)
        {
            return seoChanges;
        }

        IReadOnlyCollection<ParkZone> existingZones = await processorContext.parkZoneRepository.GetByParkIdAsync(park.Id, cancellationToken);
        List<ParkZone> mutableZones = existingZones.ToList();
        foreach (JsonElement patch in zones.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "key");
            string? id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "id");
            string? name = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, "identity"), "name") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "name");
            string? slug = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, "identity"), "slug") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "slug");
            ParkZone? zone = ParkGraphUpsertProcessorResolutionExtensions.FindZone(mutableZones, id, slug, name);
            bool isNew = zone is null;
            zone ??= new ParkZone
            {
                ParkId = park.Id,
                Name = name ?? string.Empty
            };
            PublicSeoParkZoneSnapshot? previousZoneSnapshot = isNew ? null : PublicSeoParkZoneSnapshot.FromParkZone(zone);
            ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("ParkZone", zone.Id, key, zone.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : ParkGraphUpsertProcessorJsonReadingExtensions.MatchMode(id, name));
            ParkGraphUpsertProcessorPatchingExtensions.PatchZone(zone, patch, change);
            zone.ParkId = park.Id;
            bool zoneChanged = change.Fields.Count > 0 || isNew;
            if (zoneChanged)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
                if (previousZoneSnapshot is not null)
                {
                    seoChanges.PreviousZones.Add(previousZoneSnapshot);
                }
            }

            if (apply && zoneChanged)
            {
                zone = isNew ? await processorContext.parkZoneRepository.CreateAsync(zone, cancellationToken) : await processorContext.parkZoneRepository.UpdateAsync(zone.Id, zone, cancellationToken) ?? zone;
                change.EntityId = zone.Id;
            }

            if (zoneChanged)
            {
                PublicSeoParkZoneSnapshot? currentZoneSnapshot = PublicSeoParkZoneSnapshot.FromParkZone(zone);
                if (currentZoneSnapshot is not null)
                {
                    seoChanges.CurrentZones.Add(currentZoneSnapshot);
                }
            }

            if (isNew)
            {
                mutableZones.Add(zone);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                seoChanges.ZoneKeys[key] = zone.Id;
            }

            if (!string.IsNullOrWhiteSpace(zone.Name))
            {
                seoChanges.ZoneKeys[$"zone:{ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(zone.Name)}"] = zone.Id;
            }

            result.Changes.Add(change);
        }

        return seoChanges;
    }
}
