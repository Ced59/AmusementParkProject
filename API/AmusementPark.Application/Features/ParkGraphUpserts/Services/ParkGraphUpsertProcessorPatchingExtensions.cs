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
internal static class ParkGraphUpsertProcessorPatchingExtensions
{
    internal static Park BuildNewParkFromPatch(JsonElement? parkPatch, JsonElement? identity, ParkGraphUpsertResult result)
    {
        Park park = new Park
        {
            Name = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(parkPatch, "name") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(identity, "name"),
            CountryCode = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(parkPatch, "countryCode") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(identity, "countryCode"),
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };
        double? latitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(parkPatch, "latitude");
        double? longitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(parkPatch, "longitude");
        if (latitude.HasValue && longitude.HasValue)
        {
            park.SetPosition(latitude.Value, longitude.Value);
        }
        else
        {
            result.Warnings.Add("Création de parc demandée sans latitude/longitude complètes : coordonnées non définies.");
        }

        return park;
    }

    internal static void PatchPark(Park park, JsonElement? patch, JsonElement? identity, Dictionary<string, string> founderKeys, Dictionary<string, string> operatorKeys, ParkGraphUpsertChange change, ParkGraphUpsertResult result, bool isNew)
    {
        if (patch is null)
        {
            return;
        }

        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "name", park.Name, value => park.Name = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "countryCode", park.CountryCode, value => park.CountryCode = value?.ToUpperInvariant(), change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchEnumNullable(patch, "type", park.Type, value => park.Type = value, change, "type");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchEnumNullable(patch, "audienceClassification", park.AudienceClassification, value => park.AudienceClassification = value, change, "audienceClassification");
        ParkGraphUpsertProcessorPatchingExtensions.PatchParkStatus(patch, park, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchLifecycleDate(patch, "openingDate", "openingDateText", park.OpeningDate, park.OpeningDateText, value => park.OpeningDate = value, value => park.OpeningDateText = value, change, "openingDate", "openingDateText");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchLifecycleDate(patch, "closingDate", "closingDateText", park.ClosingDate, park.ClosingDateText, value => park.ClosingDate = value, value => park.ClosingDateText = value, change, "closingDate", "closingDateText");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "founderId", park.FounderId, value => park.FounderId = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "operatorId", park.OperatorId, value => park.OperatorId = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "websiteUrl", park.WebsiteUrl, value => park.WebsiteUrl = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "street", park.Street, value => park.Street = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "city", park.City, value => park.City = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "postalCode", park.PostalCode, value => park.PostalCode = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBool(patch, "isVisible", park.IsVisible, value => park.IsVisible = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBool(patch, "isFeaturedOnHome", park.IsFeaturedOnHome, value => park.IsFeaturedOnHome = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBool(patch, "isFeaturedOnHomeSponsored", park.IsFeaturedOnHomeSponsored, value => park.IsFeaturedOnHomeSponsored = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchIntNullable(patch, "featuredHomeOrder", park.FeaturedHomeOrder, value => park.FeaturedHomeOrder = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchEnum(patch, "adminReviewStatus", park.AdminReviewStatus, value => park.AdminReviewStatus = value, change);
        string? founderKey = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "founderKey");
        if (!string.IsNullOrWhiteSpace(founderKey) && founderKeys.TryGetValue(founderKey, out string? founderId))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "founderId", park.FounderId, founderId);
            park.FounderId = founderId;
        }

        string? operatorKey = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "operatorKey");
        if (!string.IsNullOrWhiteSpace(operatorKey) && operatorKeys.TryGetValue(operatorKey, out string? operatorId))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "operatorId", park.OperatorId, operatorId);
            park.OperatorId = operatorId;
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "descriptions"))
        {
            park.Descriptions = ParkGraphUpsertProcessorLocalizedTextExtensions.PatchLocalizedTexts(park.Descriptions, ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "descriptions"), false, change, "descriptions");
        }

        ParkGraphOfficialMapUpsertPatcher.Patch(park, patch, result);
        bool hasLatitude = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "latitude");
        bool hasLongitude = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "longitude");
        if (hasLatitude || hasLongitude)
        {
            double? latitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(patch, "latitude") ?? park.Position?.Latitude;
            double? longitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(patch, "longitude") ?? park.Position?.Longitude;
            if (latitude.HasValue && longitude.HasValue)
            {
                ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "position", ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(park.Position), $"{latitude.Value.ToString(CultureInfo.InvariantCulture)},{longitude.Value.ToString(CultureInfo.InvariantCulture)}");
                park.SetPosition(latitude.Value, longitude.Value);
            }
            else if (isNew)
            {
                result.Warnings.Add("Le parc créé n'a pas de coordonnées complètes.");
            }
        }
    }

    internal static void PatchParkStatus(JsonElement? patch, Park park, ParkGraphUpsertChange change)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "status"))
        {
            return;
        }

        ParkStatus? next = ParkGraphUpsertProcessorPatchingExtensions.ReadParkStatus(patch, "status");
        if (!next.HasValue)
        {
            return;
        }

        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "status", park.Status, next.Value);
        park.Status = next.Value;
    }

    internal static ParkStatus? ReadParkStatus(JsonElement? element, string propertyName)
    {
        string? value = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.TryReadEnum(value, out ParkStatus parsed))
        {
            return parsed;
        }

        string normalized = ParkGraphUpsertProcessorPatchingExtensions.NormalizeStatusToken(value);
        return normalized switch
        {
            "operating" or "open" or "opened" or "enfonctionnement" => ParkStatus.Operating,
            "closeddefinitively" or "permanentlyclosed" or "definitivelyclosed" or "fermedefinitivement" => ParkStatus.ClosedDefinitively,
            "planned" or "announced" or "projectannounced" or "projetannonce" => ParkStatus.Planned,
            "underconstruction" or "constructionstarted" or "construction" or "entravaux" => ParkStatus.UnderConstruction,
            "temporarilyclosed" or "closedtemporarily" or "temporaryclosure" or "fermetemporairement" => ParkStatus.TemporarilyClosed,
            "cancelled" or "canceled" or "abandoned" or "projectcancelled" or "annule" or "abandonne" => ParkStatus.Cancelled,
            _ => null,
        };
    }

    internal static string NormalizeStatusToken(string value)
    {
        string decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(decomposed.Length);
        foreach (char character in decomposed)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark || character == '_' || character == '-' || character == ' ' || character == '\'')
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    internal static void PatchZone(ParkZone zone, JsonElement patch, ParkGraphUpsertChange change)
    {
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "name", zone.Name, value => zone.Name = value ?? string.Empty, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "slug", zone.Slug, value => zone.Slug = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBool(patch, "isVisible", zone.IsVisible, value => zone.IsVisible = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchInt(patch, "sortOrder", zone.SortOrder, value => zone.SortOrder = value, change);
        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "names"))
        {
            zone.Names = ParkGraphUpsertProcessorLocalizedTextExtensions.PatchLocalizedTexts(zone.Names, ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "names"), false, change, "names");
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "descriptions"))
        {
            zone.Descriptions = ParkGraphUpsertProcessorLocalizedTextExtensions.PatchLocalizedTexts(zone.Descriptions, ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "descriptions"), false, change, "descriptions");
        }

        ParkGraphUpsertProcessorPatchingExtensions.ApplyOptionalPositionPatch(zone, patch, change);
    }

    internal static void PatchItem(this ParkGraphUpsertProcessor processorContext, ParkItem item, JsonElement patch, Dictionary<string, string> zoneKeys, Dictionary<string, string> manufacturerKeys, Dictionary<string, string> manufacturerIdRemaps, ParkGraphUpsertChange change, ParkGraphUpsertResult result, bool isNew)
    {
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "name", item.Name, value => item.Name = value ?? string.Empty, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "subtype", item.Subtype, value => item.Subtype = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchEnum(patch, "category", item.Category, value => item.Category = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchEnum(patch, "type", item.Type, value => item.Type = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBool(patch, "isVisible", item.IsVisible, value => item.IsVisible = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchEnum(patch, "adminReviewStatus", item.AdminReviewStatus, value => item.AdminReviewStatus = value, change);
        string? zoneId = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "zoneId");
        string? zoneKey = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "zoneKey");
        if (!string.IsNullOrWhiteSpace(zoneKey) && zoneKeys.TryGetValue(zoneKey, out string? resolvedZoneId))
        {
            zoneId = resolvedZoneId;
        }
        else if (!string.IsNullOrWhiteSpace(zoneKey))
        {
            string normalizedZoneNameKey = $"zone:{ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(zoneKey)}";
            if (zoneKeys.TryGetValue(normalizedZoneNameKey, out string? resolvedByName))
            {
                zoneId = resolvedByName;
            }
            else
            {
                result.Warnings.Add($"ZoneKey '{zoneKey}' non résolue pour l'élément '{item.Name}'.");
            }
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "zoneId") || ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "zoneKey"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "zoneId", item.ZoneId, zoneId);
            item.ZoneId = zoneId;
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "descriptions"))
        {
            item.Descriptions = ParkGraphUpsertProcessorLocalizedTextExtensions.PatchLocalizedTexts(item.Descriptions, ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "descriptions"), false, change, "descriptions");
        }

        ParkGraphUpsertProcessorPatchingExtensions.ApplyOptionalPositionPatch(item, patch, change);
        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "attractionDetails"))
        {
            JsonElement? detailsPatch = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, "attractionDetails");
            item.AttractionDetails ??= new AttractionDetails();
            processorContext.PatchAttractionDetails(item.AttractionDetails, detailsPatch, manufacturerKeys, manufacturerIdRemaps, change, result, item.Name);
        }
        else if (isNew && item.Category == ParkItemCategory.Attraction)
        {
            item.AttractionDetails ??= new AttractionDetails();
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "attractionLocations"))
        {
            item.AttractionLocations ??= new AttractionLocations();
            ParkGraphUpsertProcessorPatchingExtensions.PatchAttractionLocations(item.AttractionLocations, ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, "attractionLocations"), change);
        }
    }

    internal static void PatchAttractionDetails(this ParkGraphUpsertProcessor processorContext, AttractionDetails details, JsonElement? patch, Dictionary<string, string> manufacturerKeys, Dictionary<string, string> manufacturerIdRemaps, ParkGraphUpsertChange change, ParkGraphUpsertResult result, string itemName)
    {
        if (patch is null)
        {
            return;
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "manufacturerId"))
        {
            string? requestedManufacturerId = ParkGraphUpsertProcessorMergeHelpersExtensions.RemapId(manufacturerIdRemaps, ParkGraphUpsertProcessorJsonReadingExtensions.ReadStringAllowNull(patch, "manufacturerId")?.Trim());
            if (string.IsNullOrWhiteSpace(requestedManufacturerId))
            {
                requestedManufacturerId = null;
            }

            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionDetails.manufacturerId", details.ManufacturerId, requestedManufacturerId);
            details.ManufacturerId = requestedManufacturerId;
        }

        string? manufacturerKey = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "manufacturerKey");
        if (!string.IsNullOrWhiteSpace(manufacturerKey) && manufacturerKeys.TryGetValue(manufacturerKey, out string? manufacturerId))
        {
            manufacturerId = ParkGraphUpsertProcessorMergeHelpersExtensions.RemapId(manufacturerIdRemaps, manufacturerId);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionDetails.manufacturerId", details.ManufacturerId, manufacturerId);
            details.ManufacturerId = manufacturerId;
        }
        else if (!string.IsNullOrWhiteSpace(manufacturerKey))
        {
            result.Warnings.Add($"ManufacturerKey '{manufacturerKey}' non résolue pour '{itemName}'.");
        }

        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "model", details.Model, value => details.Model = value, change, "attractionDetails.model");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "externalSource", details.ExternalSource, value => details.ExternalSource = value, change, "attractionDetails.externalSource");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "externalId", details.ExternalId, value => details.ExternalId = value, change, "attractionDetails.externalId");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "sourceUrl", details.SourceUrl, value => details.SourceUrl = value, change, "attractionDetails.sourceUrl");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "status", details.Status, value => details.Status = ParkItemStatusNormalizer.Normalize(value), change, "attractionDetails.status");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "materialType", details.MaterialType, value => details.MaterialType = value, change, "attractionDetails.materialType");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "seatingType", details.SeatingType, value => details.SeatingType = value, change, "attractionDetails.seatingType");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "launchType", details.LaunchType, value => details.LaunchType = value, change, "attractionDetails.launchType");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "restraintType", details.RestraintType, value => details.RestraintType = value, change, "attractionDetails.restraintType");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBoolNullable(patch, "isLaunched", details.IsLaunched, value => details.IsLaunched = value, change, "attractionDetails.isLaunched");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchLifecycleDate(patch, "openingDate", "openingDateText", details.OpeningDate, details.OpeningDateText, value => details.OpeningDate = value, value => details.OpeningDateText = value, change, "attractionDetails.openingDate", "attractionDetails.openingDateText");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchLifecycleDate(patch, "closingDate", "closingDateText", details.ClosingDate, details.ClosingDateText, value => details.ClosingDate = value, value => details.ClosingDateText = value, change, "attractionDetails.closingDate", "attractionDetails.closingDateText");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchIntNullable(patch, "durationInSeconds", details.DurationInSeconds, value => details.DurationInSeconds = value, change, "attractionDetails.durationInSeconds");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchIntNullable(patch, "capacityPerHour", details.CapacityPerHour, value => details.CapacityPerHour = value, change, "attractionDetails.capacityPerHour");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchDoubleNullable(patch, "heightInFeet", details.HeightInFeet, value => details.HeightInFeet = value, change, "attractionDetails.heightInFeet");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchDoubleNullable(patch, "heightInMeters", details.HeightInMeters, value => details.HeightInMeters = value, change, "attractionDetails.heightInMeters");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchDoubleNullable(patch, "lengthInFeet", details.LengthInFeet, value => details.LengthInFeet = value, change, "attractionDetails.lengthInFeet");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchDoubleNullable(patch, "lengthInMeters", details.LengthInMeters, value => details.LengthInMeters = value, change, "attractionDetails.lengthInMeters");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchDoubleNullable(patch, "speedInMph", details.SpeedInMph, value => details.SpeedInMph = value, change, "attractionDetails.speedInMph");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchDoubleNullable(patch, "speedInKmH", details.SpeedInKmH, value => details.SpeedInKmH = value, change, "attractionDetails.speedInKmH");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchDoubleNullable(patch, "dropInFeet", details.DropInFeet, value => details.DropInFeet = value, change, "attractionDetails.dropInFeet");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchDoubleNullable(patch, "dropInMeters", details.DropInMeters, value => details.DropInMeters = value, change, "attractionDetails.dropInMeters");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchIntNullable(patch, "inversionCount", details.InversionCount, value => details.InversionCount = value, change, "attractionDetails.inversionCount");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchIntNullable(patch, "trainCount", details.TrainCount, value => details.TrainCount = value, change, "attractionDetails.trainCount");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchIntNullable(patch, "carsPerTrain", details.CarsPerTrain, value => details.CarsPerTrain = value, change, "attractionDetails.carsPerTrain");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchIntNullable(patch, "ridersPerVehicle", details.RidersPerVehicle, value => details.RidersPerVehicle = value, change, "attractionDetails.ridersPerVehicle");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBoolNullable(patch, "hasSingleRider", details.HasSingleRider, value => details.HasSingleRider = value, change, "attractionDetails.hasSingleRider");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBoolNullable(patch, "hasFastPass", details.HasFastPass, value => details.HasFastPass = value, change, "attractionDetails.hasFastPass");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBoolNullable(patch, "isAccessibleForReducedMobility", details.IsAccessibleForReducedMobility, value => details.IsAccessibleForReducedMobility = value, change, "attractionDetails.isAccessibleForReducedMobility");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBoolNullable(patch, "isIndoor", details.IsIndoor, value => details.IsIndoor = value, change, "attractionDetails.isIndoor");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchEnumNullable(patch, "waterExposureLevel", details.WaterExposureLevel, value => details.WaterExposureLevel = value, change, "attractionDetails.waterExposureLevel");
        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "accessConditions"))
        {
            List<AttractionAccessCondition> conditions = ParkGraphUpsertProcessorResolutionExtensions.ReadAccessConditions(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "accessConditions"));
            foreach (AttractionAccessCondition condition in conditions)
            {
                processorContext.measurementConversionService.NormalizeAccessCondition(condition);
            }

            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionDetails.accessConditions", ParkGraphUpsertProcessorPatchingExtensions.DescribeAccessConditions(details.AccessConditions), ParkGraphUpsertProcessorPatchingExtensions.DescribeAccessConditions(conditions));
            details.AccessConditions = conditions;
        }

        processorContext.NormalizeAttractionDetailsAfterPatch(details, change, patch.Value);
    }

    internal static void NormalizeAttractionDetailsAfterPatch(this ParkGraphUpsertProcessor processorContext, AttractionDetails details, ParkGraphUpsertChange change, JsonElement patch)
    {
        double? currentHeightInFeet = details.HeightInFeet;
        double? currentHeightInMeters = details.HeightInMeters;
        double? currentLengthInFeet = details.LengthInFeet;
        double? currentLengthInMeters = details.LengthInMeters;
        double? currentSpeedInMph = details.SpeedInMph;
        double? currentSpeedInKmH = details.SpeedInKmH;
        double? currentDropInFeet = details.DropInFeet;
        double? currentDropInMeters = details.DropInMeters;
        string currentAccessConditions = ParkGraphUpsertProcessorPatchingExtensions.DescribeAccessConditions(details.AccessConditions);
        ParkGraphUpsertProcessorPatchingExtensions.PreferPatchedImperialMeasurements(details, patch);
        processorContext.measurementConversionService.NormalizeAttractionDetails(details);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionDetails.heightInFeet", currentHeightInFeet, details.HeightInFeet);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionDetails.heightInMeters", currentHeightInMeters, details.HeightInMeters);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionDetails.lengthInFeet", currentLengthInFeet, details.LengthInFeet);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionDetails.lengthInMeters", currentLengthInMeters, details.LengthInMeters);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionDetails.speedInMph", currentSpeedInMph, details.SpeedInMph);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionDetails.speedInKmH", currentSpeedInKmH, details.SpeedInKmH);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionDetails.dropInFeet", currentDropInFeet, details.DropInFeet);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionDetails.dropInMeters", currentDropInMeters, details.DropInMeters);
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionDetails.accessConditions", currentAccessConditions, ParkGraphUpsertProcessorPatchingExtensions.DescribeAccessConditions(details.AccessConditions));
    }

    internal static void PreferPatchedImperialMeasurements(AttractionDetails details, JsonElement patch)
    {
        if (ParkGraphUpsertProcessorPatchingExtensions.ShouldPreferPatchedImperialMeasurement(patch, "heightInFeet", "heightInMeters", details.HeightInFeet))
        {
            details.HeightInMeters = null;
        }

        if (ParkGraphUpsertProcessorPatchingExtensions.ShouldPreferPatchedImperialMeasurement(patch, "lengthInFeet", "lengthInMeters", details.LengthInFeet))
        {
            details.LengthInMeters = null;
        }

        if (ParkGraphUpsertProcessorPatchingExtensions.ShouldPreferPatchedImperialMeasurement(patch, "speedInMph", "speedInKmH", details.SpeedInMph))
        {
            details.SpeedInKmH = null;
        }

        if (ParkGraphUpsertProcessorPatchingExtensions.ShouldPreferPatchedImperialMeasurement(patch, "dropInFeet", "dropInMeters", details.DropInFeet))
        {
            details.DropInMeters = null;
        }
    }

    internal static bool ShouldPreferPatchedImperialMeasurement(JsonElement patch, string imperialPropertyName, string metricPropertyName, double? imperialValue)
    {
        return imperialValue.HasValue && ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, imperialPropertyName) && !ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, metricPropertyName);
    }

    internal static void PatchAttractionLocations(AttractionLocations locations, JsonElement? patch, ParkGraphUpsertChange change)
    {
        if (patch is null)
        {
            return;
        }

        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchLocationPoint(patch, "entrance", locations.Entrance, value => locations.Entrance = value, change, "attractionLocations.entrance");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchLocationPoint(patch, "exit", locations.Exit, value => locations.Exit = value, change, "attractionLocations.exit");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchLocationPoint(patch, "fastPassEntrance", locations.FastPassEntrance, value => locations.FastPassEntrance = value, change, "attractionLocations.fastPassEntrance");
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchLocationPoint(patch, "reducedMobilityEntrance", locations.ReducedMobilityEntrance, value => locations.ReducedMobilityEntrance = value, change, "attractionLocations.reducedMobilityEntrance");
    }

    internal static void PatchFounder(ParkFounder entity, JsonElement patch, ParkGraphUpsertChange change)
    {
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "name", entity.Name, value => entity.Name = value ?? string.Empty, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "occupation", entity.Occupation, value => entity.Occupation = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "birthDate", entity.BirthDate, value => entity.BirthDate = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "deathDate", entity.DeathDate, value => entity.DeathDate = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "birthPlace", entity.BirthPlace, value => entity.BirthPlace = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "nationalityCountryCode", entity.NationalityCountryCode, value => entity.NationalityCountryCode = value?.ToUpperInvariant(), change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "websiteUrl", entity.WebsiteUrl, value => entity.WebsiteUrl = value, change);
        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "biography"))
        {
            entity.Biography = ParkGraphUpsertProcessorLocalizedTextExtensions.PatchLocalizedTexts(entity.Biography, ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "biography"), false, change, "biography");
        }
    }

    internal static void PatchOperator(ParkOperator entity, JsonElement patch, ParkGraphUpsertChange change)
    {
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "name", entity.Name, value => entity.Name = value ?? string.Empty, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "legalName", entity.LegalName, value => entity.LegalName = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchIntNullable(patch, "foundedYear", entity.FoundedYear, value => entity.FoundedYear = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchIntNullable(patch, "closedYear", entity.ClosedYear, value => entity.ClosedYear = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchEnum(patch, "adminReviewStatus", entity.AdminReviewStatus, value => entity.AdminReviewStatus = value, change);
        ParkGraphUpsertProcessorPatchingExtensions.PatchContactDetails(patch, entity.ContactDetails, value => entity.ContactDetails = value, change);
        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "description"))
        {
            entity.Description = ParkGraphUpsertProcessorLocalizedTextExtensions.PatchLocalizedTexts(entity.Description, ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "description"), false, change, "description");
        }
    }

    internal static void PatchManufacturer(AttractionManufacturer entity, JsonElement patch, ParkGraphUpsertChange change)
    {
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "name", entity.Name, value => entity.Name = value ?? string.Empty, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "legalName", entity.LegalName, value => entity.LegalName = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchIntNullable(patch, "foundedYear", entity.FoundedYear, value => entity.FoundedYear = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchIntNullable(patch, "closedYear", entity.ClosedYear, value => entity.ClosedYear = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchString(patch, "currentLogoImageId", entity.CurrentLogoImageId, value => entity.CurrentLogoImageId = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchBool(patch, "isVisible", entity.IsVisible, value => entity.IsVisible = value, change);
        ParkGraphUpsertProcessorPrimitivePatchingExtensions.PatchEnum(patch, "adminReviewStatus", entity.AdminReviewStatus, value => entity.AdminReviewStatus = value, change);
        ParkGraphUpsertProcessorPatchingExtensions.PatchContactDetails(patch, entity.ContactDetails, value => entity.ContactDetails = value, change);
        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "biography"))
        {
            entity.Biography = ParkGraphUpsertProcessorLocalizedTextExtensions.PatchLocalizedTexts(entity.Biography, ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(patch, "biography"), false, change, "biography");
        }
    }

    internal static void PatchContactDetails(JsonElement patch, ParkReferenceContactDetails? current, Action<ParkReferenceContactDetails?> assign, ParkGraphUpsertChange change)
    {
        if (!ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "contactDetails"))
        {
            return;
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasNull(patch, "contactDetails"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "contactDetails", ParkGraphUpsertProcessorPatchingExtensions.DescribeContactDetails(current), null);
            assign(null);
            return;
        }

        JsonElement? contactPatch = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(patch, "contactDetails");
        if (contactPatch is null)
        {
            return;
        }

        ParkReferenceContactDetails next = current is null ? new ParkReferenceContactDetails() : new ParkReferenceContactDetails
        {
            WebsiteUrl = current.WebsiteUrl,
            Email = current.Email,
            PhoneNumber = current.PhoneNumber,
            Street = current.Street,
            City = current.City,
            PostalCode = current.PostalCode,
            CountryCode = current.CountryCode,
            Latitude = current.Latitude,
            Longitude = current.Longitude,
        };
        string? before = ParkGraphUpsertProcessorPatchingExtensions.DescribeContactDetails(current);
        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(contactPatch, "websiteUrl"))
        {
            next.WebsiteUrl = ParkGraphUpsertProcessorJsonReadingExtensions.ReadStringAllowNull(contactPatch, "websiteUrl")?.Trim();
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(contactPatch, "email"))
        {
            next.Email = ParkGraphUpsertProcessorJsonReadingExtensions.ReadStringAllowNull(contactPatch, "email")?.Trim();
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(contactPatch, "phoneNumber"))
        {
            next.PhoneNumber = ParkGraphUpsertProcessorJsonReadingExtensions.ReadStringAllowNull(contactPatch, "phoneNumber")?.Trim();
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(contactPatch, "street"))
        {
            next.Street = ParkGraphUpsertProcessorJsonReadingExtensions.ReadStringAllowNull(contactPatch, "street")?.Trim();
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(contactPatch, "city"))
        {
            next.City = ParkGraphUpsertProcessorJsonReadingExtensions.ReadStringAllowNull(contactPatch, "city")?.Trim();
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(contactPatch, "postalCode"))
        {
            next.PostalCode = ParkGraphUpsertProcessorJsonReadingExtensions.ReadStringAllowNull(contactPatch, "postalCode")?.Trim();
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(contactPatch, "countryCode"))
        {
            next.CountryCode = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(contactPatch, "countryCode")?.ToUpperInvariant();
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(contactPatch, "latitude"))
        {
            next.Latitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(contactPatch, "latitude");
        }

        if (ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(contactPatch, "longitude"))
        {
            next.Longitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(contactPatch, "longitude");
        }

        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "contactDetails", before, ParkGraphUpsertProcessorPatchingExtensions.DescribeContactDetails(next));
        assign(next);
    }

    internal static string? DescribeContactDetails(ParkReferenceContactDetails? contactDetails)
    {
        if (contactDetails is null)
        {
            return null;
        }

        return string.Join(" | ", new[] { contactDetails.WebsiteUrl, contactDetails.Email, contactDetails.PhoneNumber, contactDetails.Street, contactDetails.City, contactDetails.PostalCode, contactDetails.CountryCode, contactDetails.Latitude?.ToString(CultureInfo.InvariantCulture), contactDetails.Longitude?.ToString(CultureInfo.InvariantCulture), }.Where(static value => !string.IsNullOrWhiteSpace(value)));
    }

    internal static string DescribeAccessConditions(IReadOnlyCollection<AttractionAccessCondition> conditions)
    {
        return string.Join(" || ", conditions.Select(static condition => ParkGraphUpsertProcessorPatchingExtensions.DescribeAccessCondition(condition)));
    }

    internal static string DescribeAccessCondition(AttractionAccessCondition condition)
    {
        List<string> parts = new List<string>
        {
            condition.Type.ToString(),
            condition.TypeKey ?? string.Empty,
            ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(condition.IsCustom) ?? string.Empty,
            condition.CustomTypeKey ?? string.Empty,
            ParkGraphUpsertProcessorPatchingExtensions.DescribeLocalizedTextsForDiff(condition.CustomTypeLabel),
            ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(condition.Value) ?? string.Empty,
            condition.Unit?.ToString() ?? string.Empty,
            ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(condition.RequiresAccompaniment) ?? string.Empty,
            ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(condition.MinimumCompanionAge) ?? string.Empty,
            ParkGraphUpsertProcessorPatchingExtensions.DescribeLocalizedTextsForDiff(condition.Label),
            ParkGraphUpsertProcessorPatchingExtensions.DescribeLocalizedTextsForDiff(condition.Description),
            ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(condition.DisplayOrder) ?? string.Empty,
        };
        return string.Join("|", parts);
    }

    internal static string DescribeLocalizedTextsForDiff(IReadOnlyCollection<LocalizedText> texts)
    {
        Dictionary<string, string> values = ParkGraphUpsertProcessorLocalizedTextExtensions.ToLocalizedTextMap(texts);
        return string.Join(", ", values.OrderBy(static value => value.Key, StringComparer.OrdinalIgnoreCase).Select(static value => $"{value.Key}:{value.Value}"));
    }

    internal static void ApplyOptionalPositionPatch(AmusementPark.Core.Geo.GeolocatedEntityBase entity, JsonElement patch, ParkGraphUpsertChange change)
    {
        bool hasLatitude = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "latitude");
        bool hasLongitude = ParkGraphUpsertProcessorJsonReadingExtensions.HasProperty(patch, "longitude");
        if (!hasLatitude && !hasLongitude)
        {
            return;
        }

        double? latitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(patch, "latitude") ?? entity.Position?.Latitude;
        double? longitude = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(patch, "longitude") ?? entity.Position?.Longitude;
        if (latitude.HasValue && longitude.HasValue)
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "position", ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(entity.Position), $"{latitude.Value.ToString(CultureInfo.InvariantCulture)},{longitude.Value.ToString(CultureInfo.InvariantCulture)}");
            entity.SetPosition(latitude.Value, longitude.Value);
        }
        else if (ParkGraphUpsertProcessorJsonReadingExtensions.HasNull(patch, "latitude") || ParkGraphUpsertProcessorJsonReadingExtensions.HasNull(patch, "longitude"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "position", ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(entity.Position), null);
            entity.ClearPosition();
        }
    }
}
