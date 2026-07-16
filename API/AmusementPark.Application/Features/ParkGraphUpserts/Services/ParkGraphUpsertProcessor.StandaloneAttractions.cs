using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task ProcessStandaloneAttractionAsync(
        JsonElement root,
        bool createIfMissing,
        Dictionary<string, string> operatorKeys,
        Dictionary<string, string> manufacturerKeys,
        Dictionary<string, string> manufacturerIdRemaps,
        Dictionary<string, string> standaloneAttractionKeys,
        ParkGraphUpsertResult result,
        bool apply,
        CancellationToken cancellationToken)
    {
        if (this.standaloneAttractionRepository is null)
        {
            result.Errors.Add("Le repository des attractions autonomes n'est pas configure.");
            return;
        }

        JsonElement? patch = GetObject(root, "standaloneAttraction");
        JsonElement? identity = GetObject(root, "identity");
        JsonElement? migration = GetObject(root, "migration") ?? GetObject(root, "standaloneAttractionMigration");

        if (patch is null && migration is null)
        {
            result.Errors.Add("Le document standalone doit contenir un objet 'standaloneAttraction' ou 'migration'.");
            return;
        }

        StandaloneAttraction? attraction = await this.ResolveStandaloneAttractionAsync(
            patch,
            identity,
            migration,
            createIfMissing,
            this.standaloneAttractionRepository,
            result,
            cancellationToken);

        if (attraction is null)
        {
            return;
        }

        bool isNew = string.IsNullOrWhiteSpace(attraction.Id)
            || await this.standaloneAttractionRepository.GetByIdAsync(attraction.Id, true, cancellationToken) is null;
        string? key = ReadString(patch, "key") ?? ReadString(identity, "standaloneAttractionKey") ?? ReadString(identity, "key");
        ParkGraphUpsertChange change = BuildEntityChange(
            "StandaloneAttraction",
            attraction.Id,
            key,
            attraction.Name,
            isNew ? "Created" : "Unchanged",
            isNew ? "createIfMissing" : "id");

        if (patch is not null)
        {
            this.PatchStandaloneAttraction(attraction, patch.Value, operatorKeys, manufacturerKeys, manufacturerIdRemaps, change, result, isNew);
        }

        if (change.Fields.Count > 0 || isNew)
        {
            change.ChangeType = isNew ? "Created" : "Updated";
        }

        if (apply && (change.Fields.Count > 0 || isNew))
        {
            attraction = isNew
                ? await this.standaloneAttractionRepository.CreateAsync(attraction, cancellationToken)
                : await this.standaloneAttractionRepository.UpdateAsync(attraction.Id, attraction, cancellationToken) ?? attraction;
            change.EntityId = attraction.Id;
        }

        result.TargetStandaloneAttractionId = attraction.Id;
        result.TargetStandaloneAttractionName = attraction.Name;
        RegisterStandaloneAttractionKeys(attraction, key, standaloneAttractionKeys);
        result.Changes.Add(change);

        if (migration is not null)
        {
            await this.RetireMigratedParkEntitiesAsync(migration.Value, result, apply, cancellationToken);
        }
    }

    private async Task<StandaloneAttraction?> ResolveStandaloneAttractionAsync(
        JsonElement? patch,
        JsonElement? identity,
        JsonElement? migration,
        bool createIfMissing,
        IStandaloneAttractionRepository standaloneAttractionRepository,
        ParkGraphUpsertResult result,
        CancellationToken cancellationToken)
    {
        string? id = ReadString(patch, "id")
            ?? ReadString(identity, "standaloneAttractionId")
            ?? ReadString(identity, "id")
            ?? ReadString(migration, "targetStandaloneAttractionId");

        if (!string.IsNullOrWhiteSpace(id))
        {
            StandaloneAttraction? existing = await standaloneAttractionRepository.GetByIdAsync(id.Trim(), true, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }

            if (!createIfMissing && migration is null)
            {
                result.Errors.Add($"Aucune attraction autonome existante ne correspond a l'identifiant '{id}'.");
                return null;
            }
        }

        string? legacyParkId = ReadString(patch, "legacyParkId")
            ?? ReadString(identity, "legacyParkId")
            ?? ReadString(migration, "legacyParkId");
        string? legacyParkItemId = ReadString(patch, "legacyParkItemId")
            ?? ReadString(identity, "legacyParkItemId")
            ?? ReadString(migration, "legacyParkItemId");

        StandaloneAttraction? existingByLegacy = await standaloneAttractionRepository.FindByLegacyAsync(legacyParkId, legacyParkItemId, cancellationToken);
        if (existingByLegacy is not null)
        {
            return existingByLegacy;
        }

        if (migration is not null)
        {
            StandaloneAttraction? migrated = await this.BuildStandaloneAttractionFromMigrationAsync(migration.Value, result, cancellationToken);
            if (migrated is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                migrated.Id = id.Trim();
            }

            return migrated;
        }

        if (!createIfMissing)
        {
            result.Errors.Add("Aucune attraction autonome cible resolue. Selectionner une attraction existante, fournir une migration ou activer createIfMissing.");
            return null;
        }

        StandaloneAttraction created = new StandaloneAttraction
        {
            Name = ReadString(patch, "name") ?? ReadString(identity, "name") ?? string.Empty,
            CountryCode = ReadString(patch, "countryCode") ?? ReadString(identity, "countryCode"),
            Type = ReadEnum(patch, "type", ParkItemType.Attraction),
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
            LegacyParkId = legacyParkId,
            LegacyParkItemId = legacyParkItemId,
        };
        if (!string.IsNullOrWhiteSpace(id))
        {
            created.Id = id.Trim();
        }

        ApplyOptionalStandalonePosition(created, patch);
        return created;
    }

    private async Task<StandaloneAttraction?> BuildStandaloneAttractionFromMigrationAsync(
        JsonElement migration,
        ParkGraphUpsertResult result,
        CancellationToken cancellationToken)
    {
        string? legacyParkId = ReadString(migration, "legacyParkId");
        if (string.IsNullOrWhiteSpace(legacyParkId))
        {
            result.Errors.Add("La migration standalone requiert 'legacyParkId'.");
            return null;
        }

        Park? sourcePark = await this.parkRepository.GetByIdAsync(legacyParkId.Trim(), true, cancellationToken);
        if (sourcePark is null)
        {
            result.Errors.Add($"Aucun parc source ne correspond a legacyParkId '{legacyParkId}'.");
            return null;
        }

        string? legacyParkItemId = ReadString(migration, "legacyParkItemId");
        ParkItem? sourceItem = null;
        if (!string.IsNullOrWhiteSpace(legacyParkItemId))
        {
            sourceItem = await this.parkItemRepository.GetByIdAsync(legacyParkItemId.Trim(), true, cancellationToken);
            if (sourceItem is null || !string.Equals(sourceItem.ParkId, sourcePark.Id, StringComparison.Ordinal))
            {
                result.Errors.Add($"Aucun parkItem source ne correspond a legacyParkItemId '{legacyParkItemId}' pour le parc '{sourcePark.Id}'.");
                return null;
            }
        }
        else
        {
            IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetByParkIdAsync(sourcePark.Id, true, cancellationToken);
            if (items.Count == 1)
            {
                sourceItem = items.First();
            }
        }

        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Name = sourceItem?.Name ?? sourcePark.Name ?? string.Empty,
            CountryCode = sourcePark.CountryCode,
            Type = sourceItem?.Type ?? ParkItemType.Attraction,
            Subtype = sourceItem?.Subtype,
            OperatorId = sourcePark.OperatorId,
            WebsiteUrl = sourcePark.WebsiteUrl,
            Street = sourcePark.Street,
            City = sourcePark.City,
            PostalCode = sourcePark.PostalCode,
            Descriptions = sourceItem?.Descriptions.Count > 0 ? sourceItem.Descriptions.ToList() : sourcePark.Descriptions.ToList(),
            AttractionDetails = sourceItem?.AttractionDetails,
            AttractionLocations = sourceItem?.AttractionLocations,
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
            LegacyParkId = sourcePark.Id,
            LegacyParkItemId = sourceItem?.Id,
        };

        if (sourceItem?.Position is not null)
        {
            attraction.SetPosition(sourceItem.Position.Latitude, sourceItem.Position.Longitude);
        }
        else if (sourcePark.Position is not null)
        {
            attraction.SetPosition(sourcePark.Position.Latitude, sourcePark.Position.Longitude);
        }

        return attraction;
    }

    private void PatchStandaloneAttraction(
        StandaloneAttraction attraction,
        JsonElement patch,
        Dictionary<string, string> operatorKeys,
        Dictionary<string, string> manufacturerKeys,
        Dictionary<string, string> manufacturerIdRemaps,
        ParkGraphUpsertChange change,
        ParkGraphUpsertResult result,
        bool isNew)
    {
        PatchString(patch, "name", attraction.Name, value => attraction.Name = value ?? string.Empty, change);
        PatchString(patch, "countryCode", attraction.CountryCode, value => attraction.CountryCode = value?.ToUpperInvariant(), change);
        PatchEnum(patch, "type", attraction.Type, value => attraction.Type = value == ParkItemType.Other ? ParkItemType.Attraction : value, change);
        PatchString(patch, "subtype", attraction.Subtype, value => attraction.Subtype = value, change);
        PatchString(patch, "operatorId", attraction.OperatorId, value => attraction.OperatorId = value, change);
        PatchString(patch, "websiteUrl", attraction.WebsiteUrl, value => attraction.WebsiteUrl = value, change);
        PatchString(patch, "street", attraction.Street, value => attraction.Street = value, change);
        PatchString(patch, "city", attraction.City, value => attraction.City = value, change);
        PatchString(patch, "postalCode", attraction.PostalCode, value => attraction.PostalCode = value, change);
        PatchBool(patch, "isVisible", attraction.IsVisible, value => attraction.IsVisible = value, change);
        PatchEnum(patch, "adminReviewStatus", attraction.AdminReviewStatus, value => attraction.AdminReviewStatus = NormalizeStandaloneAdminReviewStatus(value), change);
        PatchString(patch, "legacyParkId", attraction.LegacyParkId, value => attraction.LegacyParkId = value, change);
        PatchString(patch, "legacyParkItemId", attraction.LegacyParkItemId, value => attraction.LegacyParkItemId = value, change);

        string? operatorKey = ReadString(patch, "operatorKey");
        if (!string.IsNullOrWhiteSpace(operatorKey) && operatorKeys.TryGetValue(operatorKey, out string? operatorId))
        {
            AddChange(change, "operatorId", attraction.OperatorId, operatorId);
            attraction.OperatorId = operatorId;
        }
        else if (!string.IsNullOrWhiteSpace(operatorKey))
        {
            result.Warnings.Add($"OperatorKey '{operatorKey}' non resolue pour l'attraction autonome '{attraction.Name}'.");
        }

        if (HasProperty(patch, "descriptions"))
        {
            attraction.Descriptions = PatchLocalizedTexts(attraction.Descriptions, GetArray(patch, "descriptions"), false, change, "descriptions");
        }

        ApplyOptionalStandalonePositionPatch(attraction, patch, change, isNew, result);

        if (HasProperty(patch, "attractionDetails"))
        {
            attraction.AttractionDetails ??= new AttractionDetails();
            this.PatchAttractionDetails(attraction.AttractionDetails, GetObject(patch, "attractionDetails"), manufacturerKeys, manufacturerIdRemaps, change, result, attraction.Name);
        }
        else if (isNew && attraction.Type == ParkItemType.Attraction)
        {
            attraction.AttractionDetails ??= new AttractionDetails();
        }

        if (HasProperty(patch, "attractionLocations"))
        {
            attraction.AttractionLocations ??= new AttractionLocations();
            PatchAttractionLocations(attraction.AttractionLocations, GetObject(patch, "attractionLocations"), change);
        }
    }

    private async Task RetireMigratedParkEntitiesAsync(JsonElement migration, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!apply)
        {
            return;
        }

        string? legacyParkId = ReadString(migration, "legacyParkId");
        if (string.IsNullOrWhiteSpace(legacyParkId))
        {
            return;
        }

        bool retireLegacyPark = ReadBool(migration, "retireLegacyPark") ?? true;
        bool retireLegacyParkItem = ReadBool(migration, "retireLegacyParkItem") ?? true;

        if (retireLegacyPark)
        {
            Park? sourcePark = await this.parkRepository.GetByIdAsync(legacyParkId.Trim(), true, cancellationToken);
            if (sourcePark is not null)
            {
                PublicSeoParkSnapshot? previousPark = PublicSeoParkSnapshot.FromPark(sourcePark);
                sourcePark.IsVisible = false;
                sourcePark.AdminReviewStatus = AdminReviewStatus.NotRelevant;
                await this.parkRepository.UpdateAsync(sourcePark.Id, sourcePark, cancellationToken);
                ParkGraphUpsertChange parkChange = BuildEntityChange("Park", sourcePark.Id, "legacyPark", sourcePark.Name ?? sourcePark.Id, "Updated", "legacyParkId");
                AddChange(parkChange, "isVisible", true, false);
                AddChange(parkChange, "adminReviewStatus", null, AdminReviewStatus.NotRelevant);
                result.Changes.Add(parkChange);
                if (previousPark is not null)
                {
                    await this.publicSeoUpdateNotifier.NotifyAsync(
                        new PublicSeoUpdate
                        {
                            PreviousParks = new[] { previousPark },
                            IncludeDiscoveryPages = true,
                        },
                        cancellationToken);
                }
            }
        }

        string? legacyParkItemId = ReadString(migration, "legacyParkItemId");
        if (retireLegacyParkItem && !string.IsNullOrWhiteSpace(legacyParkItemId))
        {
            ParkItem? sourceItem = await this.parkItemRepository.GetByIdAsync(legacyParkItemId.Trim(), true, cancellationToken);
            if (sourceItem is not null)
            {
                sourceItem.IsVisible = false;
                sourceItem.AdminReviewStatus = AdminReviewStatus.NotRelevant;
                await this.parkItemRepository.UpdateAsync(sourceItem.Id, sourceItem, cancellationToken);
                ParkGraphUpsertChange itemChange = BuildEntityChange("ParkItem", sourceItem.Id, "legacyParkItem", sourceItem.Name, "Updated", "legacyParkItemId");
                AddChange(itemChange, "isVisible", true, false);
                AddChange(itemChange, "adminReviewStatus", null, AdminReviewStatus.NotRelevant);
                result.Changes.Add(itemChange);
            }
        }
    }

    private static void ApplyOptionalStandalonePosition(StandaloneAttraction attraction, JsonElement? patch)
    {
        double? latitude = ReadDouble(patch, "latitude");
        double? longitude = ReadDouble(patch, "longitude");
        if (latitude.HasValue && longitude.HasValue)
        {
            attraction.SetPosition(latitude.Value, longitude.Value);
        }
    }

    private static void ApplyOptionalStandalonePositionPatch(
        StandaloneAttraction attraction,
        JsonElement patch,
        ParkGraphUpsertChange change,
        bool isNew,
        ParkGraphUpsertResult result)
    {
        bool hasLatitude = HasProperty(patch, "latitude");
        bool hasLongitude = HasProperty(patch, "longitude");
        if (!hasLatitude && !hasLongitude)
        {
            return;
        }

        double? latitude = ReadDouble(patch, "latitude") ?? attraction.Position?.Latitude;
        double? longitude = ReadDouble(patch, "longitude") ?? attraction.Position?.Longitude;
        if (latitude.HasValue && longitude.HasValue)
        {
            AddChange(change, "position", FormatPosition(attraction.Position), $"{latitude.Value.ToString(CultureInfo.InvariantCulture)},{longitude.Value.ToString(CultureInfo.InvariantCulture)}");
            attraction.SetPosition(latitude.Value, longitude.Value);
        }
        else if (isNew)
        {
            result.Warnings.Add("L'attraction autonome creee n'a pas de coordonnees completes.");
        }
    }

    private static void RegisterStandaloneAttractionKeys(
        StandaloneAttraction attraction,
        string? key,
        Dictionary<string, string> standaloneAttractionKeys)
    {
        if (string.IsNullOrWhiteSpace(attraction.Id))
        {
            return;
        }

        standaloneAttractionKeys["standaloneAttraction"] = attraction.Id;
        standaloneAttractionKeys["standalone-attraction"] = attraction.Id;
        standaloneAttractionKeys["attraction"] = attraction.Id;

        if (!string.IsNullOrWhiteSpace(key))
        {
            standaloneAttractionKeys[key.Trim()] = attraction.Id;
            standaloneAttractionKeys[NormalizeKey(key)] = attraction.Id;
        }

        if (!string.IsNullOrWhiteSpace(attraction.Name))
        {
            standaloneAttractionKeys[$"standalone-attraction:{NormalizeKey(attraction.Name)}"] = attraction.Id;
        }
    }

    private static AdminReviewStatus NormalizeStandaloneAdminReviewStatus(AdminReviewStatus value)
    {
        return value == AdminReviewStatus.Ready ? AdminReviewStatus.Validated : value;
    }
}
