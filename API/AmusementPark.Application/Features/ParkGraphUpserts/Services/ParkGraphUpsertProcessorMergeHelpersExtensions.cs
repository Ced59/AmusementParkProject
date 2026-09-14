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
internal static class ParkGraphUpsertProcessorMergeHelpersExtensions
{
    internal static async Task NotifyMergeSeoAsync(this ParkGraphUpsertProcessor processorContext, ParkGraphUpsertMergeSummary summary, CancellationToken cancellationToken)
    {
        if (summary.PreviousParks.Count == 0 && summary.CurrentParks.Count == 0 && summary.PreviousParkItems.Count == 0 && summary.CurrentParkItems.Count == 0)
        {
            return;
        }

        await processorContext.publicSeoUpdateNotifier.NotifyAsync(new PublicSeoUpdate { PreviousParks = summary.PreviousParks, CurrentParks = summary.CurrentParks, PreviousParkItems = summary.PreviousParkItems, CurrentParkItems = summary.CurrentParkItems, IncludeDiscoveryPages = true, }, cancellationToken);
    }

    internal static string NormalizeMergeEntityType(string? value)
    {
        string normalized = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeEnumToken(value ?? string.Empty).ToLowerInvariant();
        return normalized switch
        {
            "manufacturer" or "manufacturers" or "attractionmanufacturer" or "attractionmanufacturers" => "AttractionManufacturer",
            "park" or "parks" => "Park",
            "parkitem" or "parkitems" or "item" or "items" => "ParkItem",
            _ => value?.Trim() ?? string.Empty,
        };
    }

    internal static bool ShouldTakeSourceSection(JsonElement? sections, string sectionName)
    {
        if (sections is null || sections.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        string? value = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(sections, sectionName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return string.Equals(value, "source", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "fromSource", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "duplicate", StringComparison.OrdinalIgnoreCase);
    }

    internal static void AddAttachmentCountChange(ParkGraphUpsertChange change, string fieldName, int count)
    {
        if (count <= 0)
        {
            return;
        }

        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, fieldName, null, count);
    }

    internal static ParkGraphUpsertChange BuildDeletedMergeSourceChange(string entityType, string sourceId, string displayName, string targetId)
    {
        ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange(entityType, sourceId, null, displayName, "Deleted", "mergeSource");
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "mergedInto", sourceId, targetId);
        return change;
    }

    internal static void ApplyManufacturerIdRemaps(Dictionary<string, string> manufacturerKeys, Dictionary<string, string> remaps)
    {
        if (remaps.Count == 0)
        {
            return;
        }

        List<string> keys = manufacturerKeys.Keys.ToList();
        foreach (string key in keys)
        {
            string id = manufacturerKeys[key];
            if (remaps.TryGetValue(id, out string? targetId))
            {
                manufacturerKeys[key] = targetId;
            }
        }
    }

    internal static void AddManufacturerKeyRemaps(Dictionary<string, string> manufacturerKeys, string sourceId, string targetId)
    {
        List<string> keys = manufacturerKeys.Where(pair => string.Equals(pair.Value, sourceId, StringComparison.Ordinal)).Select(static pair => pair.Key).ToList();
        foreach (string key in keys)
        {
            manufacturerKeys[key] = targetId;
        }
    }

    internal static string? RemapId(Dictionary<string, string> remaps, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        return remaps.TryGetValue(id.Trim(), out string? targetId) ? targetId : id;
    }

    internal static AttractionManufacturer CloneManufacturer(AttractionManufacturer value)
    {
        return new AttractionManufacturer
        {
            Id = value.Id,
            CreatedAtUtc = value.CreatedAtUtc,
            UpdatedAtUtc = value.UpdatedAtUtc,
            Name = value.Name,
            LegalName = value.LegalName,
            FoundedYear = value.FoundedYear,
            ClosedYear = value.ClosedYear,
            ContactDetails = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneContactDetails(value.ContactDetails),
            Biography = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneLocalizedTexts(value.Biography),
            CurrentLogoImageId = value.CurrentLogoImageId,
            IsVisible = value.IsVisible,
            AdminReviewStatus = value.AdminReviewStatus,
        };
    }

