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
internal static class ParkGraphUpsertProcessorStandaloneAttractionsExtensions
{
    internal static async Task<bool> ProcessStandaloneAttractionAsync(this ParkGraphUpsertProcessor processorContext, JsonElement root, bool createIfMissing, Dictionary<string, string> operatorKeys, Dictionary<string, string> manufacturerKeys, Dictionary<string, string> manufacturerIdRemaps, Dictionary<string, string> standaloneAttractionKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (processorContext.standaloneAttractionRepository is null)
        {
            result.Errors.Add("Le repository des attractions autonomes n'est pas configure.");
            return false;
        }

        JsonElement? patch = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, "standaloneAttraction");
        JsonElement? identity = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, "identity");
        JsonElement? migration = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, "migration") ?? ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, "standaloneAttractionMigration");
        if (patch is null && migration is null)
        {
            result.Errors.Add("Le document standalone doit contenir un objet 'standaloneAttraction' ou 'migration'.");
            return false;
        }

        StandaloneAttraction? attraction = await processorContext.ResolveStandaloneAttractionAsync(patch, identity, migration, createIfMissing, processorContext.standaloneAttractionRepository, result, cancellationToken);
        if (attraction is null)
        {
            return false;
        }

        bool isNew = string.IsNullOrWhiteSpace(attraction.Id) || await processorContext.standaloneAttractionRepository.GetByIdAsync(attraction.Id, true, cancellationToken)is null;
        string? key = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "key") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(identity, "standaloneAttractionKey") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(identity, "key");
        ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("StandaloneAttraction", attraction.Id, key, attraction.Name, isNew ? "Created" : "Unchanged", isNew ? "createIfMissing" : "id");
        if (patch is not null)
        {
            processorContext.PatchStandaloneAttraction(attraction, patch.Value, operatorKeys, manufacturerKeys, manufacturerIdRemaps, change, result, isNew);
        }

        bool changed = change.Fields.Count > 0 || isNew;
        if (changed)
        {
            change.ChangeType = isNew ? "Created" : "Updated";
        }

        if (apply && changed)
        {
            attraction = isNew ? await processorContext.standaloneAttractionRepository.CreateAsync(attraction, cancellationToken) : await processorContext.standaloneAttractionRepository.UpdateAsync(attraction.Id, attraction, cancellationToken) ?? attraction;
            change.EntityId = attraction.Id;
        }

        result.TargetStandaloneAttractionId = attraction.Id;
        result.TargetStandaloneAttractionName = attraction.Name;
        ParkGraphUpsertProcessorStandaloneAttractionsExtensions.RegisterStandaloneAttractionKeys(attraction, key, standaloneAttractionKeys);
        result.Changes.Add(change);
        if (migration is not null)
        {
            await processorContext.RetireMigratedParkEntitiesAsync(migration.Value, result, apply, cancellationToken);
        }

        if (apply && changed && !string.IsNullOrWhiteSpace(attraction.Id))
        {
            await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.StandaloneAttractions, attraction.Id, cancellationToken);
        }

        return changed;
    }

    internal static async Task<StandaloneAttraction?> ResolveStandaloneAttractionAsync(this ParkGraphUpsertProcessor processorContext, JsonElement? patch, JsonElement? identity, JsonElement? migration, bool createIfMissing, IStandaloneAttractionRepository standaloneAttractionRepository, ParkGraphUpsertResult result, CancellationToken cancellationToken)
    {
        string? id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "id") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(identity, "standaloneAttractionId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(identity, "id") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(migration, "targetStandaloneAttractionId");
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

        string? legacyParkId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "legacyParkId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(identity, "legacyParkId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(migration, "legacyParkId");
        string? legacyParkItemId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "legacyParkItemId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(identity, "legacyParkItemId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(migration, "legacyParkItemId");
        StandaloneAttraction? existingByLegacy = await standaloneAttractionRepository.FindByLegacyAsync(legacyParkId, legacyParkItemId, cancellationToken);
        if (existingByLegacy is not null)
        {
            return existingByLegacy;
        }

        if (migration is not null)
        {
            StandaloneAttraction? migrated = await processorContext.BuildStandaloneAttractionFromMigrationAsync(migration.Value, result, cancellationToken);
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
            Name = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "name") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(identity, "name") ?? string.Empty,
            CountryCode = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "countryCode") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(identity, "countryCode"),
            Type = ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnum(patch, "type", ParkItemType.Attraction),
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
            LegacyParkId = legacyParkId,
            LegacyParkItemId = legacyParkItemId,
        };
        if (!string.IsNullOrWhiteSpace(id))
        {
            created.Id = id.Trim();
        }

        ParkGraphUpsertProcessorStandaloneAttractionsExtensions.ApplyOptionalStandalonePosition(created, patch);
        return created;
    }

    internal static async Task<StandaloneAttraction?> BuildStandaloneAttractionFromMigrationAsync(this ParkGraphUpsertProcessor processorContext, JsonElement migration, ParkGraphUpsertResult result, CancellationToken cancellationToken)
    {
        string? legacyParkId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(migration, "legacyParkId");
        if (string.IsNullOrWhiteSpace(legacyParkId))
        {
            result.Errors.Add("La migration standalone requiert 'legacyParkId'.");
            return null;
        }

        Park? sourcePark = await processorContext.parkRepository.GetByIdAsync(legacyParkId.Trim(), true, cancellationToken);
        if (sourcePark is null)
        {
            result.Errors.Add($"Aucun parc source ne correspond a legacyParkId '{legacyParkId}'.");
            return null;
        }

        string? legacyParkItemId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(migration, "legacyParkItemId");
        ParkItem? sourceItem = null;
        if (!string.IsNullOrWhiteSpace(legacyParkItemId))
        {
            sourceItem = await processorContext.parkItemRepository.GetByIdAsync(legacyParkItemId.Trim(), true, cancellationToken);
            if (sourceItem is null || !string.Equals(sourceItem.ParkId, sourcePark.Id, StringComparison.Ordinal))
            {
                result.Errors.Add($"Aucun parkItem source ne correspond a legacyParkItemId '{legacyParkItemId}' pour le parc '{sourcePark.Id}'.");
                return null;
            }
        }
        else
        {
            IReadOnlyCollection<ParkItem> items = await processorContext.parkItemRepository.GetByParkIdAsync(sourcePark.Id, true, cancellationToken);
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

    internal static void PatchStandaloneAttraction(this ParkGraphUpsertProcessor processorContext, StandaloneAttraction attraction, JsonElement patch, Dictionary<string, string> operatorKeys, Dictionary<string, string> manufacturerKeys, Dictionary<string, string> manufacturerIdRemaps, ParkGraphUpsertChange change, ParkGraphUpsertResult result, bool isNew)
    {
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "name", attraction.Name, value => attraction.Name = value ?? string.Empty, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "countryCode", attraction.CountryCode, value => attraction.CountryCode = value?.ToUpperInvariant(), change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchEnum(patch, "type", attraction.Type, value => attraction.Type = value == ParkItemType.Other ? ParkItemType.Attraction : value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "subtype", attraction.Subtype, value => attraction.Subtype = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "operatorId", attraction.OperatorId, value => attraction.OperatorId = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "websiteUrl", attraction.WebsiteUrl, value => attraction.WebsiteUrl = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "street", attraction.Street, value => attraction.Street = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "city", attraction.City, value => attraction.City = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "postalCode", attraction.PostalCode, value => attraction.PostalCode = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBool(patch, "isVisible", attraction.IsVisible, value => attraction.IsVisible = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchEnum(patch, "adminReviewStatus", attraction.AdminReviewStatus, value => attraction.AdminReviewStatus = ParkGraphUpsertProcessorStandaloneAttractionsExtensions.NormalizeStandaloneAdminReviewStatus(value), change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "legacyParkId", attraction.LegacyParkId, value => attraction.LegacyParkId = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "legacyParkItemId", attraction.LegacyParkItemId, value => attraction.LegacyParkItemId = value, change);
        string? operatorKey = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "operatorKey");
        if (!string.IsNullOrWhiteSpace(operatorKey) && operatorKeys.TryGetValue(operatorKey, out string? operatorId))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "operatorId", attraction.OperatorId, operatorId);
            attraction.OperatorId = operatorId;
        }
        else if (!string.IsNullOrWhiteSpace(operatorKey))
        {
            result.Warnings.Add($"OperatorKey '{operatorKey}' non resolue pour l'attraction autonome '{attraction.Name}'.");
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "descriptions"))
        {
            attraction.Descriptions = ParkGraphUpsertProcessorLocalizedTextExtensions.PatchLocalizedTexts(attraction.Descriptions, ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "descriptions"), false, change, "descriptions");
        }

        ParkGraphUpsertProcessorStandaloneAttractionsExtensions.ApplyOptionalStandalonePositionPatch(attraction, patch, change, isNew, result);
        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "attractionDetails"))
        {
            attraction.AttractionDetails ??= new AttractionDetails();
            processorContext.PatchAttractionDetails(attraction.AttractionDetails, ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, "attractionDetails"), manufacturerKeys, manufacturerIdRemaps, change, result, attraction.Name);
        }
        else if (isNew && attraction.Type == ParkItemType.Attraction)
        {
            attraction.AttractionDetails ??= new AttractionDetails();
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "attractionLocations"))
        {
            attraction.AttractionLocations ??= new AttractionLocations();
            ParkGraphUpsertProcessorPatchingExtensions.PatchAttractionLocations(attraction.AttractionLocations, ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, "attractionLocations"), change);
        }
    }

    internal static async Task RetireMigratedParkEntitiesAsync(this ParkGraphUpsertProcessor processorContext, JsonElement migration, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        if (!apply)
        {
            return;
        }

        string? legacyParkId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(migration, "legacyParkId");
        if (string.IsNullOrWhiteSpace(legacyParkId))
        {
            return;
        }

        bool retireLegacyPark = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(migration, "retireLegacyPark") ?? true;
        bool retireLegacyParkItem = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(migration, "retireLegacyParkItem") ?? true;
        if (retireLegacyPark)
        {
            Park? sourcePark = await processorContext.parkRepository.GetByIdAsync(legacyParkId.Trim(), true, cancellationToken);
            if (sourcePark is not null)
            {
                PublicSeoParkSnapshot? previousPark = PublicSeoParkSnapshot.FromPark(sourcePark);
                sourcePark.IsVisible = false;
                sourcePark.AdminReviewStatus = AdminReviewStatus.NotRelevant;
                await processorContext.parkRepository.UpdateAsync(sourcePark.Id, sourcePark, cancellationToken);
                await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, sourcePark.Id, cancellationToken);
                ParkGraphUpsertChange parkChange = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("Park", sourcePark.Id, "legacyPark", sourcePark.Name ?? sourcePark.Id, "Updated", "legacyParkId");
                ParkGraphUpsertProcessorResolutionExtensions.AddChange(parkChange, "isVisible", true, false);
                ParkGraphUpsertProcessorResolutionExtensions.AddChange(parkChange, "adminReviewStatus", null, AdminReviewStatus.NotRelevant);
                result.Changes.Add(parkChange);
                if (previousPark is not null)
                {
                    await processorContext.publicSeoUpdateNotifier.NotifyAsync(new PublicSeoUpdate { PreviousParks = new[] { previousPark }, IncludeDiscoveryPages = true, }, cancellationToken);
                }
            }
        }

        string? legacyParkItemId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(migration, "legacyParkItemId");
        if (retireLegacyParkItem && !string.IsNullOrWhiteSpace(legacyParkItemId))
        {
            ParkItem? sourceItem = await processorContext.parkItemRepository.GetByIdAsync(legacyParkItemId.Trim(), true, cancellationToken);
            if (sourceItem is not null)
            {
                sourceItem.IsVisible = false;
                sourceItem.AdminReviewStatus = AdminReviewStatus.NotRelevant;
                await processorContext.parkItemRepository.UpdateAsync(sourceItem.Id, sourceItem, cancellationToken);
                await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, sourceItem.Id, cancellationToken);
                ParkGraphUpsertChange itemChange = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("ParkItem", sourceItem.Id, "legacyParkItem", sourceItem.Name, "Updated", "legacyParkItemId");
                ParkGraphUpsertProcessorResolutionExtensions.AddChange(itemChange, "isVisible", true, false);
                ParkGraphUpsertProcessorResolutionExtensions.AddChange(itemChange, "adminReviewStatus", null, AdminReviewStatus.NotRelevant);
                result.Changes.Add(itemChange);
            }
        }
    }

    internal static void ApplyOptionalStandalonePosition(StandaloneAttraction attraction, JsonElement? patch)
    {
        double? latitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(patch, "latitude");
        double? longitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(patch, "longitude");
        if (latitude.HasValue && longitude.HasValue)
        {
            attraction.SetPosition(latitude.Value, longitude.Value);
        }
    }

    internal static void ApplyOptionalStandalonePositionPatch(StandaloneAttraction attraction, JsonElement patch, ParkGraphUpsertChange change, bool isNew, ParkGraphUpsertResult result)
    {
        bool hasLatitude = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "latitude");
        bool hasLongitude = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "longitude");
        if (!hasLatitude && !hasLongitude)
        {
            return;
        }

        double? latitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(patch, "latitude") ?? attraction.Position?.Latitude;
        double? longitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(patch, "longitude") ?? attraction.Position?.Longitude;
        if (latitude.HasValue && longitude.HasValue)
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "position", ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(attraction.Position), $"{latitude.Value.ToString(CultureInfo.InvariantCulture)},{longitude.Value.ToString(CultureInfo.InvariantCulture)}");
            attraction.SetPosition(latitude.Value, longitude.Value);
        }
        else if (isNew)
        {
            result.Warnings.Add("L'attraction autonome creee n'a pas de coordonnees completes.");
        }
    }

    internal static void RegisterStandaloneAttractionKeys(StandaloneAttraction attraction, string? key, Dictionary<string, string> standaloneAttractionKeys)
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
            standaloneAttractionKeys[ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(key)] = attraction.Id;
        }

        if (!string.IsNullOrWhiteSpace(attraction.Name))
        {
            standaloneAttractionKeys[$"standalone-attraction:{ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(attraction.Name)}"] = attraction.Id;
        }
    }

    internal static AdminReviewStatus NormalizeStandaloneAdminReviewStatus(AdminReviewStatus value)
    {
        return value == AdminReviewStatus.Ready ? AdminReviewStatus.Validated : value;
    }
}
