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
internal static class ParkGraphUpsertProcessorResolutionExtensions
{
    internal static List<AttractionAccessCondition> ReadAccessConditions(JsonElement? array)
    {
        if (array is null || array.Value.ValueKind != JsonValueKind.Array)
        {
            return new List<AttractionAccessCondition>();
        }

        List<AttractionAccessCondition> conditions = new List<AttractionAccessCondition>();
        foreach (JsonElement item in array.Value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            AttractionAccessCondition condition = new AttractionAccessCondition
            {
                Type = ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnum(item, "type", AttractionAccessConditionType.Custom),
                TypeKey = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "typeKey"),
                IsCustom = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(item, "isCustom"),
                CustomTypeKey = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "customTypeKey"),
                CustomTypeLabel = ParkGraphUpsertProcessorLocalizedTextExtensions.ReadLocalizedTexts(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(item, "customTypeLabel")),
                Value = ParkGraphUpsertProcessorJsonReadingExtensions.ReadDouble(item, "value"),
                Unit = ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnumNullable<AttractionAccessConditionUnit>(item, "unit"),
                RequiresAccompaniment = ParkGraphUpsertProcessorJsonReadingExtensions.ReadBool(item, "requiresAccompaniment"),
                MinimumCompanionAge = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(item, "minimumCompanionAge"),
                Label = ParkGraphUpsertProcessorLocalizedTextExtensions.ReadLocalizedTexts(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(item, "label")),
                Description = ParkGraphUpsertProcessorLocalizedTextExtensions.ReadLocalizedTexts(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(item, "description")),
                DisplayOrder = ParkGraphUpsertProcessorJsonReadingExtensions.ReadInt(item, "displayOrder"),
                ProvenanceSchemaVersion = AttractionAccessCondition.CurrentProvenanceSchemaVersion,
                SourceKind = ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnum(
                    item,
                    "sourceKind",
                    AttractionAccessConditionSourceKind.Unknown),
                SourceUrl = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "sourceUrl"),
                SourceReference = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "sourceReference"),
                CollectedAtUtc = ReadUtcDate(item, "collectedAtUtc"),
                VerifiedAtUtc = ReadUtcDate(item, "verifiedAtUtc"),
                SourceLanguageCode = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "sourceLanguageCode")?.ToLowerInvariant(),
                SourceSummary = ParkGraphUpsertProcessorLocalizedTextExtensions.ReadLocalizedTexts(ParkGraphUpsertProcessorJsonReadingExtensions.GetArray(item, "sourceSummary")),
                SourceConfidence = ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnum(
                    item,
                    "sourceConfidence",
                    AttractionAccessConditionConfidence.Unknown),
                Scope = ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnum(
                    item,
                    "scope",
                    AttractionAccessConditionScope.Attraction),
                ScopeDetail = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "scopeDetail"),
                EffectiveFrom = ReadDateOnly(item, "effectiveFrom"),
                EffectiveTo = ReadDateOnly(item, "effectiveTo"),
            };
            conditions.Add(condition);
        }

        return conditions;
    }

    private static DateTime? ReadUtcDate(JsonElement item, string propertyName)
    {
        string? value = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, propertyName);
        if (string.IsNullOrWhiteSpace(value) || !HasExplicitTimezone(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out DateTimeOffset parsed)
                ? parsed.UtcDateTime
                : null;
    }

    private static bool HasExplicitTimezone(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        int timeSeparatorIndex = trimmed.IndexOf("T", StringComparison.OrdinalIgnoreCase);
        int lastPlusIndex = trimmed.LastIndexOf('+');
        int lastMinusIndex = trimmed.LastIndexOf('-');
        return timeSeparatorIndex >= 0
            && (lastPlusIndex > timeSeparatorIndex || lastMinusIndex > timeSeparatorIndex);
    }

    private static DateOnly? ReadDateOnly(JsonElement item, string propertyName)
    {
        string? value = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, propertyName);
        return DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly parsed)
                ? parsed
                : null;
    }

    internal static void ResolveImageOwner(JsonElement patch, Park park, Dictionary<string, string> itemKeys, string? ownerTypeText, string? ownerId, out ImageOwnerType ownerType, out string? resolvedOwnerId)
    {
        ownerType = ParkGraphUpsertProcessorJsonReadingExtensions.ReadEnumFromText(ownerTypeText, ImageOwnerType.Park);
        resolvedOwnerId = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ownerId);
        string? ownerKey = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "ownerKey");
        if (string.Equals(ownerKey, "park", StringComparison.OrdinalIgnoreCase))
        {
            ownerType = ImageOwnerType.Park;
            resolvedOwnerId = park.Id;
            return;
        }

        if (!string.IsNullOrWhiteSpace(ownerKey) && itemKeys.TryGetValue(ownerKey, out string? itemId))
        {
            ownerType = ImageOwnerType.ParkItem;
            resolvedOwnerId = itemId;
            return;
        }

        if (string.IsNullOrWhiteSpace(ownerKey) == false)
        {
            string normalizedItemNameKey = $"item:{ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(ownerKey)}";
            if (itemKeys.TryGetValue(normalizedItemNameKey, out string? itemIdByName))
            {
                ownerType = ImageOwnerType.ParkItem;
                resolvedOwnerId = itemIdByName;
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(resolvedOwnerId))
        {
            resolvedOwnerId = park.Id;
            ownerType = ImageOwnerType.Park;
        }
    }

    internal static T? FindByIdOrName<T>(IReadOnlyCollection<T> entities, string? id, string? name, Func<T, string> idSelector, Func<T, string> nameSelector)
        where T : class
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            T? byId = entities.FirstOrDefault(entity => string.Equals(idSelector(entity), id, StringComparison.Ordinal));
            if (byId is not null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            string normalizedName = ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(name);
            return entities.FirstOrDefault(entity => string.Equals(ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(nameSelector(entity)), normalizedName, StringComparison.OrdinalIgnoreCase));
        }

        return default;
    }

    internal static ParkZone? FindZone(IReadOnlyCollection<ParkZone> zones, string? id, string? slug, string? name)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            ParkZone? byId = zones.FirstOrDefault(zone => string.Equals(zone.Id, id, StringComparison.Ordinal));
            if (byId is not null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(slug))
        {
            ParkZone? bySlug = zones.FirstOrDefault(zone => string.Equals(zone.Slug, slug, StringComparison.OrdinalIgnoreCase));
            if (bySlug is not null)
            {
                return bySlug;
            }
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            string normalizedName = ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(name);
            return zones.FirstOrDefault(zone => string.Equals(ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(zone.Name), normalizedName, StringComparison.OrdinalIgnoreCase) || zone.Names.Any(localized => string.Equals(ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(localized.Value), normalizedName, StringComparison.OrdinalIgnoreCase)));
        }

        return null;
    }

    internal static ParkItem? FindItem(IReadOnlyCollection<ParkItem> items, string? id, string? name, string? externalSource, string? externalId)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            ParkItem? byId = items.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal));
            if (byId is not null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(externalSource) && !string.IsNullOrWhiteSpace(externalId))
        {
            ParkItem? byExternalId = items.FirstOrDefault(item => string.Equals(item.AttractionDetails?.ExternalSource, externalSource, StringComparison.OrdinalIgnoreCase) && string.Equals(item.AttractionDetails?.ExternalId, externalId, StringComparison.OrdinalIgnoreCase));
            if (byExternalId is not null)
            {
                return byExternalId;
            }
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            string normalizedName = ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(name);
            List<ParkItem> matches = items.Where(item => string.Equals(ParkGraphUpsertProcessorLocalizedTextExtensions.NormalizeKey(item.Name), normalizedName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 1)
            {
                return matches[0];
            }
        }

        return null;
    }

    internal static ParkGraphUpsertChange BuildEntityChange(string entityType, string? entityId, string? entityKey, string displayName, string changeType, string matchedBy)
    {
        return new ParkGraphUpsertChange
        {
            EntityType = entityType,
            EntityId = entityId,
            EntityKey = entityKey,
            DisplayName = displayName,
            ChangeType = changeType,
            MatchedBy = matchedBy,
        };
    }

    internal static void AddChange(ParkGraphUpsertChange change, string field, object? oldValue, object? newValue)
    {
        string? oldText = ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(oldValue);
        string? newText = ParkGraphUpsertProcessorLocalizedTextExtensions.FormatValue(newValue);
        if (string.Equals(oldText, newText, StringComparison.Ordinal))
        {
            return;
        }

        change.Fields.Add(new ParkGraphUpsertFieldChange { Field = field, OldValue = oldText, NewValue = newText, });
    }

    internal static void FinalizeCounts(ParkGraphUpsertResult result)
    {
        result.Counts.Created = result.Changes.Count(change => string.Equals(change.ChangeType, "Created", StringComparison.Ordinal));
        result.Counts.Updated = result.Changes.Count(change => string.Equals(change.ChangeType, "Updated", StringComparison.Ordinal));
        result.Counts.Deleted = result.Changes.Count(change => string.Equals(change.ChangeType, "Deleted", StringComparison.Ordinal));
        result.Counts.Unchanged = result.Changes.Count(change => string.Equals(change.ChangeType, "Unchanged", StringComparison.Ordinal));
        result.Counts.Warnings = result.Warnings.Count;
        result.Counts.Errors = result.Errors.Count;
        result.CanApply = result.Errors.Count == 0;
    }
}
