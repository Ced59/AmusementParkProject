using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task NotifyMergeSeoAsync(ParkGraphUpsertMergeSummary summary, CancellationToken cancellationToken)
    {
        if (summary.PreviousParks.Count == 0
            && summary.CurrentParks.Count == 0
            && summary.PreviousParkItems.Count == 0
            && summary.CurrentParkItems.Count == 0)
        {
            return;
        }

        await this.publicSeoUpdateNotifier.NotifyAsync(
            new PublicSeoUpdate
            {
                PreviousParks = summary.PreviousParks,
                CurrentParks = summary.CurrentParks,
                PreviousParkItems = summary.PreviousParkItems,
                CurrentParkItems = summary.CurrentParkItems,
                IncludeDiscoveryPages = true,
            },
            cancellationToken);
    }

    private static string NormalizeMergeEntityType(string? value)
    {
        string normalized = NormalizeEnumToken(value ?? string.Empty).ToLowerInvariant();
        return normalized switch
        {
            "manufacturer" or "manufacturers" or "attractionmanufacturer" or "attractionmanufacturers" => "AttractionManufacturer",
            "park" or "parks" => "Park",
            "parkitem" or "parkitems" or "item" or "items" => "ParkItem",
            _ => value?.Trim() ?? string.Empty,
        };
    }

    private static bool ShouldTakeSourceSection(JsonElement? sections, string sectionName)
    {
        if (sections is null || sections.Value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        string? value = ReadString(sections, sectionName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return string.Equals(value, "source", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "fromSource", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "duplicate", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddAttachmentCountChange(ParkGraphUpsertChange change, string fieldName, int count)
    {
        if (count <= 0)
        {
            return;
        }

        AddChange(change, fieldName, null, count);
    }

    private static ParkGraphUpsertChange BuildDeletedMergeSourceChange(string entityType, string sourceId, string displayName, string targetId)
    {
        ParkGraphUpsertChange change = BuildEntityChange(entityType, sourceId, null, displayName, "Deleted", "mergeSource");
        AddChange(change, "mergedInto", sourceId, targetId);
        return change;
    }

    private static void ApplyManufacturerIdRemaps(Dictionary<string, string> manufacturerKeys, Dictionary<string, string> remaps)
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

    private static void AddManufacturerKeyRemaps(Dictionary<string, string> manufacturerKeys, string sourceId, string targetId)
    {
        List<string> keys = manufacturerKeys
            .Where(pair => string.Equals(pair.Value, sourceId, StringComparison.Ordinal))
            .Select(static pair => pair.Key)
            .ToList();

        foreach (string key in keys)
        {
            manufacturerKeys[key] = targetId;
        }
    }

    private static string? RemapId(Dictionary<string, string> remaps, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        return remaps.TryGetValue(id.Trim(), out string? targetId) ? targetId : id;
    }

    private static AttractionManufacturer CloneManufacturer(AttractionManufacturer value)
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
            ContactDetails = CloneContactDetails(value.ContactDetails),
            Biography = CloneLocalizedTexts(value.Biography),
            CurrentLogoImageId = value.CurrentLogoImageId,
            IsVisible = value.IsVisible,
            AdminReviewStatus = value.AdminReviewStatus,
        };
    }

    private static Park ClonePark(Park value)
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
            FounderId = value.FounderId,
            OperatorId = value.OperatorId,
            Descriptions = CloneLocalizedTexts(value.Descriptions),
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
        };
        clone.SetPosition(value.Position);
        return clone;
    }

    private static ParkItem CloneParkItem(ParkItem value)
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
            Descriptions = CloneLocalizedTexts(value.Descriptions),
            AttractionDetails = CloneAttractionDetails(value.AttractionDetails),
            AttractionLocations = CloneAttractionLocations(value.AttractionLocations),
            IsVisible = value.IsVisible,
            AdminReviewStatus = value.AdminReviewStatus,
        };
        clone.SetPosition(value.Position);
        return clone;
    }

    private static ParkReferenceContactDetails? CloneContactDetails(ParkReferenceContactDetails? value)
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

    private static List<LocalizedText> CloneLocalizedTexts(IReadOnlyCollection<LocalizedText> values)
    {
        return values
            .Select(static value => new LocalizedText(value.LanguageCode, value.Value))
            .ToList();
    }

    private static AttractionDetails? CloneAttractionDetails(AttractionDetails? value)
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
            AccessConditions = value.AccessConditions.Select(CloneAccessCondition).ToList(),
        };
    }

    private static AttractionAccessCondition CloneAccessCondition(AttractionAccessCondition value)
    {
        return new AttractionAccessCondition
        {
            Type = value.Type,
            TypeKey = value.TypeKey,
            IsCustom = value.IsCustom,
            CustomTypeKey = value.CustomTypeKey,
            CustomTypeLabel = CloneLocalizedTexts(value.CustomTypeLabel),
            Value = value.Value,
            Unit = value.Unit,
            RequiresAccompaniment = value.RequiresAccompaniment,
            MinimumCompanionAge = value.MinimumCompanionAge,
            Label = CloneLocalizedTexts(value.Label),
            Description = CloneLocalizedTexts(value.Description),
            DisplayOrder = value.DisplayOrder,
        };
    }

    private static AttractionLocations? CloneAttractionLocations(AttractionLocations? value)
    {
        if (value is null)
        {
            return null;
        }

        return new AttractionLocations
        {
            Entrance = CloneGeoPoint(value.Entrance),
            Exit = CloneGeoPoint(value.Exit),
            FastPassEntrance = CloneGeoPoint(value.FastPassEntrance),
            ReducedMobilityEntrance = CloneGeoPoint(value.ReducedMobilityEntrance),
        };
    }

    private static GeoPoint? CloneGeoPoint(GeoPoint? value)
    {
        return value is null ? null : new GeoPoint(value.Latitude, value.Longitude);
    }

    private static string? DescribeAttractionDetails(AttractionDetails? value)
    {
        if (value is null)
        {
            return null;
        }

        return string.Join(" | ", new[]
        {
            value.ManufacturerId,
            value.Model,
            value.ExternalSource,
            value.ExternalId,
            value.Status,
            value.MaterialType,
            value.SeatingType,
            value.LaunchType,
            value.RestraintType,
            FormatValue(value.OpeningDate),
            FormatValue(value.ClosingDate),
            FormatValue(value.HeightInMeters),
            FormatValue(value.LengthInMeters),
            FormatValue(value.SpeedInKmH),
            FormatValue(value.DropInMeters),
            FormatValue(value.InversionCount),
            DescribeAccessConditions(value.AccessConditions),
        }.Where(static item => !string.IsNullOrWhiteSpace(item)));
    }

    private static string? DescribeAttractionLocations(AttractionLocations? value)
    {
        if (value is null)
        {
            return null;
        }

        return string.Join(" | ", new[]
        {
            FormatPosition(value.Entrance),
            FormatPosition(value.Exit),
            FormatPosition(value.FastPassEntrance),
            FormatPosition(value.ReducedMobilityEntrance),
        }.Where(static item => !string.IsNullOrWhiteSpace(item)));
    }

    private sealed class ParkGraphUpsertMergeSummary
    {
        public Dictionary<string, string> ManufacturerIdRemaps { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        public HashSet<string> ChangedParkIds { get; } = new HashSet<string>(StringComparer.Ordinal);

        public HashSet<string> ChangedParkItemIds { get; } = new HashSet<string>(StringComparer.Ordinal);

        public List<PublicSeoParkSnapshot> PreviousParks { get; } = new List<PublicSeoParkSnapshot>();

        public List<PublicSeoParkSnapshot> CurrentParks { get; } = new List<PublicSeoParkSnapshot>();

        public List<PublicSeoParkItemSnapshot> PreviousParkItems { get; } = new List<PublicSeoParkItemSnapshot>();

        public List<PublicSeoParkItemSnapshot> CurrentParkItems { get; } = new List<PublicSeoParkItemSnapshot>();
    }
}