    internal static Park ClonePark(Park value)
    {
        Park clone = new Park
        {
            Id = value.Id,
            CreatedAtUtc = value.CreatedAtUtc,
            UpdatedAtUtc = value.UpdatedAtUtc,
            Name = value.Name,
            CountryCode = value.CountryCode,
            Type = value.Type,
            Status = value.Status,
            OpeningDate = value.OpeningDate,
            ClosingDate = value.ClosingDate,
            OpeningDateText = value.OpeningDateText,
            ClosingDateText = value.ClosingDateText,
            FounderId = value.FounderId,
            OperatorId = value.OperatorId,
            Descriptions = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneLocalizedTexts(value.Descriptions),
            IsVisible = value.IsVisible,
            AdminReviewStatus = value.AdminReviewStatus,
            IsFeaturedOnHome = value.IsFeaturedOnHome,
            FeaturedHomeOrder = value.FeaturedHomeOrder,
            IsFeaturedOnHomeSponsored = value.IsFeaturedOnHomeSponsored,
            WebsiteUrl = value.WebsiteUrl,
            Street = value.Street,
            City = value.City,
            PostalCode = value.PostalCode,
            CurrentLogoImageId = value.CurrentLogoImageId,
            OfficialMaps = value.OfficialMaps.Select(static officialMap => ParkGraphUpsertProcessorMergeHelpersExtensions.CloneOfficialMap(officialMap)).ToList(),
        };
        clone.SetPosition(value.Position);
        return clone;
    }

    internal static ParkOfficialMap CloneOfficialMap(ParkOfficialMap value)
    {
        return new ParkOfficialMap
        {
            Id = value.Id,
            Year = value.Year,
            Format = value.Format,
            DocumentUrl = value.DocumentUrl,
            StorageKey = value.StorageKey,
            OriginalFileName = value.OriginalFileName,
            ContentType = value.ContentType,
            SizeInBytes = value.SizeInBytes,
            PreviewImageUrl = value.PreviewImageUrl,
            SourcePageUrl = value.SourcePageUrl,
            LanguageCode = value.LanguageCode,
            Titles = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneLocalizedTexts(value.Titles),
            AlternativeTexts = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneLocalizedTexts(value.AlternativeTexts),
            IsVisible = value.IsVisible,
            LastVerifiedAtUtc = value.LastVerifiedAtUtc,
        };
    }

    internal static ParkItem CloneParkItem(ParkItem value)
    {
        ParkItem clone = new ParkItem
        {
            Id = value.Id,
            CreatedAtUtc = value.CreatedAtUtc,
            UpdatedAtUtc = value.UpdatedAtUtc,
            ParkId = value.ParkId,
            ZoneId = value.ZoneId,
            Name = value.Name,
            Category = value.Category,
            Type = value.Type,
            Subtype = value.Subtype,
            Descriptions = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneLocalizedTexts(value.Descriptions),
            AttractionDetails = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneAttractionDetails(value.AttractionDetails),
            AttractionLocations = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneAttractionLocations(value.AttractionLocations),
            IsVisible = value.IsVisible,
            AdminReviewStatus = value.AdminReviewStatus,
        };
        clone.SetPosition(value.Position);
        return clone;
    }

    internal static ParkReferenceContactDetails? CloneContactDetails(ParkReferenceContactDetails? value)
    {
        if (value is null)
        {
            return null;
        }

        return new ParkReferenceContactDetails
        {
            WebsiteUrl = value.WebsiteUrl,
            Email = value.Email,
            PhoneNumber = value.PhoneNumber,
            Street = value.Street,
            City = value.City,
            PostalCode = value.PostalCode,
            CountryCode = value.CountryCode,
            Latitude = value.Latitude,
            Longitude = value.Longitude,
        };
    }

    internal static List<LocalizedText> CloneLocalizedTexts(IReadOnlyCollection<LocalizedText> values)
    {
        return values.Select(static value => new LocalizedText(value.LanguageCode, value.Value)).ToList();
    }

