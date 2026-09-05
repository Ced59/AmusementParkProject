using System.Globalization;
using System.Text;
using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Services;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private static Park BuildNewParkFromPatch(JsonElement? parkPatch, JsonElement? identity, ParkGraphUpsertResult result)
    {
        Park park = new Park
        {
            Name = ReadString(parkPatch, "name") ?? ReadString(identity, "name"),
            CountryCode = ReadString(parkPatch, "countryCode") ?? ReadString(identity, "countryCode"),
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        double? latitude = ReadDouble(parkPatch, "latitude");
        double? longitude = ReadDouble(parkPatch, "longitude");
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
    private static void PatchPark(Park park, JsonElement? patch, JsonElement? identity, Dictionary<string, string> founderKeys, Dictionary<string, string> operatorKeys, ParkGraphUpsertChange change, ParkGraphUpsertResult result, bool isNew)
    {
        if (patch is null)
        {
            return;
        }

        PatchString(patch, "name", park.Name, value => park.Name = value, change);
        PatchString(patch, "countryCode", park.CountryCode, value => park.CountryCode = value?.ToUpperInvariant(), change);
        PatchEnumNullable(patch, "type", park.Type, value => park.Type = value, change, "type");
        PatchEnumNullable(patch, "audienceClassification", park.AudienceClassification, value => park.AudienceClassification = value, change, "audienceClassification");
        PatchParkStatus(patch, park, change);
        PatchLifecycleDate(
            patch,
            "openingDate",
            "openingDateText",
            park.OpeningDate,
            park.OpeningDateText,
            value => park.OpeningDate = value,
            value => park.OpeningDateText = value,
            change,
            "openingDate",
            "openingDateText");
        PatchLifecycleDate(
            patch,
            "closingDate",
            "closingDateText",
            park.ClosingDate,
            park.ClosingDateText,
            value => park.ClosingDate = value,
            value => park.ClosingDateText = value,
            change,
            "closingDate",
            "closingDateText");
        PatchString(patch, "founderId", park.FounderId, value => park.FounderId = value, change);
        PatchString(patch, "operatorId", park.OperatorId, value => park.OperatorId = value, change);
        PatchString(patch, "websiteUrl", park.WebsiteUrl, value => park.WebsiteUrl = value, change);
        PatchString(patch, "street", park.Street, value => park.Street = value, change);
        PatchString(patch, "city", park.City, value => park.City = value, change);
        PatchString(patch, "postalCode", park.PostalCode, value => park.PostalCode = value, change);
        PatchBool(patch, "isVisible", park.IsVisible, value => park.IsVisible = value, change);
        PatchBool(patch, "isFeaturedOnHome", park.IsFeaturedOnHome, value => park.IsFeaturedOnHome = value, change);
        PatchBool(patch, "isFeaturedOnHomeSponsored", park.IsFeaturedOnHomeSponsored, value => park.IsFeaturedOnHomeSponsored = value, change);
        PatchIntNullable(patch, "featuredHomeOrder", park.FeaturedHomeOrder, value => park.FeaturedHomeOrder = value, change);
        PatchEnum(patch, "adminReviewStatus", park.AdminReviewStatus, value => park.AdminReviewStatus = value, change);

        string? founderKey = ReadString(patch, "founderKey");
        if (!string.IsNullOrWhiteSpace(founderKey) && founderKeys.TryGetValue(founderKey, out string? founderId))
        {
            AddChange(change, "founderId", park.FounderId, founderId);
            park.FounderId = founderId;
        }

        string? operatorKey = ReadString(patch, "operatorKey");
        if (!string.IsNullOrWhiteSpace(operatorKey) && operatorKeys.TryGetValue(operatorKey, out string? operatorId))
        {
            AddChange(change, "operatorId", park.OperatorId, operatorId);
            park.OperatorId = operatorId;
        }

        if (HasProperty(patch, "descriptions"))
        {
            park.Descriptions = PatchLocalizedTexts(park.Descriptions, GetArray(patch, "descriptions"), false, change, "descriptions");
        }

        PatchOfficialMaps(park, patch, result);

        bool hasLatitude = HasProperty(patch, "latitude");
        bool hasLongitude = HasProperty(patch, "longitude");
        if (hasLatitude || hasLongitude)
        {
            double? latitude = ReadDouble(patch, "latitude") ?? park.Position?.Latitude;
            double? longitude = ReadDouble(patch, "longitude") ?? park.Position?.Longitude;
            if (latitude.HasValue && longitude.HasValue)
            {
                AddChange(change, "position", FormatPosition(park.Position), $"{latitude.Value.ToString(CultureInfo.InvariantCulture)},{longitude.Value.ToString(CultureInfo.InvariantCulture)}");
                park.SetPosition(latitude.Value, longitude.Value);
            }
            else if (isNew)
            {
                result.Warnings.Add("Le parc créé n'a pas de coordonnées complètes.");
            }
        }
    }

    private static void PatchParkStatus(JsonElement? patch, Park park, ParkGraphUpsertChange change)
    {
        if (!HasProperty(patch, "status"))
        {
            return;
        }

        ParkStatus? next = ReadParkStatus(patch, "status");
        if (!next.HasValue)
        {
            return;
        }

        AddChange(change, "status", park.Status, next.Value);
        park.Status = next.Value;
    }

    private static ParkStatus? ReadParkStatus(JsonElement? element, string propertyName)
    {
        string? value = ReadString(element, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (TryReadEnum(value, out ParkStatus parsed))
        {
            return parsed;
        }

        string normalized = NormalizeStatusToken(value);
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

    private static string NormalizeStatusToken(string value)
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

    private static void PatchZone(ParkZone zone, JsonElement patch, ParkGraphUpsertChange change)
    {
        PatchString(patch, "name", zone.Name, value => zone.Name = value ?? string.Empty, change);
        PatchString(patch, "slug", zone.Slug, value => zone.Slug = value, change);
        PatchBool(patch, "isVisible", zone.IsVisible, value => zone.IsVisible = value, change);
        PatchInt(patch, "sortOrder", zone.SortOrder, value => zone.SortOrder = value, change);

        if (HasProperty(patch, "names"))
        {
            zone.Names = PatchLocalizedTexts(zone.Names, GetArray(patch, "names"), false, change, "names");
        }

        if (HasProperty(patch, "descriptions"))
        {
            zone.Descriptions = PatchLocalizedTexts(zone.Descriptions, GetArray(patch, "descriptions"), false, change, "descriptions");
        }

        ApplyOptionalPositionPatch(zone, patch, change);
    }
    private void PatchItem(ParkItem item, JsonElement patch, Dictionary<string, string> zoneKeys, Dictionary<string, string> manufacturerKeys, Dictionary<string, string> manufacturerIdRemaps, ParkGraphUpsertChange change, ParkGraphUpsertResult result, bool isNew)
    {
        PatchString(patch, "name", item.Name, value => item.Name = value ?? string.Empty, change);
        PatchString(patch, "subtype", item.Subtype, value => item.Subtype = value, change);
        PatchEnum(patch, "category", item.Category, value => item.Category = value, change);
        PatchEnum(patch, "type", item.Type, value => item.Type = value, change);
        PatchBool(patch, "isVisible", item.IsVisible, value => item.IsVisible = value, change);
        PatchEnum(patch, "adminReviewStatus", item.AdminReviewStatus, value => item.AdminReviewStatus = value, change);

        string? zoneId = ReadString(patch, "zoneId");
        string? zoneKey = ReadString(patch, "zoneKey");
        if (!string.IsNullOrWhiteSpace(zoneKey) && zoneKeys.TryGetValue(zoneKey, out string? resolvedZoneId))
        {
            zoneId = resolvedZoneId;
        }
        else if (!string.IsNullOrWhiteSpace(zoneKey))
        {
            string normalizedZoneNameKey = $"zone:{NormalizeKey(zoneKey)}";
            if (zoneKeys.TryGetValue(normalizedZoneNameKey, out string? resolvedByName))
            {
                zoneId = resolvedByName;
            }
            else
            {
                result.Warnings.Add($"ZoneKey '{zoneKey}' non résolue pour l'élément '{item.Name}'.");
            }
        }

        if (HasProperty(patch, "zoneId") || HasProperty(patch, "zoneKey"))
        {
            AddChange(change, "zoneId", item.ZoneId, zoneId);
            item.ZoneId = zoneId;
        }

        if (HasProperty(patch, "descriptions"))
        {
            item.Descriptions = PatchLocalizedTexts(item.Descriptions, GetArray(patch, "descriptions"), false, change, "descriptions");
        }

        ApplyOptionalPositionPatch(item, patch, change);

        if (HasProperty(patch, "attractionDetails"))
        {
            JsonElement? detailsPatch = GetObject(patch, "attractionDetails");
            item.AttractionDetails ??= new AttractionDetails();
            this.PatchAttractionDetails(item.AttractionDetails, detailsPatch, manufacturerKeys, manufacturerIdRemaps, change, result, item.Name);
        }
        else if (isNew && item.Category == ParkItemCategory.Attraction)
        {
            item.AttractionDetails ??= new AttractionDetails();
        }

        if (HasProperty(patch, "attractionLocations"))
        {
            item.AttractionLocations ??= new AttractionLocations();
            PatchAttractionLocations(item.AttractionLocations, GetObject(patch, "attractionLocations"), change);
        }
    }
    private void PatchAttractionDetails(AttractionDetails details, JsonElement? patch, Dictionary<string, string> manufacturerKeys, Dictionary<string, string> manufacturerIdRemaps, ParkGraphUpsertChange change, ParkGraphUpsertResult result, string itemName)
    {
        if (patch is null)
        {
            return;
        }

        if (HasProperty(patch, "manufacturerId"))
        {
            string? requestedManufacturerId = RemapId(manufacturerIdRemaps, ReadStringAllowNull(patch, "manufacturerId")?.Trim());
            if (string.IsNullOrWhiteSpace(requestedManufacturerId))
            {
                requestedManufacturerId = null;
            }

            AddChange(change, "attractionDetails.manufacturerId", details.ManufacturerId, requestedManufacturerId);
            details.ManufacturerId = requestedManufacturerId;
        }

        string? manufacturerKey = ReadString(patch, "manufacturerKey");
        if (!string.IsNullOrWhiteSpace(manufacturerKey) && manufacturerKeys.TryGetValue(manufacturerKey, out string? manufacturerId))
        {
            manufacturerId = RemapId(manufacturerIdRemaps, manufacturerId);
            AddChange(change, "attractionDetails.manufacturerId", details.ManufacturerId, manufacturerId);
            details.ManufacturerId = manufacturerId;
        }
        else if (!string.IsNullOrWhiteSpace(manufacturerKey))
        {
            result.Warnings.Add($"ManufacturerKey '{manufacturerKey}' non résolue pour '{itemName}'.");
        }

        PatchString(patch, "model", details.Model, value => details.Model = value, change, "attractionDetails.model");
        PatchString(patch, "externalSource", details.ExternalSource, value => details.ExternalSource = value, change, "attractionDetails.externalSource");
        PatchString(patch, "externalId", details.ExternalId, value => details.ExternalId = value, change, "attractionDetails.externalId");
        PatchString(patch, "sourceUrl", details.SourceUrl, value => details.SourceUrl = value, change, "attractionDetails.sourceUrl");
        PatchString(patch, "status", details.Status, value => details.Status = ParkItemStatusNormalizer.Normalize(value), change, "attractionDetails.status");
        PatchString(patch, "materialType", details.MaterialType, value => details.MaterialType = value, change, "attractionDetails.materialType");
        PatchString(patch, "seatingType", details.SeatingType, value => details.SeatingType = value, change, "attractionDetails.seatingType");
        PatchString(patch, "launchType", details.LaunchType, value => details.LaunchType = value, change, "attractionDetails.launchType");
        PatchString(patch, "restraintType", details.RestraintType, value => details.RestraintType = value, change, "attractionDetails.restraintType");
        PatchBoolNullable(patch, "isLaunched", details.IsLaunched, value => details.IsLaunched = value, change, "attractionDetails.isLaunched");
        PatchLifecycleDate(
            patch,
            "openingDate",
            "openingDateText",
            details.OpeningDate,
            details.OpeningDateText,
            value => details.OpeningDate = value,
            value => details.OpeningDateText = value,
            change,
            "attractionDetails.openingDate",
            "attractionDetails.openingDateText");
        PatchLifecycleDate(
            patch,
            "closingDate",
            "closingDateText",
            details.ClosingDate,
            details.ClosingDateText,
            value => details.ClosingDate = value,
            value => details.ClosingDateText = value,
            change,
            "attractionDetails.closingDate",
            "attractionDetails.closingDateText");
        PatchIntNullable(patch, "durationInSeconds", details.DurationInSeconds, value => details.DurationInSeconds = value, change, "attractionDetails.durationInSeconds");
        PatchIntNullable(patch, "capacityPerHour", details.CapacityPerHour, value => details.CapacityPerHour = value, change, "attractionDetails.capacityPerHour");
        PatchDoubleNullable(patch, "heightInFeet", details.HeightInFeet, value => details.HeightInFeet = value, change, "attractionDetails.heightInFeet");
        PatchDoubleNullable(patch, "heightInMeters", details.HeightInMeters, value => details.HeightInMeters = value, change, "attractionDetails.heightInMeters");
        PatchDoubleNullable(patch, "lengthInFeet", details.LengthInFeet, value => details.LengthInFeet = value, change, "attractionDetails.lengthInFeet");
        PatchDoubleNullable(patch, "lengthInMeters", details.LengthInMeters, value => details.LengthInMeters = value, change, "attractionDetails.lengthInMeters");
        PatchDoubleNullable(patch, "speedInMph", details.SpeedInMph, value => details.SpeedInMph = value, change, "attractionDetails.speedInMph");
        PatchDoubleNullable(patch, "speedInKmH", details.SpeedInKmH, value => details.SpeedInKmH = value, change, "attractionDetails.speedInKmH");
        PatchDoubleNullable(patch, "dropInFeet", details.DropInFeet, value => details.DropInFeet = value, change, "attractionDetails.dropInFeet");
        PatchDoubleNullable(patch, "dropInMeters", details.DropInMeters, value => details.DropInMeters = value, change, "attractionDetails.dropInMeters");
        PatchIntNullable(patch, "inversionCount", details.InversionCount, value => details.InversionCount = value, change, "attractionDetails.inversionCount");
        PatchIntNullable(patch, "trainCount", details.TrainCount, value => details.TrainCount = value, change, "attractionDetails.trainCount");
        PatchIntNullable(patch, "carsPerTrain", details.CarsPerTrain, value => details.CarsPerTrain = value, change, "attractionDetails.carsPerTrain");
        PatchIntNullable(patch, "ridersPerVehicle", details.RidersPerVehicle, value => details.RidersPerVehicle = value, change, "attractionDetails.ridersPerVehicle");
        PatchBoolNullable(patch, "hasSingleRider", details.HasSingleRider, value => details.HasSingleRider = value, change, "attractionDetails.hasSingleRider");
        PatchBoolNullable(patch, "hasFastPass", details.HasFastPass, value => details.HasFastPass = value, change, "attractionDetails.hasFastPass");
        PatchBoolNullable(patch, "isAccessibleForReducedMobility", details.IsAccessibleForReducedMobility, value => details.IsAccessibleForReducedMobility = value, change, "attractionDetails.isAccessibleForReducedMobility");
        PatchBoolNullable(patch, "isIndoor", details.IsIndoor, value => details.IsIndoor = value, change, "attractionDetails.isIndoor");
        PatchEnumNullable(patch, "waterExposureLevel", details.WaterExposureLevel, value => details.WaterExposureLevel = value, change, "attractionDetails.waterExposureLevel");

        if (HasProperty(patch, "accessConditions"))
        {
            List<AttractionAccessCondition> conditions = ReadAccessConditions(GetArray(patch, "accessConditions"));
            foreach (AttractionAccessCondition condition in conditions)
            {
                this.measurementConversionService.NormalizeAccessCondition(condition);
            }

            AddChange(change, "attractionDetails.accessConditions", DescribeAccessConditions(details.AccessConditions), DescribeAccessConditions(conditions));
            details.AccessConditions = conditions;
        }

        NormalizeAttractionDetailsAfterPatch(details, change, patch.Value);
    }

    private void NormalizeAttractionDetailsAfterPatch(AttractionDetails details, ParkGraphUpsertChange change, JsonElement patch)
    {
        double? currentHeightInFeet = details.HeightInFeet;
        double? currentHeightInMeters = details.HeightInMeters;
        double? currentLengthInFeet = details.LengthInFeet;
        double? currentLengthInMeters = details.LengthInMeters;
        double? currentSpeedInMph = details.SpeedInMph;
        double? currentSpeedInKmH = details.SpeedInKmH;
        double? currentDropInFeet = details.DropInFeet;
        double? currentDropInMeters = details.DropInMeters;
        string currentAccessConditions = DescribeAccessConditions(details.AccessConditions);

        PreferPatchedImperialMeasurements(details, patch);
        this.measurementConversionService.NormalizeAttractionDetails(details);

        AddChange(change, "attractionDetails.heightInFeet", currentHeightInFeet, details.HeightInFeet);
        AddChange(change, "attractionDetails.heightInMeters", currentHeightInMeters, details.HeightInMeters);
        AddChange(change, "attractionDetails.lengthInFeet", currentLengthInFeet, details.LengthInFeet);
        AddChange(change, "attractionDetails.lengthInMeters", currentLengthInMeters, details.LengthInMeters);
        AddChange(change, "attractionDetails.speedInMph", currentSpeedInMph, details.SpeedInMph);
        AddChange(change, "attractionDetails.speedInKmH", currentSpeedInKmH, details.SpeedInKmH);
        AddChange(change, "attractionDetails.dropInFeet", currentDropInFeet, details.DropInFeet);
        AddChange(change, "attractionDetails.dropInMeters", currentDropInMeters, details.DropInMeters);
        AddChange(change, "attractionDetails.accessConditions", currentAccessConditions, DescribeAccessConditions(details.AccessConditions));
    }

    private static void PreferPatchedImperialMeasurements(AttractionDetails details, JsonElement patch)
    {
        if (ShouldPreferPatchedImperialMeasurement(patch, "heightInFeet", "heightInMeters", details.HeightInFeet))
        {
            details.HeightInMeters = null;
        }

        if (ShouldPreferPatchedImperialMeasurement(patch, "lengthInFeet", "lengthInMeters", details.LengthInFeet))
        {
            details.LengthInMeters = null;
        }

        if (ShouldPreferPatchedImperialMeasurement(patch, "speedInMph", "speedInKmH", details.SpeedInMph))
        {
            details.SpeedInKmH = null;
        }

        if (ShouldPreferPatchedImperialMeasurement(patch, "dropInFeet", "dropInMeters", details.DropInFeet))
        {
            details.DropInMeters = null;
        }
    }

    private static bool ShouldPreferPatchedImperialMeasurement(JsonElement patch, string imperialPropertyName, string metricPropertyName, double? imperialValue)
    {
        return imperialValue.HasValue && HasProperty(patch, imperialPropertyName) && !HasProperty(patch, metricPropertyName);
    }
    private static void PatchAttractionLocations(AttractionLocations locations, JsonElement? patch, ParkGraphUpsertChange change)
    {
        if (patch is null)
        {
            return;
        }

        PatchLocationPoint(patch, "entrance", locations.Entrance, value => locations.Entrance = value, change, "attractionLocations.entrance");
        PatchLocationPoint(patch, "exit", locations.Exit, value => locations.Exit = value, change, "attractionLocations.exit");
        PatchLocationPoint(patch, "fastPassEntrance", locations.FastPassEntrance, value => locations.FastPassEntrance = value, change, "attractionLocations.fastPassEntrance");
        PatchLocationPoint(patch, "reducedMobilityEntrance", locations.ReducedMobilityEntrance, value => locations.ReducedMobilityEntrance = value, change, "attractionLocations.reducedMobilityEntrance");
    }
    private static void PatchFounder(ParkFounder entity, JsonElement patch, ParkGraphUpsertChange change)
    {
        PatchString(patch, "name", entity.Name, value => entity.Name = value ?? string.Empty, change);
        PatchString(patch, "occupation", entity.Occupation, value => entity.Occupation = value, change);
        PatchString(patch, "birthDate", entity.BirthDate, value => entity.BirthDate = value, change);
        PatchString(patch, "deathDate", entity.DeathDate, value => entity.DeathDate = value, change);
        PatchString(patch, "birthPlace", entity.BirthPlace, value => entity.BirthPlace = value, change);
        PatchString(patch, "nationalityCountryCode", entity.NationalityCountryCode, value => entity.NationalityCountryCode = value?.ToUpperInvariant(), change);
        PatchString(patch, "websiteUrl", entity.WebsiteUrl, value => entity.WebsiteUrl = value, change);
        if (HasProperty(patch, "biography"))
        {
            entity.Biography = PatchLocalizedTexts(entity.Biography, GetArray(patch, "biography"), false, change, "biography");
        }
    }
    private static void PatchOperator(ParkOperator entity, JsonElement patch, ParkGraphUpsertChange change)
    {
        PatchString(patch, "name", entity.Name, value => entity.Name = value ?? string.Empty, change);
        PatchString(patch, "legalName", entity.LegalName, value => entity.LegalName = value, change);
        PatchIntNullable(patch, "foundedYear", entity.FoundedYear, value => entity.FoundedYear = value, change);
        PatchIntNullable(patch, "closedYear", entity.ClosedYear, value => entity.ClosedYear = value, change);
        PatchEnum(patch, "adminReviewStatus", entity.AdminReviewStatus, value => entity.AdminReviewStatus = value, change);
        PatchContactDetails(patch, entity.ContactDetails, value => entity.ContactDetails = value, change);
        if (HasProperty(patch, "description"))
        {
            entity.Description = PatchLocalizedTexts(entity.Description, GetArray(patch, "description"), false, change, "description");
        }
    }
    private static void PatchManufacturer(AttractionManufacturer entity, JsonElement patch, ParkGraphUpsertChange change)
    {
        PatchString(patch, "name", entity.Name, value => entity.Name = value ?? string.Empty, change);
        PatchString(patch, "legalName", entity.LegalName, value => entity.LegalName = value, change);
        PatchIntNullable(patch, "foundedYear", entity.FoundedYear, value => entity.FoundedYear = value, change);
        PatchIntNullable(patch, "closedYear", entity.ClosedYear, value => entity.ClosedYear = value, change);
        PatchString(patch, "currentLogoImageId", entity.CurrentLogoImageId, value => entity.CurrentLogoImageId = value, change);
        PatchBool(patch, "isVisible", entity.IsVisible, value => entity.IsVisible = value, change);
        PatchEnum(patch, "adminReviewStatus", entity.AdminReviewStatus, value => entity.AdminReviewStatus = value, change);
        PatchContactDetails(patch, entity.ContactDetails, value => entity.ContactDetails = value, change);
        if (HasProperty(patch, "biography"))
        {
            entity.Biography = PatchLocalizedTexts(entity.Biography, GetArray(patch, "biography"), false, change, "biography");
        }
    }
    private static void PatchContactDetails(JsonElement patch, ParkReferenceContactDetails? current, Action<ParkReferenceContactDetails?> assign, ParkGraphUpsertChange change)
    {
        if (!HasProperty(patch, "contactDetails"))
        {
            return;
        }

        if (HasNull(patch, "contactDetails"))
        {
            AddChange(change, "contactDetails", DescribeContactDetails(current), null);
            assign(null);
            return;
        }

        JsonElement? contactPatch = GetObject(patch, "contactDetails");
        if (contactPatch is null)
        {
            return;
        }

        ParkReferenceContactDetails next = current is null
            ? new ParkReferenceContactDetails()
            : new ParkReferenceContactDetails
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

        string? before = DescribeContactDetails(current);
        if (HasProperty(contactPatch, "websiteUrl"))
        {
            next.WebsiteUrl = ReadStringAllowNull(contactPatch, "websiteUrl")?.Trim();
        }

        if (HasProperty(contactPatch, "email"))
        {
            next.Email = ReadStringAllowNull(contactPatch, "email")?.Trim();
        }

        if (HasProperty(contactPatch, "phoneNumber"))
        {
            next.PhoneNumber = ReadStringAllowNull(contactPatch, "phoneNumber")?.Trim();
        }

        if (HasProperty(contactPatch, "street"))
        {
            next.Street = ReadStringAllowNull(contactPatch, "street")?.Trim();
        }

        if (HasProperty(contactPatch, "city"))
        {
            next.City = ReadStringAllowNull(contactPatch, "city")?.Trim();
        }

        if (HasProperty(contactPatch, "postalCode"))
        {
            next.PostalCode = ReadStringAllowNull(contactPatch, "postalCode")?.Trim();
        }

        if (HasProperty(contactPatch, "countryCode"))
        {
            next.CountryCode = ReadString(contactPatch, "countryCode")?.ToUpperInvariant();
        }

        if (HasProperty(contactPatch, "latitude"))
        {
            next.Latitude = ReadDouble(contactPatch, "latitude");
        }

        if (HasProperty(contactPatch, "longitude"))
        {
            next.Longitude = ReadDouble(contactPatch, "longitude");
        }

        AddChange(change, "contactDetails", before, DescribeContactDetails(next));
        assign(next);
    }
    private static string? DescribeContactDetails(ParkReferenceContactDetails? contactDetails)
    {
        if (contactDetails is null)
        {
            return null;
        }

        return string.Join(" | ", new[]
        {
            contactDetails.WebsiteUrl,
            contactDetails.Email,
            contactDetails.PhoneNumber,
            contactDetails.Street,
            contactDetails.City,
            contactDetails.PostalCode,
            contactDetails.CountryCode,
            contactDetails.Latitude?.ToString(CultureInfo.InvariantCulture),
            contactDetails.Longitude?.ToString(CultureInfo.InvariantCulture),
        }.Where(static value => !string.IsNullOrWhiteSpace(value)));
    }
    private static string DescribeAccessConditions(IReadOnlyCollection<AttractionAccessCondition> conditions)
    {
        return string.Join(" || ", conditions.Select(static condition => DescribeAccessCondition(condition)));
    }
    private static string DescribeAccessCondition(AttractionAccessCondition condition)
    {
        List<string> parts = new List<string>
        {
            condition.Type.ToString(),
            condition.TypeKey ?? string.Empty,
            FormatValue(condition.IsCustom) ?? string.Empty,
            condition.CustomTypeKey ?? string.Empty,
            DescribeLocalizedTextsForDiff(condition.CustomTypeLabel),
            FormatValue(condition.Value) ?? string.Empty,
            condition.Unit?.ToString() ?? string.Empty,
            FormatValue(condition.RequiresAccompaniment) ?? string.Empty,
            FormatValue(condition.MinimumCompanionAge) ?? string.Empty,
            DescribeLocalizedTextsForDiff(condition.Label),
            DescribeLocalizedTextsForDiff(condition.Description),
            FormatValue(condition.DisplayOrder) ?? string.Empty,
        };

        return string.Join("|", parts);
    }
    private static string DescribeLocalizedTextsForDiff(IReadOnlyCollection<LocalizedText> texts)
    {
        Dictionary<string, string> values = ToLocalizedTextMap(texts);
        return string.Join(", ", values
            .OrderBy(static value => value.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static value => $"{value.Key}:{value.Value}"));
    }
    private static void ApplyOptionalPositionPatch(AmusementPark.Core.Geo.GeolocatedEntityBase entity, JsonElement patch, ParkGraphUpsertChange change)
    {
        bool hasLatitude = HasProperty(patch, "latitude");
        bool hasLongitude = HasProperty(patch, "longitude");
        if (!hasLatitude && !hasLongitude)
        {
            return;
        }

        double? latitude = ReadDouble(patch, "latitude") ?? entity.Position?.Latitude;
        double? longitude = ReadDouble(patch, "longitude") ?? entity.Position?.Longitude;
        if (latitude.HasValue && longitude.HasValue)
        {
            AddChange(change, "position", FormatPosition(entity.Position), $"{latitude.Value.ToString(CultureInfo.InvariantCulture)},{longitude.Value.ToString(CultureInfo.InvariantCulture)}");
            entity.SetPosition(latitude.Value, longitude.Value);
        }
        else if (HasNull(patch, "latitude") || HasNull(patch, "longitude"))
        {
            AddChange(change, "position", FormatPosition(entity.Position), null);
            entity.ClearPosition();
        }
    }

    private static void PatchOfficialMaps(Park park, JsonElement? parkPatch, ParkGraphUpsertResult result)
    {
        JsonElement? patches = GetArray(parkPatch, "officialMaps");
        if (patches is null)
        {
            return;
        }

        List<ParkOfficialMap> officialMaps = park.OfficialMaps
            .Select(static officialMap => CloneOfficialMap(officialMap))
            .ToList();
        HashSet<string> processedIdentities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement patch in patches.Value.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                result.Errors.Add("Chaque entrée park.officialMaps doit être un objet JSON.");
                continue;
            }

            string? id = ReadString(patch, "id");
            string? key = ReadString(patch, "key");
            int? requestedYear = ReadInt(patch, "year");
            string? requestedLanguageValue = ReadString(patch, "languageCode");
            string? requestedLanguageCode = NormalizeLanguageCode(requestedLanguageValue);
            ParkOfficialMapFormat? requestedFormat = ReadEnumNullable<ParkOfficialMapFormat>(patch, "format");
            ParkOfficialMap? existing = FindOfficialMap(officialMaps, id, key, requestedYear, requestedLanguageCode, requestedFormat);
            bool isNew = existing is null;
            ParkOfficialMap candidate = existing is null
                ? new ParkOfficialMap
                {
                    Id = id ?? key ?? Guid.NewGuid().ToString("N"),
                    LanguageCode = requestedLanguageCode,
                    IsVisible = false,
                }
                : CloneOfficialMap(existing);
            string matchMode = ResolveOfficialMapMatchMode(existing, id, key);
            ParkGraphUpsertChange change = BuildEntityChange(
                "ParkOfficialMap",
                candidate.Id,
                key,
                BuildOfficialMapDisplayName(candidate, requestedYear),
                isNew ? "Created" : "Unchanged",
                isNew ? "year+languageCode+format" : matchMode);

            PatchInt(patch, "year", candidate.Year, value => candidate.Year = value, change);
            PatchEnum(patch, "format", candidate.Format, value => candidate.Format = value, change);
            PatchString(patch, "documentUrl", candidate.DocumentUrl, value => candidate.DocumentUrl = value, change);
            PatchString(patch, "storageKey", candidate.StorageKey, value => candidate.StorageKey = value, change);
            PatchString(patch, "originalFileName", candidate.OriginalFileName, value => candidate.OriginalFileName = value, change);
            PatchString(patch, "contentType", candidate.ContentType, value => candidate.ContentType = value, change);
            PatchIntNullable(
                patch,
                "sizeInBytes",
                candidate.SizeInBytes is >= 0 and <= int.MaxValue ? (int?)candidate.SizeInBytes.Value : null,
                value => candidate.SizeInBytes = value,
                change);
            PatchString(patch, "previewImageUrl", candidate.PreviewImageUrl, value => candidate.PreviewImageUrl = value, change);
            PatchString(patch, "sourcePageUrl", candidate.SourcePageUrl, value => candidate.SourcePageUrl = value, change);
            PatchString(patch, "languageCode", candidate.LanguageCode, value => candidate.LanguageCode = NormalizeLanguageCode(value), change);
            PatchBool(patch, "isVisible", candidate.IsVisible, value => candidate.IsVisible = value, change);
            PatchDateNullable(patch, "lastVerifiedAtUtc", candidate.LastVerifiedAtUtc, value => candidate.LastVerifiedAtUtc = value, change, "lastVerifiedAtUtc");

            if (HasProperty(patch, "titles"))
            {
                candidate.Titles = PatchLocalizedTexts(candidate.Titles, GetArray(patch, "titles"), false, change, "titles");
            }

            if (HasProperty(patch, "alternativeTexts"))
            {
                candidate.AlternativeTexts = PatchLocalizedTexts(candidate.AlternativeTexts, GetArray(patch, "alternativeTexts"), false, change, "alternativeTexts");
            }

            List<string> validationErrors = ValidateOfficialMap(park.Id, candidate);
            if (isNew && !requestedFormat.HasValue)
            {
                validationErrors.Add($"format est obligatoire pour la nouvelle carte officielle '{candidate.Id}'.");
            }
            if (!string.IsNullOrWhiteSpace(requestedLanguageValue) && requestedLanguageCode is null)
            {
                validationErrors.Add($"languageCode dépasse 16 caractères pour la carte officielle '{candidate.Id}'.");
            }

            string identity = BuildOfficialMapIdentity(candidate);
            if (!processedIdentities.Add(identity))
            {
                validationErrors.Add($"La carte officielle '{identity}' est définie plusieurs fois dans le même lot.");
            }

            ParkOfficialMap? duplicate = officialMaps.FirstOrDefault(officialMap => !ReferenceEquals(officialMap, existing)
                && !string.Equals(officialMap.Id, existing?.Id, StringComparison.OrdinalIgnoreCase)
                && string.Equals(BuildOfficialMapIdentity(officialMap), identity, StringComparison.OrdinalIgnoreCase));
            if (duplicate is not null)
            {
                validationErrors.Add($"Une carte officielle existe déjà pour {candidate.Year}, la langue '{candidate.LanguageCode ?? "non précisée"}' et le format '{candidate.Format}'.");
            }

            if (validationErrors.Count > 0)
            {
                foreach (string validationError in validationErrors)
                {
                    result.Errors.Add(validationError);
                }

                continue;
            }

            change.DisplayName = BuildOfficialMapDisplayName(candidate, null);
            change.ChangeType = isNew ? "Created" : change.Fields.Count > 0 ? "Updated" : "Unchanged";
            result.Changes.Add(change);

            if (isNew)
            {
                officialMaps.Add(candidate);
                continue;
            }

            int existingIndex = officialMaps.FindIndex(officialMap => string.Equals(officialMap.Id, existing!.Id, StringComparison.OrdinalIgnoreCase));
            officialMaps[existingIndex] = candidate;
        }

        park.OfficialMaps = officialMaps
            .OrderByDescending(static officialMap => officialMap.Year)
            .ThenBy(static officialMap => officialMap.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static officialMap => officialMap.Format)
            .ThenBy(static officialMap => officialMap.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static ParkOfficialMap? FindOfficialMap(
        IReadOnlyCollection<ParkOfficialMap> officialMaps,
        string? id,
        string? key,
        int? year,
        string? languageCode,
        ParkOfficialMapFormat? format)
    {
        string? requestedId = id ?? key;
        if (!string.IsNullOrWhiteSpace(requestedId))
        {
            ParkOfficialMap? byId = officialMaps.FirstOrDefault(officialMap => string.Equals(officialMap.Id, requestedId, StringComparison.OrdinalIgnoreCase));
            if (byId is not null)
            {
                return byId;
            }
        }

        if (!year.HasValue || !format.HasValue)
        {
            return null;
        }

        return officialMaps.FirstOrDefault(officialMap => officialMap.Year == year.Value
            && officialMap.Format == format.Value
            && string.Equals(NormalizeLanguageCode(officialMap.LanguageCode), languageCode, StringComparison.OrdinalIgnoreCase));
    }

    private static List<string> ValidateOfficialMap(string parkId, ParkOfficialMap officialMap)
    {
        List<string> errors = new List<string>();
        if (!IsStableOfficialMapIdentifier(officialMap.Id))
        {
            errors.Add("L'identifiant d'une carte officielle doit contenir entre 1 et 80 caractères alphanumériques, tirets ou underscores.");
        }

        int maximumYear = DateTime.UtcNow.Year + 1;
        if (officialMap.Year < 1800 || officialMap.Year > maximumYear)
        {
            errors.Add($"L'année '{officialMap.Year.ToString(CultureInfo.InvariantCulture)}' d'une carte officielle doit être comprise entre 1800 et {maximumYear.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (!Enum.IsDefined(officialMap.Format))
        {
            errors.Add($"Le format de la carte officielle {officialMap.Year.ToString(CultureInfo.InvariantCulture)} est invalide.");
        }

        bool hasExternalDocument = IsAbsoluteHttpUrl(officialMap.DocumentUrl);
        bool hasStoredDocument = !string.IsNullOrWhiteSpace(officialMap.StorageKey);
        if (!hasExternalDocument && !hasStoredDocument)
        {
            errors.Add($"documentUrl ou storageKey est obligatoire pour la carte officielle {officialMap.Year.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (!string.IsNullOrWhiteSpace(officialMap.DocumentUrl) && !hasExternalDocument)
        {
            errors.Add($"documentUrl doit être une URL HTTP(S) absolue pour la carte officielle {officialMap.Year.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (hasStoredDocument)
        {
            if (!ParkOfficialMapStorageKeys.BelongsTo(officialMap.StorageKey!, parkId, officialMap.Id))
            {
                errors.Add($"storageKey ne correspond pas au parc et à la carte officielle '{officialMap.Id}'.");
            }

            if (string.IsNullOrWhiteSpace(officialMap.OriginalFileName)
                || string.IsNullOrWhiteSpace(officialMap.ContentType)
                || !officialMap.SizeInBytes.HasValue
                || officialMap.SizeInBytes.Value <= 0)
            {
                errors.Add($"originalFileName, contentType et sizeInBytes sont obligatoires avec storageKey pour la carte officielle '{officialMap.Id}'.");
            }

            if (!string.IsNullOrWhiteSpace(officialMap.OriginalFileName) && officialMap.OriginalFileName.Length > 180)
            {
                errors.Add($"originalFileName dépasse 180 caractères pour la carte officielle '{officialMap.Id}'.");
            }

            if (!string.IsNullOrWhiteSpace(officialMap.ContentType)
                && !IsSupportedOfficialMapContentType(officialMap.Format, officialMap.ContentType))
            {
                errors.Add($"contentType ne correspond pas au format de la carte officielle '{officialMap.Id}'.");
            }

            if (officialMap.SizeInBytes > 25 * 1024 * 1024)
            {
                errors.Add($"sizeInBytes dépasse la limite de 25 Mo pour la carte officielle '{officialMap.Id}'.");
            }
        }

        if (!string.IsNullOrWhiteSpace(officialMap.PreviewImageUrl) && !IsAbsoluteHttpUrl(officialMap.PreviewImageUrl))
        {
            errors.Add($"previewImageUrl doit être une URL HTTP(S) absolue pour la carte officielle {officialMap.Year.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (!string.IsNullOrWhiteSpace(officialMap.SourcePageUrl) && !IsAbsoluteHttpUrl(officialMap.SourcePageUrl))
        {
            errors.Add($"sourcePageUrl doit être une URL HTTP(S) absolue pour la carte officielle {officialMap.Year.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (officialMap.IsVisible
            && officialMap.Format == ParkOfficialMapFormat.Image
            && !officialMap.AlternativeTexts.Any(static text => !string.IsNullOrWhiteSpace(text.LanguageCode) && !string.IsNullOrWhiteSpace(text.Value)))
        {
            errors.Add($"Une carte officielle image visible doit fournir au moins un texte alternatif localisé ({officialMap.Year.ToString(CultureInfo.InvariantCulture)}).");
        }

        return errors;
    }

    private static bool IsStableOfficialMapIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 80 || !IsAsciiLetterOrDigit(value[0]))
        {
            return false;
        }

        return value.All(static character => IsAsciiLetterOrDigit(character) || character is '-' or '_');
    }

    private static bool IsAsciiLetterOrDigit(char character)
    {
        return character is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '0' and <= '9';
    }

    private static bool IsSupportedOfficialMapContentType(ParkOfficialMapFormat format, string contentType)
    {
        string normalized = contentType.Trim().ToLowerInvariant();
        return format switch
        {
            ParkOfficialMapFormat.Image => normalized is "image/jpeg" or "image/png" or "image/webp" or "image/gif",
            ParkOfficialMapFormat.Pdf => normalized == "application/pdf",
            ParkOfficialMapFormat.Other => normalized is "application/vnd.google-earth.kml+xml"
                or "application/vnd.google-earth.kmz"
                or "application/zip",
            _ => false,
        };
    }

    private static bool IsAbsoluteHttpUrl(string? value)
    {
        return Uri.TryCreate(value?.Trim(), UriKind.Absolute, out Uri? uri)
            && (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildOfficialMapIdentity(ParkOfficialMap officialMap)
    {
        return $"{officialMap.Year.ToString(CultureInfo.InvariantCulture)}:{NormalizeLanguageCode(officialMap.LanguageCode) ?? "und"}:{officialMap.Format}";
    }

    private static string BuildOfficialMapDisplayName(ParkOfficialMap officialMap, int? requestedYear)
    {
        int year = requestedYear ?? officialMap.Year;
        string language = NormalizeLanguageCode(officialMap.LanguageCode) ?? "und";
        return $"Carte officielle {year.ToString(CultureInfo.InvariantCulture)} ({language}, {officialMap.Format})";
    }

    private static string DescribeOfficialMaps(IReadOnlyCollection<ParkOfficialMap> officialMaps)
    {
        return string.Join(", ", officialMaps
            .OrderByDescending(static officialMap => officialMap.Year)
            .ThenBy(static officialMap => officialMap.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static officialMap => officialMap.Format)
            .Select(static officialMap => BuildOfficialMapIdentity(officialMap)));
    }

    private static string ResolveOfficialMapMatchMode(ParkOfficialMap? existing, string? id, string? key)
    {
        if (existing is null)
        {
            return "none";
        }

        if (!string.IsNullOrWhiteSpace(id))
        {
            return "id";
        }

        if (!string.IsNullOrWhiteSpace(key) && string.Equals(existing.Id, key, StringComparison.OrdinalIgnoreCase))
        {
            return "key";
        }

        return "year+languageCode+format";
    }

    private static string? NormalizeLanguageCode(string? value)
    {
        string? normalized = NormalizeString(value)?.ToLowerInvariant();
        return normalized is not null && normalized.Length <= 16 ? normalized : null;
    }
}
