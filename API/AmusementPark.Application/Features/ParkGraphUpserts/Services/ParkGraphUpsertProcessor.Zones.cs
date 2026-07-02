using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Search;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task<ParkGraphUpsertZoneSeoChanges> ProcessZonesAsync(JsonElement root, Park park, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        ParkGraphUpsertZoneSeoChanges seoChanges = new ParkGraphUpsertZoneSeoChanges();
        if (!root.TryGetProperty("zones", out JsonElement zones) || zones.ValueKind != JsonValueKind.Array)
        {
            return seoChanges;
        }

        IReadOnlyCollection<ParkZone> existingZones = await this.parkZoneRepository.GetByParkIdAsync(park.Id, cancellationToken);
        List<ParkZone> mutableZones = existingZones.ToList();
        foreach (JsonElement patch in zones.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ReadString(patch, "key");
            string? id = ReadString(patch, "id");
            string? name = ReadString(GetObject(patch, "identity"), "name") ?? ReadString(patch, "name");
            string? slug = ReadString(GetObject(patch, "identity"), "slug") ?? ReadString(patch, "slug");
            ParkZone? zone = FindZone(mutableZones, id, slug, name);
            bool isNew = zone is null;
            zone ??= new ParkZone { ParkId = park.Id, Name = name ?? string.Empty };
            PublicSeoParkZoneSnapshot? previousZoneSnapshot = isNew ? null : PublicSeoParkZoneSnapshot.FromParkZone(zone);

            ParkGraphUpsertChange change = BuildEntityChange("ParkZone", zone.Id, key, zone.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : MatchMode(id, name));
            PatchZone(zone, patch, change);
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
                zone = isNew
                    ? await this.parkZoneRepository.CreateAsync(zone, cancellationToken)
                    : await this.parkZoneRepository.UpdateAsync(zone.Id, zone, cancellationToken) ?? zone;
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
                seoChanges.ZoneKeys[$"zone:{NormalizeKey(zone.Name)}"] = zone.Id;
            }

            result.Changes.Add(change);
        }

        return seoChanges;
    }

    private sealed class ParkGraphUpsertZoneSeoChanges
    {
        public Dictionary<string, string> ZoneKeys { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public List<PublicSeoParkZoneSnapshot> PreviousZones { get; } = new List<PublicSeoParkZoneSnapshot>();

        public List<PublicSeoParkZoneSnapshot> CurrentZones { get; } = new List<PublicSeoParkZoneSnapshot>();
    }
}