    internal static AttractionDetails? CloneAttractionDetails(AttractionDetails? value)
    {
        if (value is null)
        {
            return null;
        }

        return new AttractionDetails
        {
            ManufacturerId = value.ManufacturerId,
            Model = value.Model,
            ExternalSource = value.ExternalSource,
            ExternalId = value.ExternalId,
            SourceUrl = value.SourceUrl,
            Status = value.Status,
            MaterialType = value.MaterialType,
            SeatingType = value.SeatingType,
            LaunchType = value.LaunchType,
            RestraintType = value.RestraintType,
            IsLaunched = value.IsLaunched,
            OpeningDate = value.OpeningDate,
            ClosingDate = value.ClosingDate,
            OpeningDateText = value.OpeningDateText,
            ClosingDateText = value.ClosingDateText,
            DurationInSeconds = value.DurationInSeconds,
            CapacityPerHour = value.CapacityPerHour,
            HeightInFeet = value.HeightInFeet,
            HeightInMeters = value.HeightInMeters,
            LengthInFeet = value.LengthInFeet,
            LengthInMeters = value.LengthInMeters,
            SpeedInMph = value.SpeedInMph,
            SpeedInKmH = value.SpeedInKmH,
            DropInFeet = value.DropInFeet,
            DropInMeters = value.DropInMeters,
            InversionCount = value.InversionCount,
            TrainCount = value.TrainCount,
            CarsPerTrain = value.CarsPerTrain,
            RidersPerVehicle = value.RidersPerVehicle,
            HasSingleRider = value.HasSingleRider,
            HasFastPass = value.HasFastPass,
            IsAccessibleForReducedMobility = value.IsAccessibleForReducedMobility,
            IsIndoor = value.IsIndoor,
            WaterExposureLevel = value.WaterExposureLevel,
            AccessConditions = value.AccessConditions.Select(ParkGraphUpsertProcessorMergeHelpersExtensions.CloneAccessCondition).ToList(),
        };
    }

    internal static AttractionAccessCondition CloneAccessCondition(AttractionAccessCondition value)
    {
        return new AttractionAccessCondition
        {
            Type = value.Type,
            TypeKey = value.TypeKey,
            IsCustom = value.IsCustom,
            CustomTypeKey = value.CustomTypeKey,
            CustomTypeLabel = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneLocalizedTexts(value.CustomTypeLabel),
            Value = value.Value,
            Unit = value.Unit,
            RequiresAccompaniment = value.RequiresAccompaniment,
            MinimumCompanionAge = value.MinimumCompanionAge,
            Label = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneLocalizedTexts(value.Label),
            Description = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneLocalizedTexts(value.Description),
            DisplayOrder = value.DisplayOrder,
            ProvenanceSchemaVersion = value.ProvenanceSchemaVersion,
            SourceKind = value.SourceKind,
            SourceUrl = value.SourceUrl,
            SourceReference = value.SourceReference,
            CollectedAtUtc = value.CollectedAtUtc,
            VerifiedAtUtc = value.VerifiedAtUtc,
            SourceLanguageCode = value.SourceLanguageCode,
            SourceSummary = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneLocalizedTexts(value.SourceSummary),
            SourceConfidence = value.SourceConfidence,
            Scope = value.Scope,
            ScopeDetail = value.ScopeDetail,
            EffectiveFrom = value.EffectiveFrom,
            EffectiveTo = value.EffectiveTo,
        };
    }

    internal static AttractionLocations? CloneAttractionLocations(AttractionLocations? value)
    {
        if (value is null)
        {
            return null;
        }

        return new AttractionLocations
        {
            Entrance = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneGeoPoint(value.Entrance),
            Exit = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneGeoPoint(value.Exit),
            FastPassEntrance = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneGeoPoint(value.FastPassEntrance),
            ReducedMobilityEntrance = ParkGraphUpsertProcessorMergeHelpersExtensions.CloneGeoPoint(value.ReducedMobilityEntrance),
        };
    }

    internal static GeoPoint? CloneGeoPoint(GeoPoint? value)
    {
        return value is null ? null : new GeoPoint(value.Latitude, value.Longitude);
    }

    internal static string? DescribeAttractionDetails(AttractionDetails? value)
    {
        if (value is null)
        {
            return null;
        }

        return string.Join(" | ", new[] { value.ManufacturerId, value.Model, value.ExternalSource, value.ExternalId, value.Status, value.MaterialType, value.SeatingType, value.LaunchType, value.RestraintType, ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(value.OpeningDate), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(value.ClosingDate), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(value.HeightInMeters), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(value.LengthInMeters), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(value.SpeedInKmH), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(value.DropInMeters), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(value.InversionCount), ParkGraphUpsertProcessorPatchingExtensions.DescribeAccessConditions(value.AccessConditions), }.Where(static item => !string.IsNullOrWhiteSpace(item)));
    }

    internal static string? DescribeAttractionLocations(AttractionLocations? value)
    {
        if (value is null)
        {
            return null;
        }

        return string.Join(" | ", new[] { ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(value.Entrance), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(value.Exit), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(value.FastPassEntrance), ParkGraphUpsertProcessorLocalizedTextExtensions.FormatPosition(value.ReducedMobilityEntrance), }.Where(static item => !string.IsNullOrWhiteSpace(item)));
    }
}
