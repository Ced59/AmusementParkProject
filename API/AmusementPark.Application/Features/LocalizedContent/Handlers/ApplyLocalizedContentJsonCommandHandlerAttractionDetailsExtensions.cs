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
using AmusementPark.Application.Common.Measurements;

namespace AmusementPark.Application.Features.LocalizedContent.Handlers;
internal static class ApplyLocalizedContentJsonCommandHandlerAttractionDetailsExtensions
{
    internal static bool TryApplyAttractionDetailsRawField(ParkItem item, KeyValuePair<string, JsonElement> field, List<string> updatedFields)
    {
        string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
        if (!ApplyLocalizedContentJsonCommandHandlerAttractionDetailsExtensions.IsAttractionDetailsField(normalizedField))
        {
            return false;
        }

        item.AttractionDetails ??= new AttractionDetails();
        ApplyLocalizedContentJsonCommandHandlerAttractionDetailsExtensions.ApplyAttractionDetailsField(item.AttractionDetails, normalizedField, field.Value, updatedFields, $"attractionDetails.{field.Key}");
        return true;
    }

    internal static void ApplyAttractionDetailsRawFields(AttractionDetails details, JsonElement value, List<string> updatedFields)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(property.Name);
            if (normalizedField is "accessconditions" or "attractionaccessconditions")
            {
                continue;
            }

