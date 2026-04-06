using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkItems;

/// <summary>
/// Helpers de normalisation métier des park items et de leurs détails d'attraction.
/// </summary>
internal static class ParkItemNormalization
{
    public static void Normalize(ParkItem parkItem)
    {
        ArgumentNullException.ThrowIfNull(parkItem);

        parkItem.ParkId = NormalizeRequiredText(parkItem.ParkId);
        parkItem.ZoneId = NormalizeOptionalText(parkItem.ZoneId);
        parkItem.Name = NormalizeRequiredText(parkItem.Name);
        parkItem.Subtype = NormalizeOptionalText(parkItem.Subtype);
        parkItem.Descriptions = NormalizeLocalizedTexts(parkItem.Descriptions);
        parkItem.AttractionDetails = NormalizeAttractionDetails(parkItem.Category, parkItem.AttractionDetails);
        parkItem.AttractionLocations = NormalizeAttractionLocations(parkItem.Category, parkItem.AttractionLocations);
    }

    private static string NormalizeRequiredText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static List<LocalizedText> NormalizeLocalizedTexts(IEnumerable<LocalizedText>? values)
    {
        if (values is null)
        {
            return new List<LocalizedText>();
        }

        Dictionary<string, LocalizedText> deduplicated = new Dictionary<string, LocalizedText>(StringComparer.OrdinalIgnoreCase);

        foreach (LocalizedText value in values.Where(static value => value is not null))
        {
            if (string.IsNullOrWhiteSpace(value.LanguageCode))
            {
                continue;
            }

            string normalizedLanguageCode = value.LanguageCode.Trim().ToLowerInvariant();
            string? normalizedText = NormalizeOptionalText(value.Value);
            if (string.IsNullOrWhiteSpace(normalizedText))
            {
                continue;
            }

            deduplicated[normalizedLanguageCode] = new LocalizedText(normalizedLanguageCode, normalizedText);
        }

        return deduplicated.Values.ToList();
    }

    private static AttractionDetails? NormalizeAttractionDetails(ParkItemCategory category, AttractionDetails? details)
    {
        if (category != ParkItemCategory.Attraction || details is null)
        {
            return null;
        }

        AttractionDetails normalized = new AttractionDetails
        {
            ManufacturerId = NormalizeOptionalText(details.ManufacturerId),
            Model = NormalizeOptionalText(details.Model),
            ExternalSource = NormalizeOptionalText(details.ExternalSource),
            ExternalId = NormalizeOptionalText(details.ExternalId),
            SourceUrl = NormalizeOptionalText(details.SourceUrl),
            Status = NormalizeOptionalText(details.Status),
            MaterialType = NormalizeOptionalText(details.MaterialType),
            SeatingType = NormalizeOptionalText(details.SeatingType),
            LaunchType = NormalizeOptionalText(details.LaunchType),
            RestraintType = NormalizeOptionalText(details.RestraintType),
            IsLaunched = details.IsLaunched,
            OpeningDate = details.OpeningDate?.Date,
            ClosingDate = details.ClosingDate?.Date,
            OpeningDateText = NormalizeOptionalText(details.OpeningDateText),
            ClosingDateText = NormalizeOptionalText(details.ClosingDateText),
            DurationInSeconds = NormalizeNullableInt(details.DurationInSeconds),
            CapacityPerHour = NormalizeNullableInt(details.CapacityPerHour),
            HeightInFeet = NormalizeNullableDouble(details.HeightInFeet),
            HeightInMeters = NormalizeNullableDouble(details.HeightInMeters),
            LengthInFeet = NormalizeNullableDouble(details.LengthInFeet),
            LengthInMeters = NormalizeNullableDouble(details.LengthInMeters),
            SpeedInMph = NormalizeNullableDouble(details.SpeedInMph),
            SpeedInKmH = NormalizeNullableDouble(details.SpeedInKmH),
            DropInMeters = NormalizeNullableDouble(details.DropInMeters),
            InversionCount = NormalizeNullableInt(details.InversionCount),
            TrainCount = NormalizeNullableInt(details.TrainCount),
            CarsPerTrain = NormalizeNullableInt(details.CarsPerTrain),
            RidersPerVehicle = NormalizeNullableInt(details.RidersPerVehicle),
            HasSingleRider = details.HasSingleRider,
            HasFastPass = details.HasFastPass,
            IsAccessibleForReducedMobility = details.IsAccessibleForReducedMobility,
            IsIndoor = details.IsIndoor,
            WaterExposureLevel = details.WaterExposureLevel,
            AccessConditions = NormalizeAccessConditions(details.AccessConditions),
        };

        if (!HasAtLeastOneAttractionDetail(normalized))
        {
            return null;
        }

        return normalized;
    }

    private static List<AttractionAccessCondition> NormalizeAccessConditions(IEnumerable<AttractionAccessCondition>? values)
    {
        if (values is null)
        {
            return new List<AttractionAccessCondition>();
        }

        List<AttractionAccessCondition> normalized = values
            .Where(static value => value is not null)
            .Select(NormalizeAccessCondition)
            .Where(static value => value is not null)
            .Cast<AttractionAccessCondition>()
            .OrderBy(static value => value.DisplayOrder ?? int.MaxValue)
            .ToList();

        return normalized;
    }

