using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private static void ApplyManufacturerMergeSections(AttractionManufacturer source, AttractionManufacturer target, JsonElement? sections, ParkGraphUpsertChange change)
    {
        if (ShouldTakeSourceSection(sections, "identity"))
        {
            AddChange(change, "name", target.Name, source.Name);
            AddChange(change, "legalName", target.LegalName, source.LegalName);
            AddChange(change, "foundedYear", target.FoundedYear, source.FoundedYear);
            AddChange(change, "closedYear", target.ClosedYear, source.ClosedYear);
            target.Name = source.Name;
            target.LegalName = source.LegalName;
            target.FoundedYear = source.FoundedYear;
            target.ClosedYear = source.ClosedYear;
        }

        if (ShouldTakeSourceSection(sections, "contactDetails"))
        {
            AddChange(change, "contactDetails", DescribeContactDetails(target.ContactDetails), DescribeContactDetails(source.ContactDetails));
            target.ContactDetails = CloneContactDetails(source.ContactDetails);
        }

        if (ShouldTakeSourceSection(sections, "biography"))
        {
            AddLocalizedTextChanges(change, "biography", target.Biography, source.Biography);
            target.Biography = CloneLocalizedTexts(source.Biography);
        }

        if (ShouldTakeSourceSection(sections, "logo"))
        {
            AddChange(change, "currentLogoImageId", target.CurrentLogoImageId, source.CurrentLogoImageId);
            target.CurrentLogoImageId = source.CurrentLogoImageId;
        }

        if (ShouldTakeSourceSection(sections, "administration"))
        {
            AddChange(change, "isVisible", target.IsVisible, source.IsVisible);
            AddChange(change, "adminReviewStatus", target.AdminReviewStatus, source.AdminReviewStatus);
            target.IsVisible = source.IsVisible;
            target.AdminReviewStatus = source.AdminReviewStatus;
        }
    }

    private static void ApplyParkMergeSections(Park source, Park target, JsonElement? sections, ParkGraphUpsertChange change)
    {
        if (ShouldTakeSourceSection(sections, "identity"))
        {
            AddChange(change, "name", target.Name, source.Name);
            AddChange(change, "countryCode", target.CountryCode, source.CountryCode);
            AddChange(change, "type", target.Type, source.Type);
            AddChange(change, "status", target.Status, source.Status);
            target.Name = source.Name;
            target.CountryCode = source.CountryCode;
            target.Type = source.Type;
            target.Status = source.Status;
        }

        if (ShouldTakeSourceSection(sections, "ownership"))
        {
            AddChange(change, "founderId", target.FounderId, source.FounderId);
            AddChange(change, "operatorId", target.OperatorId, source.OperatorId);
            target.FounderId = source.FounderId;
            target.OperatorId = source.OperatorId;
        }

        if (ShouldTakeSourceSection(sections, "contact"))
        {
            AddChange(change, "websiteUrl", target.WebsiteUrl, source.WebsiteUrl);
            AddChange(change, "street", target.Street, source.Street);
            AddChange(change, "city", target.City, source.City);
            AddChange(change, "postalCode", target.PostalCode, source.PostalCode);
            target.WebsiteUrl = source.WebsiteUrl;
            target.Street = source.Street;
            target.City = source.City;
            target.PostalCode = source.PostalCode;
        }

        if (ShouldTakeSourceSection(sections, "descriptions"))
        {
            AddLocalizedTextChanges(change, "descriptions", target.Descriptions, source.Descriptions);
            target.Descriptions = CloneLocalizedTexts(source.Descriptions);
        }

        if (ShouldTakeSourceSection(sections, "location"))
        {
            AddChange(change, "position", FormatPosition(target.Position), FormatPosition(source.Position));
            target.SetPosition(source.Position);
        }

        if (ShouldTakeSourceSection(sections, "logo"))
        {
            AddChange(change, "currentLogoImageId", target.CurrentLogoImageId, source.CurrentLogoImageId);
            target.CurrentLogoImageId = source.CurrentLogoImageId;
        }

        if (ShouldTakeSourceSection(sections, "visibility"))
        {
            AddChange(change, "isVisible", target.IsVisible, source.IsVisible);
            AddChange(change, "adminReviewStatus", target.AdminReviewStatus, source.AdminReviewStatus);
            target.IsVisible = source.IsVisible;
            target.AdminReviewStatus = source.AdminReviewStatus;
        }

        if (ShouldTakeSourceSection(sections, "homeFeature"))
        {
            AddChange(change, "isFeaturedOnHome", target.IsFeaturedOnHome, source.IsFeaturedOnHome);
            AddChange(change, "featuredHomeOrder", target.FeaturedHomeOrder, source.FeaturedHomeOrder);
            AddChange(change, "isFeaturedOnHomeSponsored", target.IsFeaturedOnHomeSponsored, source.IsFeaturedOnHomeSponsored);
            target.IsFeaturedOnHome = source.IsFeaturedOnHome;
            target.FeaturedHomeOrder = source.FeaturedHomeOrder;
            target.IsFeaturedOnHomeSponsored = source.IsFeaturedOnHomeSponsored;
        }
    }

    private static void ApplyParkItemMergeSections(ParkItem source, ParkItem target, JsonElement? sections, ParkGraphUpsertChange change)
    {
        if (ShouldTakeSourceSection(sections, "identity"))
        {
            AddChange(change, "name", target.Name, source.Name);
            AddChange(change, "category", target.Category, source.Category);
            AddChange(change, "type", target.Type, source.Type);
            AddChange(change, "subtype", target.Subtype, source.Subtype);
            target.Name = source.Name;
            target.Category = source.Category;
            target.Type = source.Type;
            target.Subtype = source.Subtype;
        }

        if (ShouldTakeSourceSection(sections, "zone"))
        {
            AddChange(change, "zoneId", target.ZoneId, source.ZoneId);
            target.ZoneId = source.ZoneId;
        }

        if (ShouldTakeSourceSection(sections, "descriptions"))
        {
            AddLocalizedTextChanges(change, "descriptions", target.Descriptions, source.Descriptions);
            target.Descriptions = CloneLocalizedTexts(source.Descriptions);
        }

        if (ShouldTakeSourceSection(sections, "location"))
        {
            AddChange(change, "position", FormatPosition(target.Position), FormatPosition(source.Position));
            target.SetPosition(source.Position);
        }

        if (ShouldTakeSourceSection(sections, "attractionDetails"))
        {
            AddChange(change, "attractionDetails", DescribeAttractionDetails(target.AttractionDetails), DescribeAttractionDetails(source.AttractionDetails));
            target.AttractionDetails = CloneAttractionDetails(source.AttractionDetails);
        }

        if (ShouldTakeSourceSection(sections, "attractionLocations"))
        {
            AddChange(change, "attractionLocations", DescribeAttractionLocations(target.AttractionLocations), DescribeAttractionLocations(source.AttractionLocations));
            target.AttractionLocations = CloneAttractionLocations(source.AttractionLocations);
        }

        if (ShouldTakeSourceSection(sections, "visibility"))
        {
            AddChange(change, "isVisible", target.IsVisible, source.IsVisible);
            AddChange(change, "adminReviewStatus", target.AdminReviewStatus, source.AdminReviewStatus);
            target.IsVisible = source.IsVisible;
            target.AdminReviewStatus = source.AdminReviewStatus;
        }
    }
}
