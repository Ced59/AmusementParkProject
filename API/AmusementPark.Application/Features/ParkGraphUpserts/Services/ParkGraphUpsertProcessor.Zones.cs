using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
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
    private async Task<Dictionary<string, string>> ProcessZonesAsync(JsonElement root, Park park, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        Dictionary<string, string> zoneKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty("zones", out JsonElement zones) || zones.ValueKind != JsonValueKind.Array)
        {
            return zoneKeys;
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

            ParkGraphUpsertChange change = BuildEntityChange("ParkZone", zone.Id, key, zone.Name, isNew ? "Created" : "Unchanged", isNew ? "name" : MatchMode(id, name));
            PatchZone(zone, patch, change);
            zone.ParkId = park.Id;

            if (change.Fields.Count > 0 || isNew)
            {
                change.ChangeType = isNew ? "Created" : "Updated";
            }

            if (apply && (change.Fields.Count > 0 || isNew))
            {
                zone = isNew
                    ? await this.parkZoneRepository.CreateAsync(zone, cancellationToken)
                    : await this.parkZoneRepository.UpdateAsync(zone.Id, zone, cancellationToken) ?? zone;
                change.EntityId = zone.Id;
            }

            if (isNew)
            {
                mutableZones.Add(zone);
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                zoneKeys[key] = zone.Id;
            }

            if (!string.IsNullOrWhiteSpace(zone.Name))
            {
                zoneKeys[$"zone:{NormalizeKey(zone.Name)}"] = zone.Id;
            }

            result.Changes.Add(change);
        }

        return zoneKeys;
    }
}