            ApplyLocalizedContentJsonCommandHandlerAttractionDetailsExtensions.ApplyAttractionDetailsField(details, normalizedField, property.Value, updatedFields, $"attractionDetails.{property.Name}");
        }
    }

    internal static bool IsAttractionDetailsField(string normalizedField)
    {
        return normalizedField is "manufacturerid" or "model" or "externalsource" or "externalid" or "sourceurl" or "status" or "materialtype" or "seatingtype" or "launchtype" or "restrainttype" or "islaunched" or "openingdate" or "closingdate" or "openingdatetext" or "closingdatetext" or "durationinseconds" or "duration" or "capacityperhour" or "heightinfeet" or "heightinmeters" or "height" or "lengthinfeet" or "lengthinmeters" or "length" or "speedinmph" or "speedinkmh" or "speed" or "dropinfeet" or "dropinmeters" or "drop" or "inversioncount" or "inversions" or "traincount" or "carspertrain" or "riderspervehicle" or "hassinglerider" or "hasfastpass" or "isaccessibleforreducedmobility" or "isindoor" or "waterexposurelevel";
    }

    internal static bool ShouldNormalizeAttractionDetails(LocalizedContentPatch patch)
    {
        if (patch.AccessConditions.Count > 0)
        {
            return true;
        }

        return patch.RawFields.Keys.Any(static key =>
        {
            string normalizedKey = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(key);
            return normalizedKey is "attractiondetails" or "details" || ApplyLocalizedContentJsonCommandHandlerAttractionDetailsExtensions.IsAttractionDetailsField(normalizedKey);
        });
    }

    internal static void ApplyAttractionDetailsField(AttractionDetails details, string normalizedField, JsonElement value, List<string> updatedFields, string fieldName)
    {
        switch (normalizedField)
        {
            case "manufacturerid":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(value, v => details.ManufacturerId = v, updatedFields, fieldName);
                break;
            case "model":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(value, v => details.Model = v, updatedFields, fieldName);
                break;
            case "externalsource":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(value, v => details.ExternalSource = v, updatedFields, fieldName);
                break;
            case "externalid":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(value, v => details.ExternalId = v, updatedFields, fieldName);
                break;
            case "sourceurl":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(value, v => details.SourceUrl = v, updatedFields, fieldName);
                break;
            case "status":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(value, v => details.Status = v, updatedFields, fieldName);
                break;
            case "materialtype":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(value, v => details.MaterialType = v, updatedFields, fieldName);
                break;
            case "seatingtype":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(value, v => details.SeatingType = v, updatedFields, fieldName);
                break;
            case "launchtype":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(value, v => details.LaunchType = v, updatedFields, fieldName);
                break;
            case "restrainttype":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(value, v => details.RestraintType = v, updatedFields, fieldName);
                break;
            case "islaunched":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyBool(value, v => details.IsLaunched = v, updatedFields, fieldName);
                break;
            case "openingdate":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyDate(value, v => details.OpeningDate = v, updatedFields, fieldName);
                break;
            case "closingdate":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyDate(value, v => details.ClosingDate = v, updatedFields, fieldName);
                break;
            case "openingdatetext":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(value, v => details.OpeningDateText = v, updatedFields, fieldName);
                break;
            case "closingdatetext":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(value, v => details.ClosingDateText = v, updatedFields, fieldName);
                break;
            case "duration":
            case "durationinseconds":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyInt(value, v => details.DurationInSeconds = v, updatedFields, fieldName);
                break;
            case "capacityperhour":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyInt(value, v => details.CapacityPerHour = v, updatedFields, fieldName);
                break;
            case "heightinfeet":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyDouble(value, v => details.HeightInFeet = v, updatedFields, fieldName);
                break;
            case "height":
            case "heightinmeters":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyDouble(value, v => details.HeightInMeters = v, updatedFields, fieldName);
                break;
            case "lengthinfeet":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyDouble(value, v => details.LengthInFeet = v, updatedFields, fieldName);
                break;
            case "length":
            case "lengthinmeters":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyDouble(value, v => details.LengthInMeters = v, updatedFields, fieldName);
                break;
            case "speedinmph":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyDouble(value, v => details.SpeedInMph = v, updatedFields, fieldName);
                break;
            case "speed":
            case "speedinkmh":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyDouble(value, v => details.SpeedInKmH = v, updatedFields, fieldName);
                break;
            case "dropinfeet":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyDouble(value, v => details.DropInFeet = v, updatedFields, fieldName);
                break;
            case "drop":
            case "dropinmeters":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyDouble(value, v => details.DropInMeters = v, updatedFields, fieldName);
                break;
            case "inversions":
            case "inversioncount":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyInt(value, v => details.InversionCount = v, updatedFields, fieldName);
                break;
            case "traincount":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyInt(value, v => details.TrainCount = v, updatedFields, fieldName);
                break;
            case "carspertrain":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyInt(value, v => details.CarsPerTrain = v, updatedFields, fieldName);
                break;
            case "riderspervehicle":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyInt(value, v => details.RidersPerVehicle = v, updatedFields, fieldName);
                break;
            case "hassinglerider":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyBool(value, v => details.HasSingleRider = v, updatedFields, fieldName);
                break;
            case "hasfastpass":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyBool(value, v => details.HasFastPass = v, updatedFields, fieldName);
                break;
            case "isaccessibleforreducedmobility":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyBool(value, v => details.IsAccessibleForReducedMobility = v, updatedFields, fieldName);
                break;
            case "isindoor":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyBool(value, v => details.IsIndoor = v, updatedFields, fieldName);
                break;
            case "waterexposurelevel":
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyEnum<AttractionWaterExposureLevel>(value, v => details.WaterExposureLevel = v, updatedFields, fieldName);
                break;
        }
    }

    internal static ParkReferenceContactDetails? ReadContactDetails(JsonElement value, ParkReferenceContactDetails? existing)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return existing;
        }

        ParkReferenceContactDetails details = existing is null ? new ParkReferenceContactDetails() : new ParkReferenceContactDetails
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
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(property.Name);
            switch (normalizedField)
            {
                case "websiteurl":
                case "website":
                    details.WebsiteUrl = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.NormalizeOptionalText(ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadString(property.Value));
                    break;
                case "email":
                    details.Email = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.NormalizeOptionalText(ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadString(property.Value));
                    break;
                case "phonenumber":
                case "phone":
                    details.PhoneNumber = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.NormalizeOptionalText(ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadString(property.Value));
                    break;
                case "street":
                    details.Street = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.NormalizeOptionalText(ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadString(property.Value));
                    break;
                case "city":
                    details.City = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.NormalizeOptionalText(ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadString(property.Value));
                    break;
                case "postalcode":
                case "zipcode":
                    details.PostalCode = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.NormalizeOptionalText(ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadString(property.Value));
                    break;
                case "countrycode":
                case "country":
                    details.CountryCode = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.NormalizeOptionalText(ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadString(property.Value))?.ToUpperInvariant();
                    break;
                case "latitude":
                case "lat":
                    details.Latitude = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadDouble(property.Value);
                    break;
                case "longitude":
                case "lng":
                case "lon":
                    details.Longitude = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadDouble(property.Value);
                    break;
            }
        }

        bool hasValue = !string.IsNullOrWhiteSpace(details.WebsiteUrl) || !string.IsNullOrWhiteSpace(details.Email) || !string.IsNullOrWhiteSpace(details.PhoneNumber) || !string.IsNullOrWhiteSpace(details.Street) || !string.IsNullOrWhiteSpace(details.City) || !string.IsNullOrWhiteSpace(details.PostalCode) || !string.IsNullOrWhiteSpace(details.CountryCode) || details.Latitude.HasValue || details.Longitude.HasValue;
        return hasValue ? details : null;
    }
}
