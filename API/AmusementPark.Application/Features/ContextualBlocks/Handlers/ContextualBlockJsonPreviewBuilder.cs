using System.Text.Json;
using AmusementPark.Application.Features.ContextualBlocks.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ContextualBlocks.Handlers;

internal static class ContextualBlockJsonPreviewBuilder
{
    private static readonly HashSet<string> RootAllowedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "documentType",
        "schemaVersion",
        "blockType",
        "target",
        "ids",
        "block",
        "metadata",
    };
    private static readonly HashSet<string> TargetAllowedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "entityType",
        "entityId",
    };
    private static readonly HashSet<string> MetadataAllowedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "source",
        "exportedAtUtc",
    };
    private static readonly HashSet<string> DescriptionBlockAllowedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "parkId",
        "descriptions",
    };
    private static readonly HashSet<string> LocationBlockAllowedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "parkId",
        "latitude",
        "longitude",
    };

    private static readonly HashSet<string> ParkItemDescriptionBlockAllowedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "parkId",
        "parkItemId",
        "zoneId",
        "descriptions",
    };

    private static readonly HashSet<string> ParkItemLocationBlockAllowedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "parkId",
        "parkItemId",
        "zoneId",
        "latitude",
        "longitude",
    };

    private static readonly HashSet<string> LocalizedTextAllowedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "languageCode",
        "value",
    };

    private static readonly HashSet<string> PracticalBlockAllowedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "parkId",
        "countryCode",
        "city",
        "street",
        "postalCode",
        "websiteUrl",
        "founderId",
        "operatorId",
        "latitude",
        "longitude",
    };

    private static readonly HashSet<string> ParkOnlyIdAllowedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "parkId",
    };

    private static readonly HashSet<string> PracticalIdAllowedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "parkId",
        "founderId",
        "operatorId",
    };

    private static readonly HashSet<string> ParkItemDescriptionIdAllowedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "parkId",
        "parkItemId",
        "zoneId",
    };

    public static ContextualBlockPreviewResult PreviewParkItemDescription(string blockType, ParkItem item, JsonElement document)
    {
        ContextualBlockPreviewResult result = CreateResult(blockType, nameof(ParkItem), item.Id, item.Name);
        PreviewJsonDocument(document, blockType, item.Id, nameof(ParkItem), ParkItemDescriptionIdAllowedProperties, result, out JsonElement block, out JsonElement ids);

        if (block.ValueKind != JsonValueKind.Object)
        {
            FinalizeResult(result, 0);
            return result;
        }

        ValidateAllowedProperties(block, ParkItemDescriptionBlockAllowedProperties, "block", result);
        ValidateParkItemAttachment(ids, "ids", item, result);
        ValidateParkItemAttachment(block, "block", item, result);

        List<ContextualBlockPreviewChange> changes = PreviewDescriptionBlock(
            item.Descriptions,
            nameof(ParkItem),
            item.Id,
            item.Name,
            block,
            result);

        AddChanges(result, changes);
        FinalizeResult(result, ContextualBlockContracts.SupportedLanguageCodes.Length);
        return result;
    }

    public static ContextualBlockPreviewResult PreviewParkItemLocation(string blockType, ParkItem item, JsonElement document)
    {
        ContextualBlockPreviewResult result = CreateResult(blockType, nameof(ParkItem), item.Id, item.Name);
        PreviewJsonDocument(document, blockType, item.Id, nameof(ParkItem), ParkItemDescriptionIdAllowedProperties, result, out JsonElement block, out JsonElement ids);

        if (block.ValueKind != JsonValueKind.Object)
        {
            FinalizeResult(result, 0);
            return result;
        }

        ValidateAllowedProperties(block, ParkItemLocationBlockAllowedProperties, "block", result);
        ValidateParkItemAttachment(ids, "ids", item, result);
        ValidateParkItemAttachment(block, "block", item, result);

        List<ContextualBlockPreviewChange> changes = ContextualBlockLocationPreviewBuilder.PreviewLocationBlock(
            nameof(ParkItem),
            item.Id,
            item.Name,
            item.Position?.Latitude,
            item.Position?.Longitude,
            block,
            result);

        AddChanges(result, changes);
        FinalizeResult(result, 2);
        return result;
    }

    public static ContextualBlockPreviewResult PreviewParkDescription(string blockType, Park park, JsonElement document)
    {
        ContextualBlockPreviewResult result = CreateResult(blockType, nameof(Park), park.Id, park.Name);
        PreviewJsonDocument(document, blockType, park.Id, nameof(Park), ParkOnlyIdAllowedProperties, result, out JsonElement block, out JsonElement ids);

        if (block.ValueKind != JsonValueKind.Object)
        {
            FinalizeResult(result, 0);
            return result;
        }

        ValidateAllowedProperties(block, DescriptionBlockAllowedProperties, "block", result);
        ValidateParkId(ids, "ids.parkId", park.Id, result);
        ValidateParkId(block, "block.parkId", park.Id, result);

        List<ContextualBlockPreviewChange> changes = PreviewDescriptionBlock(
            park.Descriptions,
            nameof(Park),
            park.Id,
            park.Name,
            block,
            result);

        AddChanges(result, changes);
        FinalizeResult(result, ContextualBlockContracts.SupportedLanguageCodes.Length);
        return result;
    }

    public static ContextualBlockPreviewResult PreviewParkLocation(string blockType, Park park, JsonElement document)
    {
        ContextualBlockPreviewResult result = CreateResult(blockType, nameof(Park), park.Id, park.Name);
        PreviewJsonDocument(document, blockType, park.Id, nameof(Park), ParkOnlyIdAllowedProperties, result, out JsonElement block, out JsonElement ids);

        if (block.ValueKind != JsonValueKind.Object)
        {
            FinalizeResult(result, 0);
            return result;
        }

        ValidateAllowedProperties(block, LocationBlockAllowedProperties, "block", result);
        ValidateParkId(ids, "ids.parkId", park.Id, result);
        ValidateParkId(block, "block.parkId", park.Id, result);

        List<ContextualBlockPreviewChange> changes = ContextualBlockLocationPreviewBuilder.PreviewLocationBlock(
            nameof(Park),
            park.Id,
            park.Name,
            park.Position?.Latitude,
            park.Position?.Longitude,
            block,
            result);

        AddChanges(result, changes);
        FinalizeResult(result, 2);
        return result;
    }

    public static ContextualBlockPreviewResult PreviewParkPractical(string blockType, Park park, JsonElement document)
    {
        ContextualBlockPreviewResult result = CreateResult(blockType, nameof(Park), park.Id, park.Name);
        PreviewJsonDocument(document, blockType, park.Id, nameof(Park), PracticalIdAllowedProperties, result, out JsonElement block, out JsonElement ids);

        if (block.ValueKind != JsonValueKind.Object)
        {
            FinalizeResult(result, 0);
            return result;
        }

        ValidateAllowedProperties(block, PracticalBlockAllowedProperties, "block", result);
        ValidateParkId(ids, "ids.parkId", park.Id, result);
        ValidateParkId(block, "block.parkId", park.Id, result);

        List<ContextualBlockPreviewChange> changes = ContextualBlockPracticalPreviewBuilder.PreviewPracticalBlock(park, block, result);
        AddChanges(result, changes);
        FinalizeResult(result, PracticalBlockAllowedProperties.Count - 1);
        return result;
    }

    private static ContextualBlockPreviewResult CreateResult(string blockType, string entityType, string entityId, string? displayName)
    {
        return new ContextualBlockPreviewResult
        {
            BlockType = blockType,
            Target = new ContextualBlockPreviewTarget
            {
                EntityType = entityType,
                EntityId = entityId,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? entityId : displayName,
            },
        };
    }

    private static void PreviewJsonDocument(
        JsonElement document,
        string routeBlockType,
        string routeEntityId,
        string expectedEntityType,
        HashSet<string> allowedIds,
        ContextualBlockPreviewResult result,
        out JsonElement block,
        out JsonElement ids)
    {
        block = default;
        ids = default;

        if (document.ValueKind != JsonValueKind.Object)
        {
            result.Errors.Add("Le document JSON racine doit etre un objet.");
            return;
        }

        ValidateAllowedProperties(document, RootAllowedProperties, "document", result);
        ValidateDocumentIdentity(document, routeBlockType, routeEntityId, expectedEntityType, allowedIds, result, out ids);
        TryGetRequiredObject(document, "block", "document.block", result, out block);
    }

    private static void ValidateDocumentIdentity(
        JsonElement root,
        string routeBlockType,
        string routeEntityId,
        string expectedEntityType,
        HashSet<string> allowedIds,
        ContextualBlockPreviewResult result,
        out JsonElement ids)
    {
        ids = default;

        string? documentType = ReadString(root, "documentType");
        if (!string.Equals(documentType, ContextualBlockContracts.DocumentType, StringComparison.Ordinal))
        {
            result.Errors.Add($"document.documentType doit valoir '{ContextualBlockContracts.DocumentType}'.");
        }

        string? blockType = ReadString(root, "blockType");
        if (!string.Equals(blockType, routeBlockType, StringComparison.Ordinal))
        {
            result.Errors.Add($"document.blockType doit correspondre au bloc cible '{routeBlockType}'.");
        }

        if (TryGetRequiredObject(root, "target", "document.target", result, out JsonElement target))
        {
            ValidateAllowedProperties(target, TargetAllowedProperties, "target", result);
            string? entityType = ReadString(target, "entityType");
            if (!string.Equals(entityType, expectedEntityType, StringComparison.Ordinal))
            {
                result.Errors.Add($"target.entityType doit valoir '{expectedEntityType}'.");
            }

            string? entityId = ReadString(target, "entityId");
            if (!string.Equals(entityId, routeEntityId, StringComparison.Ordinal))
            {
                result.Errors.Add($"target.entityId doit correspondre a l'entite cible '{routeEntityId}'.");
            }
        }

        if (TryGetRequiredObject(root, "ids", "document.ids", result, out JsonElement documentIds))
        {
            ids = documentIds;
            ValidateAllowedProperties(ids, allowedIds, "ids", result);
        }

        if (root.TryGetProperty("metadata", out JsonElement metadata))
        {
            if (metadata.ValueKind == JsonValueKind.Object)
            {
                ValidateAllowedProperties(metadata, MetadataAllowedProperties, "metadata", result);
            }
            else
            {
                result.Errors.Add("metadata doit etre un objet quand il est fourni.");
            }
        }
    }

    private static List<ContextualBlockPreviewChange> PreviewDescriptionBlock(
        IReadOnlyCollection<LocalizedText> currentDescriptions,
        string entityType,
        string entityId,
        string? displayName,
        JsonElement block,
        ContextualBlockPreviewResult result)
    {
        List<ContextualBlockPreviewChange> changes = new List<ContextualBlockPreviewChange>();
        if (!block.TryGetProperty("descriptions", out JsonElement descriptions) || descriptions.ValueKind != JsonValueKind.Array)
        {
            result.Errors.Add("block.descriptions doit etre un tableau.");
            return changes;
        }

        Dictionary<string, string?> draftValuesByLanguage = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (JsonElement description in descriptions.EnumerateArray())
        {
            if (description.ValueKind != JsonValueKind.Object)
            {
                result.Errors.Add("Chaque entree block.descriptions doit etre un objet.");
                continue;
            }

            ValidateAllowedProperties(description, LocalizedTextAllowedProperties, "block.descriptions[]", result);
            string languageCode = NormalizeLanguageCode(ReadString(description, "languageCode"));
            if (languageCode.Length == 0)
            {
                result.Errors.Add("Chaque entree block.descriptions doit fournir languageCode.");
                continue;
            }

            if (!ContextualBlockContracts.SupportedLanguageCodes.Contains(languageCode, StringComparer.Ordinal))
            {
                result.Errors.Add($"Langue '{languageCode}' non autorisee dans block.descriptions.");
                continue;
            }

            if (draftValuesByLanguage.ContainsKey(languageCode))
            {
                result.Errors.Add($"Langue '{languageCode}' dupliquee dans block.descriptions.");
                continue;
            }

            if (!description.TryGetProperty("value", out JsonElement valueElement))
            {
                result.Errors.Add($"Langue '{languageCode}' : le champ value est requis.");
                continue;
            }

            if (valueElement.ValueKind != JsonValueKind.String && valueElement.ValueKind != JsonValueKind.Null)
            {
                result.Errors.Add($"Langue '{languageCode}' : value doit etre une chaine ou null.");
                continue;
            }

            draftValuesByLanguage[languageCode] = valueElement.ValueKind == JsonValueKind.Null ? null : valueElement.GetString();
        }

        Dictionary<string, string?> currentValuesByLanguage = BuildCurrentLocalizedValues(currentDescriptions);
        foreach (string languageCode in ContextualBlockContracts.SupportedLanguageCodes)
        {
            if (!draftValuesByLanguage.ContainsKey(languageCode))
            {
                result.Errors.Add($"Langue '{languageCode}' manquante dans block.descriptions.");
                continue;
            }

            string? oldValue = currentValuesByLanguage.TryGetValue(languageCode, out string? currentValue) ? currentValue : null;
            string? newValue = draftValuesByLanguage[languageCode];
            if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                changes.Add(BuildChange(
                    entityType,
                    entityId,
                    displayName,
                    $"descriptions.{languageCode}.value",
                    languageCode,
                    oldValue,
                    newValue));
            }
        }

        return result.Errors.Count > 0 ? new List<ContextualBlockPreviewChange>() : changes;
    }

    private static void AddChanges(ContextualBlockPreviewResult result, List<ContextualBlockPreviewChange> changes)
    {
        if (result.Errors.Count > 0)
        {
            return;
        }

        result.Changes.AddRange(changes);
    }

    private static ContextualBlockPreviewChange BuildChange(
        string entityType,
        string entityId,
        string? displayName,
        string fieldName,
        string? languageCode,
        string? oldValue,
        string? newValue)
    {
        return new ContextualBlockPreviewChange
        {
            EntityType = entityType,
            EntityId = entityId,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? entityId : displayName,
            Field = fieldName,
            LanguageCode = languageCode,
            ChangeType = "Updated",
            OldValue = oldValue,
            NewValue = newValue,
        };
    }

    private static bool TryGetRequiredObject(JsonElement owner, string propertyName, string path, ContextualBlockPreviewResult result, out JsonElement value)
    {
        if (!owner.TryGetProperty(propertyName, out value))
        {
            result.Errors.Add($"{path} est requis.");
            return false;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            result.Errors.Add($"{path} doit etre un objet.");
            return false;
        }

        return true;
    }

    private static void ValidateAllowedProperties(JsonElement value, HashSet<string> allowedProperties, string path, ContextualBlockPreviewResult result)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (!allowedProperties.Contains(property.Name))
            {
                result.Errors.Add($"Champ hors perimetre '{path}.{property.Name}' interdit pour ce bloc.");
            }
        }
    }

    private static void ValidateParkItemAttachment(JsonElement value, string path, ParkItem expectedItem, ContextualBlockPreviewResult result)
    {
        ValidateStringEquals(value, "parkId", $"{path}.parkId", expectedItem.ParkId, result);
        ValidateStringEquals(value, "parkItemId", $"{path}.parkItemId", expectedItem.Id, result);

        if (value.TryGetProperty("zoneId", out JsonElement _))
        {
            ValidateOptionalStringEquals(value, "zoneId", $"{path}.zoneId", expectedItem.ZoneId, result);
        }
    }

    private static void ValidateParkId(JsonElement value, string path, string expectedParkId, ContextualBlockPreviewResult result)
    {
        ValidateStringEquals(value, "parkId", path, expectedParkId, result);
    }

    private static void ValidateStringEquals(JsonElement value, string propertyName, string path, string expectedValue, ContextualBlockPreviewResult result)
    {
        string? actualValue = ReadString(value, propertyName);
        if (!string.Equals(actualValue, expectedValue, StringComparison.Ordinal))
        {
            result.Errors.Add($"{path} doit correspondre a l'entite cible '{expectedValue}'.");
        }
    }

    private static void ValidateOptionalStringEquals(JsonElement value, string propertyName, string path, string? expectedValue, ContextualBlockPreviewResult result)
    {
        string? actualValue = ReadString(value, propertyName);
        if (!string.Equals(actualValue, expectedValue, StringComparison.Ordinal))
        {
            result.Errors.Add($"{path} doit correspondre a l'entite cible '{expectedValue ?? string.Empty}'.");
        }
    }

    private static Dictionary<string, string?> BuildCurrentLocalizedValues(IReadOnlyCollection<LocalizedText> values)
    {
        Dictionary<string, string?> valuesByLanguage = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (LocalizedText value in values)
        {
            string languageCode = NormalizeLanguageCode(value.LanguageCode);
            if (languageCode.Length > 0 && !valuesByLanguage.ContainsKey(languageCode))
            {
                valuesByLanguage[languageCode] = value.Value;
            }
        }

        return valuesByLanguage;
    }

    private static string? ReadString(JsonElement? element, string propertyName)
    {
        if (element is null || element.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.Value.TryGetProperty(propertyName, out JsonElement property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static string NormalizeLanguageCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private static void FinalizeResult(ContextualBlockPreviewResult result, int inspectedFieldCount)
    {
        result.CanApply = result.Errors.Count == 0;
        result.Counts.Updated = result.Changes.Count;
        result.Counts.Unchanged = Math.Max(0, inspectedFieldCount - result.Changes.Count);
        result.Counts.Warnings = result.Warnings.Count;
        result.Counts.Errors = result.Errors.Count;
    }
}
