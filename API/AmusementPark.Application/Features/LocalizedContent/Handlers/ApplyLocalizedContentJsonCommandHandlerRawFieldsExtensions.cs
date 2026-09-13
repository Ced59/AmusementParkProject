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
internal static class ApplyLocalizedContentJsonCommandHandlerRawFieldsExtensions
{
    internal static ApplicationResult ApplyParkRawFields(Park park, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        double? latitude = null;
        double? longitude = null;
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is "name")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => park.Name = value, updatedFields, "name");
            }
            else if (normalizedField is "countrycode" or "country")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => park.CountryCode = value?.ToUpperInvariant(), updatedFields, "countryCode");
            }
            else if (normalizedField is "type" or "parktype")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyEnum<ParkType>(field.Value, value => park.Type = value, updatedFields, "type");
            }
            else if (normalizedField is "founderid")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => park.FounderId = value, updatedFields, "founderId");
            }
            else if (normalizedField is "operatorid")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => park.OperatorId = value, updatedFields, "operatorId");
            }
            else if (normalizedField is "websiteurl" or "website")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => park.WebsiteUrl = value, updatedFields, "websiteUrl");
            }
            else if (normalizedField is "street")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => park.Street = value, updatedFields, "street");
            }
            else if (normalizedField is "city")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => park.City = value, updatedFields, "city");
            }
            else if (normalizedField is "postalcode" or "zipcode")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => park.PostalCode = value, updatedFields, "postalCode");
            }
            else if (normalizedField is "isvisible" or "visible")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyBool(field.Value, value => park.IsVisible = value, updatedFields, "isVisible");
            }
            else if (normalizedField is "adminreviewstatus" or "reviewstatus")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyEnum<AdminReviewStatus>(field.Value, value => park.AdminReviewStatus = value, updatedFields, "adminReviewStatus");
            }
            else if (normalizedField is "isfeaturedonhome")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyBool(field.Value, value => park.IsFeaturedOnHome = value, updatedFields, "isFeaturedOnHome");
            }
            else if (normalizedField is "featuredhomeorder")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyInt(field.Value, value => park.FeaturedHomeOrder = value, updatedFields, "featuredHomeOrder");
            }
            else if (normalizedField is "isfeaturedonhomesponsored")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyBool(field.Value, value => park.IsFeaturedOnHomeSponsored = value, updatedFields, "isFeaturedOnHomeSponsored");
            }
            else if (normalizedField is "latitude" or "lat")
            {
                latitude = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadDouble(field.Value);
            }
            else if (normalizedField is "longitude" or "lng" or "lon")
            {
                longitude = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadDouble(field.Value);
            }
            else if (normalizedField is "position" or "location" or "coordinates")
            {
                (double? readLatitude, double? readLongitude) = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ReadPosition(field.Value);
                latitude = readLatitude ?? latitude;
                longitude = readLongitude ?? longitude;
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.Park, field.Key));
            }
        }

        ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyPosition(park, latitude, longitude, updatedFields);
        return ApplicationResult.Success();
    }

    internal static ApplicationResult ApplyParkZoneRawFields(ParkZone zone, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        double? latitude = null;
        double? longitude = null;
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is "parkid")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => zone.ParkId = value ?? zone.ParkId, updatedFields, "parkId");
            }
            else if (normalizedField is "name")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => zone.Name = value ?? zone.Name, updatedFields, "name");
            }
            else if (normalizedField is "slug")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => zone.Slug = value, updatedFields, "slug");
            }
            else if (normalizedField is "isvisible" or "visible")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyBool(field.Value, value => zone.IsVisible = value, updatedFields, "isVisible");
            }
            else if (normalizedField is "sortorder" or "displayorder" or "order")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyInt(field.Value, value => zone.SortOrder = value ?? zone.SortOrder, updatedFields, "sortOrder");
            }
            else if (normalizedField is "latitude" or "lat")
            {
                latitude = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadDouble(field.Value);
            }
            else if (normalizedField is "longitude" or "lng" or "lon")
            {
                longitude = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadDouble(field.Value);
            }
            else if (normalizedField is "position" or "location" or "coordinates")
            {
                (double? readLatitude, double? readLongitude) = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ReadPosition(field.Value);
                latitude = readLatitude ?? latitude;
                longitude = readLongitude ?? longitude;
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.ParkZone, field.Key));
            }
        }

        ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyPosition(zone, latitude, longitude, updatedFields);
        return ApplicationResult.Success();
    }

    internal static ApplicationResult ApplyParkItemRawFields(ParkItem item, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        double? latitude = null;
        double? longitude = null;
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is "parkid")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => item.ParkId = value ?? item.ParkId, updatedFields, "parkId");
            }
            else if (normalizedField is "zoneid")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => item.ZoneId = value, updatedFields, "zoneId");
            }
            else if (normalizedField is "name")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => item.Name = value ?? item.Name, updatedFields, "name");
            }
            else if (normalizedField is "category")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyEnum<ParkItemCategory>(field.Value, value => item.Category = value, updatedFields, "category");
            }
            else if (normalizedField is "type" or "itemtype")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyEnum<ParkItemType>(field.Value, value => item.Type = value, updatedFields, "type");
            }
            else if (normalizedField is "subtype")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => item.Subtype = value, updatedFields, "subtype");
            }
            else if (normalizedField is "isvisible" or "visible")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyBool(field.Value, value => item.IsVisible = value, updatedFields, "isVisible");
            }
            else if (normalizedField is "adminreviewstatus" or "reviewstatus")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyEnum<AdminReviewStatus>(field.Value, value => item.AdminReviewStatus = value, updatedFields, "adminReviewStatus");
            }
            else if (normalizedField is "latitude" or "lat")
            {
                latitude = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadDouble(field.Value);
            }
            else if (normalizedField is "longitude" or "lng" or "lon")
            {
                longitude = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.ReadDouble(field.Value);
            }
            else if (normalizedField is "position" or "location" or "coordinates")
            {
                (double? readLatitude, double? readLongitude) = ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ReadPosition(field.Value);
                latitude = readLatitude ?? latitude;
                longitude = readLongitude ?? longitude;
            }
            else if (normalizedField is "attractiondetails" or "details")
            {
                item.AttractionDetails ??= new AttractionDetails();
                ApplyLocalizedContentJsonCommandHandlerAttractionDetailsExtensions.ApplyAttractionDetailsRawFields(item.AttractionDetails, field.Value, updatedFields);
            }
            else if (ApplyLocalizedContentJsonCommandHandlerAttractionDetailsExtensions.TryApplyAttractionDetailsRawField(item, field, updatedFields))
            {
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.ParkItem, field.Key));
            }
        }

        ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyPosition(item, latitude, longitude, updatedFields);
        return ApplicationResult.Success();
    }

    internal static ApplicationResult ApplyParkOperatorRawFields(ParkOperator entity, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is "name")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => entity.Name = value ?? entity.Name, updatedFields, "name");
            }
            else if (normalizedField is "legalname")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => entity.LegalName = value, updatedFields, "legalName");
            }
            else if (normalizedField is "foundedyear")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyInt(field.Value, value => entity.FoundedYear = value, updatedFields, "foundedYear");
            }
            else if (normalizedField is "closedyear")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyInt(field.Value, value => entity.ClosedYear = value, updatedFields, "closedYear");
            }
            else if (normalizedField is "contactdetails" or "contact")
            {
                entity.ContactDetails = ApplyLocalizedContentJsonCommandHandlerAttractionDetailsExtensions.ReadContactDetails(field.Value, entity.ContactDetails);
                updatedFields.Add("contactDetails");
            }
            else if (normalizedField is "adminreviewstatus" or "reviewstatus")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyEnum<AdminReviewStatus>(field.Value, value => entity.AdminReviewStatus = value, updatedFields, "adminReviewStatus");
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.ParkOperator, field.Key));
            }
        }

        return ApplicationResult.Success();
    }

    internal static ApplicationResult ApplyParkFounderRawFields(ParkFounder entity, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is "name")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => entity.Name = value ?? entity.Name, updatedFields, "name");
            }
            else if (normalizedField is "occupation")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => entity.Occupation = value, updatedFields, "occupation");
            }
            else if (normalizedField is "birthdate")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => entity.BirthDate = value, updatedFields, "birthDate");
            }
            else if (normalizedField is "deathdate")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => entity.DeathDate = value, updatedFields, "deathDate");
            }
            else if (normalizedField is "birthplace")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => entity.BirthPlace = value, updatedFields, "birthPlace");
            }
            else if (normalizedField is "nationalitycountrycode" or "countrycode" or "nationality")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => entity.NationalityCountryCode = value?.ToUpperInvariant(), updatedFields, "nationalityCountryCode");
            }
            else if (normalizedField is "websiteurl" or "website")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => entity.WebsiteUrl = value, updatedFields, "websiteUrl");
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.ParkFounder, field.Key));
            }
        }

        return ApplicationResult.Success();
    }

    internal static ApplicationResult ApplyAttractionManufacturerRawFields(AttractionManufacturer entity, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is "name")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => entity.Name = value ?? entity.Name, updatedFields, "name");
            }
            else if (normalizedField is "legalname")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => entity.LegalName = value, updatedFields, "legalName");
            }
            else if (normalizedField is "foundedyear")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyInt(field.Value, value => entity.FoundedYear = value, updatedFields, "foundedYear");
            }
            else if (normalizedField is "closedyear")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyInt(field.Value, value => entity.ClosedYear = value, updatedFields, "closedYear");
            }
            else if (normalizedField is "contactdetails" or "contact")
            {
                entity.ContactDetails = ApplyLocalizedContentJsonCommandHandlerAttractionDetailsExtensions.ReadContactDetails(field.Value, entity.ContactDetails);
                updatedFields.Add("contactDetails");
            }
            else if (normalizedField is "adminreviewstatus" or "reviewstatus")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyEnum<AdminReviewStatus>(field.Value, value => entity.AdminReviewStatus = value, updatedFields, "adminReviewStatus");
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.AttractionManufacturer, field.Key));
            }
        }

        return ApplicationResult.Success();
    }

    internal static ApplicationResult ApplyImageTagRawFields(ImageTag tag, IReadOnlyDictionary<string, JsonElement> rawFields, List<string> updatedFields)
    {
        foreach (KeyValuePair<string, JsonElement> field in rawFields)
        {
            string normalizedField = ApplyLocalizedContentJsonCommandHandlerParsingExtensions.NormalizeField(field.Key);
            if (normalizedField is "slug" or "key")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyString(field.Value, value => tag.Slug = value ?? tag.Slug, updatedFields, "slug");
            }
            else if (normalizedField is "isactive" or "active")
            {
                ApplyLocalizedContentJsonCommandHandlerFieldReadersExtensions.ApplyBool(field.Value, value => tag.IsActive = value, updatedFields, "isActive");
            }
            else
            {
                return ApplicationResult.Failure(LocalizedContentApplicationErrors.UnsupportedField(LocalizedContentEntityType.ImageTag, field.Key));
            }
        }

        return ApplicationResult.Success();
    }
}
