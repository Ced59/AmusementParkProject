using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkItems.Services;

public sealed class ParkItemContentQualityService
{
    public ParkItemContentQualityResult Evaluate(ParkItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        List<string> availableLanguageCodes = item.Descriptions
            .Where(static description => !string.IsNullOrWhiteSpace(description.LanguageCode) && !string.IsNullOrWhiteSpace(description.Value))
            .Select(static description => description.LanguageCode.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static languageCode => languageCode, StringComparer.Ordinal)
            .ToList();

        bool structureComplete = !string.IsNullOrWhiteSpace(item.ParkId)
            && !string.IsNullOrWhiteSpace(item.Name)
            && item.Category != ParkItemCategory.Other;
        bool hasAnyDescription = availableLanguageCodes.Count > 0;
        bool hasFrenchDescription = HasDescription(item.Descriptions, "fr");
        bool hasEnglishDescription = HasDescription(item.Descriptions, "en");
        bool hasZone = !string.IsNullOrWhiteSpace(item.ZoneId);
        bool hasPreciseType = item.Type != ParkItemType.Other && item.Type != ParkItemType.Attraction;
        bool hasLocation = item.Position is not null || item.AttractionLocations?.Entrance is not null;
        bool hasAccessConditions = item.Category != ParkItemCategory.Attraction || item.AttractionDetails?.AccessConditions.Count > 0;

        List<string> missingRequirementKeys = new List<string>();
        AddMissing(missingRequirementKeys, structureComplete, "structure");
        AddMissing(missingRequirementKeys, hasAnyDescription, "descriptionAny");
        AddMissing(missingRequirementKeys, hasFrenchDescription, "descriptionFr");
        AddMissing(missingRequirementKeys, hasEnglishDescription, "descriptionEn");
        AddMissing(missingRequirementKeys, hasZone, "zone");
        AddMissing(missingRequirementKeys, hasPreciseType, "typePrecise");

        bool isPublishable = structureComplete
            && hasAnyDescription
            && hasPreciseType;

        return new ParkItemContentQualityResult
        {
            StructureComplete = structureComplete,
            HasAnyDescription = hasAnyDescription,
            HasFrenchDescription = hasFrenchDescription,
            HasEnglishDescription = hasEnglishDescription,
            HasZone = hasZone,
            HasPreciseType = hasPreciseType,
            HasLocation = hasLocation,
            HasAccessConditions = hasAccessConditions,
            IsPublishable = isPublishable,
            AvailableLanguageCodes = availableLanguageCodes,
            MissingRequirementKeys = missingRequirementKeys,
        };
    }

    public ParkItemAdminPublicationSignalsResult BuildPublicationSignals(ParkItem item)
    {
        ParkItemContentQualityResult quality = this.Evaluate(item);

        return new ParkItemAdminPublicationSignalsResult
        {
            IsVisible = item.IsVisible,
            AdminReviewStatus = item.AdminReviewStatus,
            LastUpdatedAtUtc = item.UpdatedAtUtc,
            AvailableLanguageCodes = quality.AvailableLanguageCodes,
            IsPublishable = quality.IsPublishable,
        };
    }

    private static bool HasDescription(IEnumerable<LocalizedText> descriptions, string languageCode)
    {
        return descriptions.Any(description =>
            string.Equals(description.LanguageCode?.Trim(), languageCode, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(description.Value));
    }

    private static void AddMissing(List<string> missingRequirementKeys, bool condition, string requirementKey)
    {
        if (!condition)
        {
            missingRequirementKeys.Add(requirementKey);
        }
    }
}