    private static AttractionAccessCondition? NormalizeAccessCondition(AttractionAccessCondition value)
    {
        ArgumentNullException.ThrowIfNull(value);

        AttractionAccessCondition normalized = new AttractionAccessCondition
        {
            Type = value.Type,
            IsCustom = value.IsCustom == true || value.Type == AttractionAccessConditionType.Custom ? true : null,
            Value = NormalizeNullableDouble(value.Value),
            Unit = value.Unit,
            RequiresAccompaniment = value.RequiresAccompaniment,
            MinimumCompanionAge = NormalizeNullableInt(value.MinimumCompanionAge),
            Label = NormalizeLocalizedTexts(value.Label),
            Description = NormalizeLocalizedTexts(value.Description),
            DisplayOrder = NormalizeNullableInt(value.DisplayOrder),
        };

        if (!HasAtLeastOneAccessConditionValue(normalized))
        {
            return null;
        }

        return normalized;
    }

    private static bool HasAtLeastOneAccessConditionValue(AttractionAccessCondition condition)
    {
        if (condition.Type != AttractionAccessConditionType.Custom)
        {
            return true;
        }

        return condition.Value != null ||
               condition.Unit != null ||
               condition.RequiresAccompaniment == true ||
               condition.MinimumCompanionAge != null ||
               condition.Label.Count > 0 ||
               condition.Description.Count > 0;
    }

    private static AttractionLocations? NormalizeAttractionLocations(ParkItemCategory category, AttractionLocations? locations)
    {
        if (category != ParkItemCategory.Attraction || locations is null)
        {
            return null;
        }

        AttractionLocations normalized = new AttractionLocations
        {
            Entrance = NormalizeGeoPoint(locations.Entrance),
            Exit = NormalizeGeoPoint(locations.Exit),
            FastPassEntrance = NormalizeGeoPoint(locations.FastPassEntrance),
            ReducedMobilityEntrance = NormalizeGeoPoint(locations.ReducedMobilityEntrance),
        };

        if (normalized.Entrance is null &&
            normalized.Exit is null &&
            normalized.FastPassEntrance is null &&
            normalized.ReducedMobilityEntrance is null)
        {
            return null;
        }

        return normalized;
    }

    private static GeoPoint? NormalizeGeoPoint(GeoPoint? value)
    {
        if (value is null)
        {
            return null;
        }

        if (!IsValidLatitude(value.Latitude) || !IsValidLongitude(value.Longitude))
        {
            return null;
        }

        return new GeoPoint(value.Latitude, value.Longitude);
    }

    private static int? NormalizeNullableInt(int? value)
    {
        return value.HasValue && value.Value >= 0 ? value.Value : null;
    }

    private static double? NormalizeNullableDouble(double? value)
    {
        return value.HasValue && value.Value >= 0 ? value.Value : null;
    }

    private static bool HasAtLeastOneAttractionDetail(AttractionDetails details)
    {
        return !string.IsNullOrWhiteSpace(details.ManufacturerId) ||
               !string.IsNullOrWhiteSpace(details.Model) ||
               !string.IsNullOrWhiteSpace(details.ExternalSource) ||
               !string.IsNullOrWhiteSpace(details.ExternalId) ||
               !string.IsNullOrWhiteSpace(details.SourceUrl) ||
               !string.IsNullOrWhiteSpace(details.Status) ||
               !string.IsNullOrWhiteSpace(details.MaterialType) ||
               !string.IsNullOrWhiteSpace(details.SeatingType) ||
               !string.IsNullOrWhiteSpace(details.LaunchType) ||
               !string.IsNullOrWhiteSpace(details.RestraintType) ||
               details.IsLaunched == true ||
               details.OpeningDate != null ||
               details.ClosingDate != null ||
               !string.IsNullOrWhiteSpace(details.OpeningDateText) ||
               !string.IsNullOrWhiteSpace(details.ClosingDateText) ||
               details.DurationInSeconds != null ||
               details.CapacityPerHour != null ||
               details.HeightInFeet != null ||
               details.HeightInMeters != null ||
               details.LengthInFeet != null ||
               details.LengthInMeters != null ||
               details.SpeedInMph != null ||
               details.SpeedInKmH != null ||
               details.DropInMeters != null ||
               details.InversionCount != null ||
               details.TrainCount != null ||
               details.CarsPerTrain != null ||
               details.RidersPerVehicle != null ||
               details.HasSingleRider == true ||
               details.HasFastPass == true ||
               details.IsAccessibleForReducedMobility == true ||
               details.IsIndoor == true ||
               details.WaterExposureLevel != null ||
               details.AccessConditions.Count > 0;
    }

    private static bool IsValidLatitude(double latitude)
    {
        return latitude >= -90 && latitude <= 90;
    }

    private static bool IsValidLongitude(double longitude)
    {
        return longitude >= -180 && longitude <= 180;
    }
}
