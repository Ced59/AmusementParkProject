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
internal static class ParkGraphUpsertProcessorMergeSectionsExtensions
{
    internal static void ApplyManufacturerMergeSections(AttractionManufacturer source, AttractionManufacturer target, JsonElement? sections, ParkGraphUpsertChange change)
    {
        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "identity"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "name", target.Name, source.Name);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "legalName", target.LegalName, source.LegalName);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "foundedYear", target.FoundedYear, source.FoundedYear);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "closedYear", target.ClosedYear, source.ClosedYear);
            target.Name = source.Name;
            target.LegalName = source.LegalName;
            target.FoundedYear = source.FoundedYear;
            target.ClosedYear = source.ClosedYear;
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "contactDetails"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "contactDetails", ParkGraphUpsertProcessorPatchingExtensions.DescribeContactDetails(target.ContactDetails), ParkGraphUpsertProcessorPatchingExtensions.DescribeContactDetails(source.ContactDetails));
            target.ContactDetails = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneContactDetails(source.ContactDetails);
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "biography"))
        {
            ParkGraphUpsertProcessorLocalizedTextExtensions.AddLocalizedTextChanges(change, "biography", target.Biography, source.Biography);
            target.Biography = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneLocalizedTexts(source.Biography);
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "logo"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "currentLogoImageId", target.CurrentLogoImageId, source.CurrentLogoImageId);
            target.CurrentLogoImageId = source.CurrentLogoImageId;
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "administration"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "isVisible", target.IsVisible, source.IsVisible);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "adminReviewStatus", target.AdminReviewStatus, source.AdminReviewStatus);
            target.IsVisible = source.IsVisible;
            target.AdminReviewStatus = source.AdminReviewStatus;
        }
    }

    internal static void ApplyParkMergeSections(Park source, Park target, JsonElement? sections, ParkGraphUpsertChange change, ParkGraphUpsertResult result)
    {
        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "identity"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "name", target.Name, source.Name);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "countryCode", target.CountryCode, source.CountryCode);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "type", target.Type, source.Type);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "audienceClassification", target.AudienceClassification, source.AudienceClassification);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "status", target.Status, source.Status);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "openingDate", target.OpeningDate, source.OpeningDate);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "closingDate", target.ClosingDate, source.ClosingDate);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "openingDateText", target.OpeningDateText, source.OpeningDateText);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "closingDateText", target.ClosingDateText, source.ClosingDateText);
            target.Name = source.Name;
            target.CountryCode = source.CountryCode;
            target.Type = source.Type;
            target.AudienceClassification = source.AudienceClassification;
            target.Status = source.Status;
            target.OpeningDate = source.OpeningDate;
            target.ClosingDate = source.ClosingDate;
            target.OpeningDateText = source.OpeningDateText;
            target.ClosingDateText = source.ClosingDateText;
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "ownership"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "founderId", target.FounderId, source.FounderId);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "operatorId", target.OperatorId, source.OperatorId);
            target.FounderId = source.FounderId;
            target.OperatorId = source.OperatorId;
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "contact"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "websiteUrl", target.WebsiteUrl, source.WebsiteUrl);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "street", target.Street, source.Street);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "city", target.City, source.City);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "postalCode", target.PostalCode, source.PostalCode);
            target.WebsiteUrl = source.WebsiteUrl;
            target.Street = source.Street;
            target.City = source.City;
            target.PostalCode = source.PostalCode;
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "descriptions"))
        {
            ParkGraphUpsertProcessorLocalizedTextExtensions.AddLocalizedTextChanges(change, "descriptions", target.Descriptions, source.Descriptions);
            target.Descriptions = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneLocalizedTexts(source.Descriptions);
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "location"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "position", ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(target.Position), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(source.Position));
            target.SetPosition(source.Position);
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "logo"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "currentLogoImageId", target.CurrentLogoImageId, source.CurrentLogoImageId);
            target.CurrentLogoImageId = source.CurrentLogoImageId;
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "visibility"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "isVisible", target.IsVisible, source.IsVisible);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "adminReviewStatus", target.AdminReviewStatus, source.AdminReviewStatus);
            target.IsVisible = source.IsVisible;
            target.AdminReviewStatus = source.AdminReviewStatus;
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "homeFeature"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "isFeaturedOnHome", target.IsFeaturedOnHome, source.IsFeaturedOnHome);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "featuredHomeOrder", target.FeaturedHomeOrder, source.FeaturedHomeOrder);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "isFeaturedOnHomeSponsored", target.IsFeaturedOnHomeSponsored, source.IsFeaturedOnHomeSponsored);
            target.IsFeaturedOnHome = source.IsFeaturedOnHome;
            target.FeaturedHomeOrder = source.FeaturedHomeOrder;
            target.IsFeaturedOnHomeSponsored = source.IsFeaturedOnHomeSponsored;
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "officialMaps"))
        {
            int errorCountBeforeSection = result.Errors.Count;
            List<ParkOfficialMap> officialMaps = new List<ParkOfficialMap>();
            foreach (ParkOfficialMap sourceOfficialMap in source.OfficialMaps)
            {
                ParkOfficialMap officialMap = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneOfficialMap(sourceOfficialMap);
                if (!string.IsNullOrWhiteSpace(sourceOfficialMap.StorageKey))
                {
                    officialMap.StorageKey = ParkOfficialMapStorageKeys.ReassignToPark(sourceOfficialMap.StorageKey, source.Id, target.Id, sourceOfficialMap.Id);
                    if (officialMap.StorageKey is null)
                    {
                        result.Errors.Add($"La carte officielle '{sourceOfficialMap.Id}' ne possède pas une clé de stockage valide pour le parc source '{source.Id}'.");
                    }
                }

                officialMaps.Add(officialMap);
            }

            if (result.Errors.Count > errorCountBeforeSection)
            {
                return;
            }

            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "officialMaps", ParkGraphOfficialMapUpsertPatcher.Describe(target.OfficialMaps), ParkGraphOfficialMapUpsertPatcher.Describe(source.OfficialMaps));
            target.OfficialMaps = officialMaps;
        }
    }

    internal static void ApplyParkItemMergeSections(ParkItem source, ParkItem target, JsonElement? sections, ParkGraphUpsertChange change)
    {
        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "identity"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "name", target.Name, source.Name);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "category", target.Category, source.Category);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "type", target.Type, source.Type);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "subtype", target.Subtype, source.Subtype);
            target.Name = source.Name;
            target.Category = source.Category;
            target.Type = source.Type;
            target.Subtype = source.Subtype;
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "zone"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "zoneId", target.ZoneId, source.ZoneId);
            target.ZoneId = source.ZoneId;
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "descriptions"))
        {
            ParkGraphUpsertProcessorLocalizedTextExtensions.AddLocalizedTextChanges(change, "descriptions", target.Descriptions, source.Descriptions);
            target.Descriptions = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneLocalizedTexts(source.Descriptions);
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "location"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "position", ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(target.Position), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(source.Position));
            target.SetPosition(source.Position);
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "attractionDetails"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionDetails", ParkGraphUpsertProcessorMergeHelpersExtensions.DescribeAttractionDetails(target.AttractionDetails), ParkGraphUpsertProcessorMergeHelpersExtensions.DescribeAttractionDetails(source.AttractionDetails));
            target.AttractionDetails = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneAttractionDetails(source.AttractionDetails);
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "attractionLocations"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "attractionLocations", ParkGraphUpsertProcessorMergeHelpersExtensions.DescribeAttractionLocations(target.AttractionLocations), ParkGraphUpsertProcessorMergeHelpersExtensions.DescribeAttractionLocations(source.AttractionLocations));
            target.AttractionLocations = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneAttractionLocations(source.AttractionLocations);
        }

        if (ParkGraphUpsertProcessorMergeHelpersExtensions.ShouldTakeSourceSection(sections, "visibility"))
        {
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "isVisible", target.IsVisible, source.IsVisible);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "adminReviewStatus", target.AdminReviewStatus, source.AdminReviewStatus);
            target.IsVisible = source.IsVisible;
            target.AdminReviewStatus = source.AdminReviewStatus;
        }
    }
}
