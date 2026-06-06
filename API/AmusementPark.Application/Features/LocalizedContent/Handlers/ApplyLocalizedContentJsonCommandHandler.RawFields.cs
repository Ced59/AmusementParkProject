using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.LocalizedContent.Commands;
using AmusementPark.Application.Features.LocalizedContent.Results;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.LocalizedContent.Handlers;

public sealed partial class ApplyLocalizedContentJsonCommandHandler
{
    private static ApplicationResult ApplyParkRawFields(Park park, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        double? latitude = null;
        double? longitude = null;
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is "name")
            {
                ApplyString(field.Value, value => park.Name = value, updatedFields, "name");
            }
            else if (normalizedField is "countrycode" or "country")
            {
                ApplyString(field.Value, value => park.CountryCode = value?.ToUpperInvariant(), updatedFields, "countryCode");
            }
            else if (normalizedField is "type" or "parktype")
            {
                ApplyEnum<ParkType>(field.Value, value => park.Type = value, updatedFields, "type");
            }
            else if (normalizedField is "founderid")
            {
                ApplyString(field.Value, value => park.FounderId = value, updatedFields, "founderId");
            }
            else if (normalizedField is "operatorid")
            {
                ApplyString(field.Value, value => park.OperatorId = value, updatedFields, "operatorId");
            }
            else if (normalizedField is "websiteurl" or "website")
            {
                ApplyString(field.Value, value => park.WebsiteUrl = value, updatedFields, "websiteUrl");
            }
            else if (normalizedField is "street")
            {
                ApplyString(field.Value, value => park.Street = value, updatedFields, "street");
            }
            else if (normalizedField is "city")
            {
                ApplyString(field.Value, value => park.City = value, updatedFields, "city");
            }
            else if (normalizedField is "postalcode" or "zipcode")
            {
                ApplyString(field.Value, value => park.PostalCode = value, updatedFields, "postalCode");
            }
            else if (normalizedField is "isvisible" or "visible")
            {
                ApplyBool(field.Value, value => park.IsVisible = value, updatedFields, "isVisible");
            }
            else if (normalizedField is "adminreviewstatus" or "reviewstatus")
            {
                ApplyEnum<AdminReviewStatus>(field.Value, value => park.AdminReviewStatus = value, updatedFields, "adminReviewStatus");
            }
            else if (normalizedField is "isfeaturedonhome")
            {
                ApplyBool(field.Value, value => park.IsFeaturedOnHome = value, updatedFields, "isFeaturedOnHome");
            }
            else if (normalizedField is "featuredhomeorder")
            {
                ApplyInt(field.Value, value => park.FeaturedHomeOrder = value, updatedFields, "featuredHomeOrder");
            }
            else if (normalizedField is "isfeaturedonhomesponsored")
            {
                ApplyBool(field.Value, value => park.IsFeaturedOnHomeSponsored = value, updatedFields, "isFeaturedOnHomeSponsored");
            }
            else if (normalizedField is "latitude" or "lat")
            {
                latitude = ReadDouble(field.Value);
            }
            else if (normalizedField is "longitude" or "lng" or "lon")
            {
                longitude = ReadDouble(field.Value);
            }
            else if (normalizedField is "position" or "location" or "coordinates")
            {
                (double? readLatitude, double? readLongitude) = ReadPosition(field.Value);
                latitude = readLatitude ?? latitude;
                longitude = readLongitude ?? longitude;
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.Park, field.Key));
            }
        }

        ApplyPosition(park, latitude, longitude, updatedFields);
        return ApplicationResult.Success();
    }
    private static ApplicationResult ApplyParkZoneRawFields(ParkZone zone, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        double? latitude = null;
        double? longitude = null;
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is "parkid")
            {
                ApplyString(field.Value, value => zone.ParkId = value ?? zone.ParkId, updatedFields, "parkId");
            }
            else if (normalizedField is "name")
            {
                ApplyString(field.Value, value => zone.Name = value ?? zone.Name, updatedFields, "name");
            }
            else if (normalizedField is "slug")
            {
                ApplyString(field.Value, value => zone.Slug = value, updatedFields, "slug");
            }
            else if (normalizedField is "isvisible" or "visible")
            {
                ApplyBool(field.Value, value => zone.IsVisible = value, updatedFields, "isVisible");
            }
            else if (normalizedField is "sortorder" or "displayorder" or "order")
            {
                ApplyInt(field.Value, value => zone.SortOrder = value ?? zone.SortOrder, updatedFields, "sortOrder");
            }
            else if (normalizedField is "latitude" or "lat")
            {
                latitude = ReadDouble(field.Value);
            }
            else if (normalizedField is "longitude" or "lng" or "lon")
            {
                longitude = ReadDouble(field.Value);
            }
            else if (normalizedField is "position" or "location" or "coordinates")
            {
                (double? readLatitude, double? readLongitude) = ReadPosition(field.Value);
                latitude = readLatitude ?? latitude;
                longitude = readLongitude ?? longitude;
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.ParkZone, field.Key));
            }
        }

        ApplyPosition(zone, latitude, longitude, updatedFields);
        return ApplicationResult.Success();
    }
    private static ApplicationResult ApplyParkItemRawFields(ParkItem item, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        double? latitude = null;
        double? longitude = null;
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is "parkid")
            {
                ApplyString(field.Value, value => item.ParkId = value ?? item.ParkId, updatedFields, "parkId");
            }
            else if (normalizedField is "zoneid")
            {
                ApplyString(field.Value, value => item.ZoneId = value, updatedFields, "zoneId");
            }
            else if (normalizedField is "name")
            {
                ApplyString(field.Value, value => item.Name = value ?? item.Name, updatedFields, "name");
            }
            else if (normalizedField is "category")
            {
                ApplyEnum<ParkItemCategory>(field.Value, value => item.Category = value, updatedFields, "category");
            }
            else if (normalizedField is "type" or "itemtype")
            {
                ApplyEnum<ParkItemType>(field.Value, value => item.Type = value, updatedFields, "type");
            }
            else if (normalizedField is "subtype")
            {
                ApplyString(field.Value, value => item.Subtype = value, updatedFields, "subtype");
            }
            else if (normalizedField is "isvisible" or "visible")
            {
                ApplyBool(field.Value, value => item.IsVisible = value, updatedFields, "isVisible");
            }
            else if (normalizedField is "adminreviewstatus" or "reviewstatus")
            {
                ApplyEnum<AdminReviewStatus>(field.Value, value => item.AdminReviewStatus = value, updatedFields, "adminReviewStatus");
            }
            else if (normalizedField is "latitude" or "lat")
            {
                latitude = ReadDouble(field.Value);
            }
            else if (normalizedField is "longitude" or "lng" or "lon")
            {
                longitude = ReadDouble(field.Value);
            }
            else if (normalizedField is "position" or "location" or "coordinates")
            {
                (double? readLatitude, double? readLongitude) = ReadPosition(field.Value);
                latitude = readLatitude ?? latitude;
                longitude = readLongitude ?? longitude;
            }
            else if (normalizedField is "attractiondetails" or "details")
            {
                item.AttractionDetails ??= new AttractionDetails();
                ApplyAttractionDetailsRawFields(item.AttractionDetails, field.Value, updatedFields);
            }
            else if (TryApplyAttractionDetailsRawField(item, field, updatedFields))
            {
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.ParkItem, field.Key));
            }
        }

        ApplyPosition(item, latitude, longitude, updatedFields);
        return ApplicationResult.Success();
    }
    private static ApplicationResult ApplyParkOperatorRawFields(ParkOperator entity, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is "name")
            {
                ApplyString(field.Value, value => entity.Name = value ?? entity.Name, updatedFields, "name");
            }
            else if (normalizedField is "legalname")
            {
                ApplyString(field.Value, value => entity.LegalName = value, updatedFields, "legalName");
            }
            else if (normalizedField is "foundedyear")
            {
                ApplyInt(field.Value, value => entity.FoundedYear = value, updatedFields, "foundedYear");
            }
            else if (normalizedField is "closedyear")
            {
                ApplyInt(field.Value, value => entity.ClosedYear = value, updatedFields, "closedYear");
            }
            else if (normalizedField is "contactdetails" or "contact")
            {
                entity.ContactDetails = ReadContactDetails(field.Value, entity.ContactDetails);
                updatedFields.Add("contactDetails");
            }
            else if (normalizedField is "adminreviewstatus" or "reviewstatus")
            {
                ApplyEnum<AdminReviewStatus>(field.Value, value => entity.AdminReviewStatus = value, updatedFields, "adminReviewStatus");
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.ParkOperator, field.Key));
            }
        }

        return ApplicationResult.Success();
    }
    private static ApplicationResult ApplyParkFounderRawFields(ParkFounder entity, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is "name")
            {
                ApplyString(field.Value, value => entity.Name = value ?? entity.Name, updatedFields, "name");
            }
            else if (normalizedField is "occupation")
            {
                ApplyString(field.Value, value => entity.Occupation = value, updatedFields, "occupation");
            }
            else if (normalizedField is "birthdate")
            {
                ApplyString(field.Value, value => entity.BirthDate = value, updatedFields, "birthDate");
            }
            else if (normalizedField is "deathdate")
            {
                ApplyString(field.Value, value => entity.DeathDate = value, updatedFields, "deathDate");
            }
            else if (normalizedField is "birthplace")
            {
                ApplyString(field.Value, value => entity.BirthPlace = value, updatedFields, "birthPlace");
            }
            else if (normalizedField is "nationalitycountrycode" or "countrycode" or "nationality")
            {
                ApplyString(field.Value, value => entity.NationalityCountryCode = value?.ToUpperInvariant(), updatedFields, "nationalityCountryCode");
            }
            else if (normalizedField is "websiteurl" or "website")
            {
                ApplyString(field.Value, value => entity.WebsiteUrl = value, updatedFields, "websiteUrl");
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.ParkFounder, field.Key));
            }
        }

        return ApplicationResult.Success();
    }
    private static ApplicationResult ApplyAttractionManufacturerRawFields(AttractionManufacturer entity, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is "name")
            {
                ApplyString(field.Value, value => entity.Name = value ?? entity.Name, updatedFields, "name");
            }
            else if (normalizedField is "legalname")
            {
                ApplyString(field.Value, value => entity.LegalName = value, updatedFields, "legalName");
            }
            else if (normalizedField is "foundedyear")
            {
                ApplyInt(field.Value, value => entity.FoundedYear = value, updatedFields, "foundedYear");
            }
            else if (normalizedField is "closedyear")
            {
                ApplyInt(field.Value, value => entity.ClosedYear = value, updatedFields, "closedYear");
            }
            else if (normalizedField is "contactdetails" or "contact")
            {
                entity.ContactDetails = ReadContactDetails(field.Value, entity.ContactDetails);
                updatedFields.Add("contactDetails");
            }
            else if (normalizedField is "adminreviewstatus" or "reviewstatus")
            {
                ApplyEnum<AdminReviewStatus>(field.Value, value => entity.AdminReviewStatus = value, updatedFields, "adminReviewStatus");
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.AttractionManufacturer, field.Key));
            }
        }

        return ApplicationResult.Success();
    }
    private static ApplicationResult ApplyImageTagRawFields(ImageTag tag, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = NormalizeField(field.Key);
            if (normalizedField is "slug" or "key")
            {
                ApplyString(field.Value, value => tag.Slug = value ?? tag.Slug, updatedFields, "slug");
            }
            else if (normalizedField is "isactive" or "active")
            {
                ApplyBool(field.Value, value => tag.IsActive = value, updatedFields, "isActive");
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.ImageTag, field.Key));
            }
        }

        return ApplicationResult.Success();
    }
    private static bool TryApplyAttractionDetailsRawField(ParkItem item, KeyValuePair<string, JsonElement> field, List<string> updatedFields)
    {
        string normalizedField = NormalizeField(field.Key);
        if (!IsAttractionDetailsField(normalizedField))
        {
            return false;
        }

        item.AttractionDetails ??= new AttractionDetails();
        ApplyAttractionDetailsField(item.AttractionDetails, normalizedField, field.Value, updatedFields, $"attractionDetails.{field.Key}");
        return true;
    }
    private static void ApplyAttractionDetailsRawFields(AttractionDetails details, JsonElement value, List<string> updatedFields)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            string normalizedField = NormalizeField(property.Name);
            if (normalizedField is "accessconditions" or "attractionaccessconditions")
            {
                continue;
            }

            ApplyAttractionDetailsField(details, normalizedField, property.Value, updatedFields, $"attractionDetails.{property.Name}");
        }
    }
    private static bool IsAttractionDetailsField(string normalizedField)
    {
        return normalizedField is "manufacturerid" or "model" or "externalsource" or "externalid" or "sourceurl" or "status" or
            "materialtype" or "seatingtype" or "launchtype" or "restrainttype" or "islaunched" or "openingdate" or "closingdate" or
            "openingdatetext" or "closingdatetext" or "durationinseconds" or "duration" or "capacityperhour" or "heightinfeet" or
            "heightinmeters" or "height" or "lengthinfeet" or "lengthinmeters" or "length" or "speedinmph" or "speedinkmh" or "speed" or
            "dropinmeters" or "drop" or "inversioncount" or "inversions" or "traincount" or "carspertrain" or "riderspervehicle" or
            "hassinglerider" or "hasfastpass" or "isaccessibleforreducedmobility" or "isindoor" or "waterexposurelevel";
    }
    private static void ApplyAttractionDetailsField(AttractionDetails details, string normalizedField, JsonElement value, List<string> updatedFields, string fieldName)
    {
        switch (normalizedField)
        {
            case "manufacturerid": ApplyString(value, v => details.ManufacturerId = v, updatedFields, fieldName); break;
            case "model": ApplyString(value, v => details.Model = v, updatedFields, fieldName); break;
            case "externalsource": ApplyString(value, v => details.ExternalSource = v, updatedFields, fieldName); break;
            case "externalid": ApplyString(value, v => details.ExternalId = v, updatedFields, fieldName); break;
            case "sourceurl": ApplyString(value, v => details.SourceUrl = v, updatedFields, fieldName); break;
            case "status": ApplyString(value, v => details.Status = v, updatedFields, fieldName); break;
            case "materialtype": ApplyString(value, v => details.MaterialType = v, updatedFields, fieldName); break;
            case "seatingtype": ApplyString(value, v => details.SeatingType = v, updatedFields, fieldName); break;
            case "launchtype": ApplyString(value, v => details.LaunchType = v, updatedFields, fieldName); break;
            case "restrainttype": ApplyString(value, v => details.RestraintType = v, updatedFields, fieldName); break;
            case "islaunched": ApplyBool(value, v => details.IsLaunched = v, updatedFields, fieldName); break;
            case "openingdate": ApplyDate(value, v => details.OpeningDate = v, updatedFields, fieldName); break;
            case "closingdate": ApplyDate(value, v => details.ClosingDate = v, updatedFields, fieldName); break;
            case "openingdatetext": ApplyString(value, v => details.OpeningDateText = v, updatedFields, fieldName); break;
            case "closingdatetext": ApplyString(value, v => details.ClosingDateText = v, updatedFields, fieldName); break;
            case "duration":
            case "durationinseconds": ApplyInt(value, v => details.DurationInSeconds = v, updatedFields, fieldName); break;
            case "capacityperhour": ApplyInt(value, v => details.CapacityPerHour = v, updatedFields, fieldName); break;
            case "heightinfeet": ApplyDouble(value, v => details.HeightInFeet = v, updatedFields, fieldName); break;
            case "height":
            case "heightinmeters": ApplyDouble(value, v => details.HeightInMeters = v, updatedFields, fieldName); break;
            case "lengthinfeet": ApplyDouble(value, v => details.LengthInFeet = v, updatedFields, fieldName); break;
            case "length":
            case "lengthinmeters": ApplyDouble(value, v => details.LengthInMeters = v, updatedFields, fieldName); break;
            case "speedinmph": ApplyDouble(value, v => details.SpeedInMph = v, updatedFields, fieldName); break;
            case "speed":
            case "speedinkmh": ApplyDouble(value, v => details.SpeedInKmH = v, updatedFields, fieldName); break;
            case "drop":
            case "dropinmeters": ApplyDouble(value, v => details.DropInMeters = v, updatedFields, fieldName); break;
            case "inversions":
            case "inversioncount": ApplyInt(value, v => details.InversionCount = v, updatedFields, fieldName); break;
            case "traincount": ApplyInt(value, v => details.TrainCount = v, updatedFields, fieldName); break;
            case "carspertrain": ApplyInt(value, v => details.CarsPerTrain = v, updatedFields, fieldName); break;
            case "riderspervehicle": ApplyInt(value, v => details.RidersPerVehicle = v, updatedFields, fieldName); break;
            case "hassinglerider": ApplyBool(value, v => details.HasSingleRider = v, updatedFields, fieldName); break;
            case "hasfastpass": ApplyBool(value, v => details.HasFastPass = v, updatedFields, fieldName); break;
            case "isaccessibleforreducedmobility": ApplyBool(value, v => details.IsAccessibleForReducedMobility = v, updatedFields, fieldName); break;
            case "isindoor": ApplyBool(value, v => details.IsIndoor = v, updatedFields, fieldName); break;
            case "waterexposurelevel": ApplyEnum<AttractionWaterExposureLevel>(value, v => details.WaterExposureLevel = v, updatedFields, fieldName); break;
        }
    }
    private static ParkReferenceContactDetails? ReadContactDetails(JsonElement value, ParkReferenceContactDetails? existing)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return existing;
        }

        ParkReferenceContactDetails details = existing is null
            ? new ParkReferenceContactDetails()
            : new ParkReferenceContactDetails
            {
                WebsiteUrl = existing.WebsiteUrl,
                Email = existing.Email,
                PhoneNumber = existing.PhoneNumber,
                Street = existing.Street,
                City = existing.City,
                PostalCode = existing.PostalCode,
                CountryCode = existing.CountryCode,
                Latitude = existing.Latitude,
                Longitude = existing.Longitude,
            };

        foreach (JsonProperty property in value.EnumerateObject())
        {
            string normalizedField = NormalizeField(property.Name);
            switch (normalizedField)
            {
                case "websiteurl":
                case "website": details.WebsiteUrl = NormalizeOptionalText(ReadString(property.Value)); break;
                case "email": details.Email = NormalizeOptionalText(ReadString(property.Value)); break;
                case "phonenumber":
                case "phone": details.PhoneNumber = NormalizeOptionalText(ReadString(property.Value)); break;
                case "street": details.Street = NormalizeOptionalText(ReadString(property.Value)); break;
                case "city": details.City = NormalizeOptionalText(ReadString(property.Value)); break;
                case "postalcode":
                case "zipcode": details.PostalCode = NormalizeOptionalText(ReadString(property.Value)); break;
                case "countrycode":
                case "country": details.CountryCode = NormalizeOptionalText(ReadString(property.Value))?.ToUpperInvariant(); break;
                case "latitude":
                case "lat": details.Latitude = ReadDouble(property.Value); break;
                case "longitude":
                case "lng":
                case "lon": details.Longitude = ReadDouble(property.Value); break;
            }
        }

        bool hasValue = !string.IsNullOrWhiteSpace(details.WebsiteUrl) ||
                        !string.IsNullOrWhiteSpace(details.Email) ||
                        !string.IsNullOrWhiteSpace(details.PhoneNumber) ||
                        !string.IsNullOrWhiteSpace(details.Street) ||
                        !string.IsNullOrWhiteSpace(details.City) ||
                        !string.IsNullOrWhiteSpace(details.PostalCode) ||
                        !string.IsNullOrWhiteSpace(details.CountryCode) ||
                        details.Latitude.HasValue ||
                        details.Longitude.HasValue;

        return hasValue ? details : null;
    }
}
