using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private static List<AttractionAccessCondition> ReadAccessConditions(JsonElement? array)
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
                Type = ReadEnum(item, "type", AttractionAccessConditionType.Custom),
                TypeKey = ReadString(item, "typeKey"),
                IsCustom = ReadBool(item, "isCustom"),
                CustomTypeKey = ReadString(item, "customTypeKey"),
                CustomTypeLabel = ReadLocalizedTexts(GetArray(item, "customTypeLabel")),
                Value = ReadDouble(item, "value"),
                Unit = ReadEnumNullable<AttractionAccessConditionUnit>(item, "unit"),
                RequiresAccompaniment = ReadBool(item, "requiresAccompaniment"),
                MinimumCompanionAge = ReadInt(item, "minimumCompanionAge"),
                Label = ReadLocalizedTexts(GetArray(item, "label")),
                Description = ReadLocalizedTexts(GetArray(item, "description")),
                DisplayOrder = ReadInt(item, "displayOrder"),
            };
            conditions.Add(condition);
        }

        return conditions;
    }
    private static void ResolveImageOwner(JsonElement patch, Park park, Dictionary<string, string> itemKeys, string? ownerTypeText, string? ownerId, out ImageOwnerType ownerType, out string? resolvedOwnerId)
    {
        ownerType = ReadEnumFromText(ownerTypeText, ImageOwnerType.Park);
        resolvedOwnerId = NormalizeString(ownerId);
        string? ownerKey = ReadString(patch, "ownerKey");
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
            string normalizedItemNameKey = $"item:{NormalizeKey(ownerKey)}";
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
    private static T? FindByIdOrName<T>(IReadOnlyCollection<T> entities, string? id, string? name, Func<T, string> idSelector, Func<T, string> nameSelector)
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
            string normalizedName = NormalizeKey(name);
            return entities.FirstOrDefault(entity => string.Equals(NormalizeKey(nameSelector(entity)), normalizedName, StringComparison.OrdinalIgnoreCase));
        }

        return default;
    }
    private static ParkZone? FindZone(IReadOnlyCollection<ParkZone> zones, string? id, string? slug, string? name)
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
            string normalizedName = NormalizeKey(name);
            return zones.FirstOrDefault(zone => string.Equals(NormalizeKey(zone.Name), normalizedName, StringComparison.OrdinalIgnoreCase)
                || zone.Names.Any(localized => string.Equals(NormalizeKey(localized.Value), normalizedName, StringComparison.OrdinalIgnoreCase)));
        }

        return null;
    }
    private static ParkItem? FindItem(IReadOnlyCollection<ParkItem> items, string? id, string? name, string? externalSource, string? externalId)
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
            ParkItem? byExternalId = items.FirstOrDefault(item => string.Equals(item.AttractionDetails?.ExternalSource, externalSource, StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.AttractionDetails?.ExternalId, externalId, StringComparison.OrdinalIgnoreCase));
            if (byExternalId is not null)
            {
                return byExternalId;
            }
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            string normalizedName = NormalizeKey(name);
            List<ParkItem> matches = items
                .Where(item => string.Equals(NormalizeKey(item.Name), normalizedName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count == 1)
            {
                return matches[0];
            }
        }

        return null;
    }
    private static ParkGraphUpsertChange BuildEntityChange(string entityType, string? entityId, string? entityKey, string displayName, string changeType, string matchedBy)
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
    private static void AddChange(ParkGraphUpsertChange change, string field, object? oldValue, object? newValue)
    {
        string? oldText = FormatValue(oldValue);
        string? newText = FormatValue(newValue);
        if (string.Equals(oldText, newText, StringComparison.Ordinal))
        {
            return;
        }

        change.Fields.Add(new ParkGraphUpsertFieldChange
        {
            Field = field,
            OldValue = oldText,
            NewValue = newText,
        });
    }
    private static void FinalizeCounts(ParkGraphUpsertResult result)
    {
        result.Counts.Created = result.Changes.Count(change => string.Equals(change.ChangeType, "Created", StringComparison.Ordinal));
        result.Counts.Updated = result.Changes.Count(change => string.Equals(change.ChangeType, "Updated", StringComparison.Ordinal));
        result.Counts.Unchanged = result.Changes.Count(change => string.Equals(change.ChangeType, "Unchanged", StringComparison.Ordinal));
        result.Counts.Warnings = result.Warnings.Count;
        result.Counts.Errors = result.Errors.Count;
        result.CanApply = result.Errors.Count == 0;
    }
}
