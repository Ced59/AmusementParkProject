using System.Globalization;
using System.Text.Json;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ContextualBlocks.Commands;
using AmusementPark.Application.Features.ContextualBlocks.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ContextualBlocks.Handlers;

public sealed class PreviewContextualBlockJsonCommandHandler
    : ICommandHandler<PreviewContextualBlockJsonCommand, ApplicationResult<ContextualBlockPreviewResult>>
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

    private readonly IParkRepository parkRepository;

    public PreviewContextualBlockJsonCommandHandler(IParkRepository parkRepository)
    {
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<ContextualBlockPreviewResult>> HandleAsync(PreviewContextualBlockJsonCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.BlockType))
        {
            return ApplicationResult<ContextualBlockPreviewResult>.Failure(ApplicationErrors.Required("blockType"));
        }

        if (string.IsNullOrWhiteSpace(command.EntityId))
        {
            return ApplicationResult<ContextualBlockPreviewResult>.Failure(ApplicationErrors.Required("entityId"));
        }

        string blockType = command.BlockType.Trim();
        if (!ContextualBlockContracts.IsSupportedBlockType(blockType))
        {
            return ApplicationResult<ContextualBlockPreviewResult>.Failure(ContextualBlockApplicationErrors.UnsupportedBlockType(blockType));
        }

        string entityId = command.EntityId.Trim();
        Park? park = await this.parkRepository.GetByIdAsync(entityId, true, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ContextualBlockPreviewResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Park), entityId));
        }

        ContextualBlockPreviewResult result = CreateResult(blockType, park);
        if (command.Document.ValueKind != JsonValueKind.Object)
        {
            result.Errors.Add("Le document JSON racine doit etre un objet.");
            FinalizeResult(result, 0);
            return ApplicationResult<ContextualBlockPreviewResult>.Success(result);
        }

        JsonElement root = command.Document;
        ValidateAllowedProperties(root, RootAllowedProperties, "document", result);
        ValidateDocumentIdentity(root, blockType, entityId, result);

        if (!TryGetRequiredObject(root, "block", "document.block", result, out JsonElement block))
        {
            FinalizeResult(result, 0);
            return ApplicationResult<ContextualBlockPreviewResult>.Success(result);
        }

        if (string.Equals(blockType, ContextualBlockContracts.ParkDescriptionBlockType, StringComparison.Ordinal))
        {
            ValidateAllowedProperties(block, DescriptionBlockAllowedProperties, "block", result);
            ValidateParkId(block, "block.parkId", entityId, result);
            List<ContextualBlockPreviewChange> changes = PreviewDescriptionBlock(park, block, result);
            AddChanges(result, changes);
            FinalizeResult(result, ContextualBlockContracts.SupportedLanguageCodes.Length);
            return ApplicationResult<ContextualBlockPreviewResult>.Success(result);
        }

        ValidateAllowedProperties(block, PracticalBlockAllowedProperties, "block", result);
        ValidateParkId(block, "block.parkId", entityId, result);
        List<ContextualBlockPreviewChange> practicalChanges = PreviewPracticalBlock(park, block, result);
        AddChanges(result, practicalChanges);
        FinalizeResult(result, PracticalBlockAllowedProperties.Count - 1);
        return ApplicationResult<ContextualBlockPreviewResult>.Success(result);
    }

    private static ContextualBlockPreviewResult CreateResult(string blockType, Park park)
    {
        return new ContextualBlockPreviewResult
        {
            BlockType = blockType,
            Target = new ContextualBlockPreviewTarget
            {
                EntityType = nameof(Park),
                EntityId = park.Id,
                DisplayName = string.IsNullOrWhiteSpace(park.Name) ? park.Id : park.Name,
            },
        };
    }

    private static void ValidateDocumentIdentity(JsonElement root, string routeBlockType, string routeEntityId, ContextualBlockPreviewResult result)
    {
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
            if (!string.Equals(entityType, nameof(Park), StringComparison.Ordinal))
            {
                result.Errors.Add("target.entityType doit valoir 'Park'.");
            }

            string? entityId = ReadString(target, "entityId");
            if (!string.Equals(entityId, routeEntityId, StringComparison.Ordinal))
            {
                result.Errors.Add($"target.entityId doit correspondre a l'entite cible '{routeEntityId}'.");
            }
        }

        if (TryGetRequiredObject(root, "ids", "document.ids", result, out JsonElement ids))
        {
            HashSet<string> allowedIds = string.Equals(routeBlockType, ContextualBlockContracts.ParkPracticalBlockType, StringComparison.Ordinal)
                ? PracticalIdAllowedProperties
                : ParkOnlyIdAllowedProperties;
            ValidateAllowedProperties(ids, allowedIds, "ids", result);
            ValidateParkId(ids, "ids.parkId", routeEntityId, result);
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

    private static List<ContextualBlockPreviewChange> PreviewDescriptionBlock(Park park, JsonElement block, ContextualBlockPreviewResult result)
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

        Dictionary<string, string?> currentValuesByLanguage = BuildCurrentLocalizedValues(park.Descriptions);
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
                    park,
                    $"descriptions.{languageCode}.value",
                    languageCode,
                    oldValue,
                    newValue));
            }
        }

        return result.Errors.Count > 0 ? new List<ContextualBlockPreviewChange>() : changes;
    }

    private static List<ContextualBlockPreviewChange> PreviewPracticalBlock(Park park, JsonElement block, ContextualBlockPreviewResult result)
    {
        List<ContextualBlockPreviewChange> changes = new List<ContextualBlockPreviewChange>();

        PreviewStringField(park, block, "countryCode", park.CountryCode, result, changes);
        PreviewStringField(park, block, "city", park.City, result, changes);
        PreviewStringField(park, block, "street", park.Street, result, changes);
        PreviewStringField(park, block, "postalCode", park.PostalCode, result, changes);
        PreviewStringField(park, block, "websiteUrl", park.WebsiteUrl, result, changes);
        PreviewStringField(park, block, "founderId", park.FounderId, result, changes);
        PreviewStringField(park, block, "operatorId", park.OperatorId, result, changes);
        PreviewNumberField(park, block, "latitude", park.Position?.Latitude, result, changes);
        PreviewNumberField(park, block, "longitude", park.Position?.Longitude, result, changes);

        return result.Errors.Count > 0 ? new List<ContextualBlockPreviewChange>() : changes;
    }

    private static void PreviewStringField(
        Park park,
        JsonElement block,
        string fieldName,
        string? currentValue,
        ContextualBlockPreviewResult result,
        List<ContextualBlockPreviewChange> changes)
    {
        if (!block.TryGetProperty(fieldName, out JsonElement value))
        {
            return;
        }

        if (value.ValueKind != JsonValueKind.String && value.ValueKind != JsonValueKind.Null)
        {
            result.Errors.Add($"block.{fieldName} doit etre une chaine ou null.");
            return;
        }

        string? newValue = value.ValueKind == JsonValueKind.Null ? null : value.GetString();
        if (!string.Equals(currentValue, newValue, StringComparison.Ordinal))
        {
            changes.Add(BuildChange(park, fieldName, null, currentValue, newValue));
        }
    }

    private static void PreviewNumberField(
        Park park,
        JsonElement block,
        string fieldName,
        double? currentValue,
        ContextualBlockPreviewResult result,
        List<ContextualBlockPreviewChange> changes)
    {
        if (!block.TryGetProperty(fieldName, out JsonElement value))
        {
            return;
        }

        if (value.ValueKind != JsonValueKind.Number && value.ValueKind != JsonValueKind.Null)
        {
            result.Errors.Add($"block.{fieldName} doit etre un nombre ou null.");
            return;
        }

        double? newValue = value.ValueKind == JsonValueKind.Null ? null : value.GetDouble();
        if (currentValue != newValue)
        {
            changes.Add(BuildChange(park, fieldName, null, FormatNumber(currentValue), FormatNumber(newValue)));
        }
    }

    private static void AddChanges(ContextualBlockPreviewResult result, List<ContextualBlockPreviewChange> changes)
    {
        if (result.Errors.Count > 0)
        {
            return;
        }

        result.Changes.AddRange(changes);
    }

    private static ContextualBlockPreviewChange BuildChange(Park park, string fieldName, string? languageCode, string? oldValue, string? newValue)
    {
        return new ContextualBlockPreviewChange
        {
            EntityType = nameof(Park),
            EntityId = park.Id,
            DisplayName = string.IsNullOrWhiteSpace(park.Name) ? park.Id : park.Name,
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

    private static void ValidateParkId(JsonElement value, string path, string expectedParkId, ContextualBlockPreviewResult result)
    {
        string? parkId = ReadString(value, "parkId");
        if (!string.Equals(parkId, expectedParkId, StringComparison.Ordinal))
        {
            result.Errors.Add($"{path} doit correspondre a l'entite cible '{expectedParkId}'.");
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

    private static string? FormatNumber(double? value)
    {
        return value?.ToString("G17", CultureInfo.InvariantCulture);
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
