using System.Text.Json;
using AmusementPark.Application.Features.AttractionAccessConditionTypes;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.LocalizedContent.Handlers;

public sealed partial class ApplyLocalizedContentJsonCommandHandler
{
    private static bool TryParsePatch(string json, out LocalizedContentPatch? patch)
    {
        patch = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            JsonElement root = document.RootElement;
            bool replaceExisting = TryReadReplaceExisting(root);
            Dictionary<string, IReadOnlyCollection<LocalizedText>> fields = new Dictionary<string, IReadOnlyCollection<LocalizedText>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, JsonElement> rawFields = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            List<AccessConditionPatch> accessConditions = new List<AccessConditionPatch>();

            foreach (JsonProperty property in root.EnumerateObject())
            {
                string normalizedPropertyName = NormalizeField(property.Name);
                if (normalizedPropertyName is "mode" or "replace" or "replaceexisting" or "entitytype" or "entityid")
                {
                    continue;
                }

                if (normalizedPropertyName is "fields" && property.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty fieldProperty in property.Value.EnumerateObject())
                    {
                        if (!AddLocalizedField(fields, fieldProperty.Name, fieldProperty.Value))
                        {
                            rawFields[fieldProperty.Name] = fieldProperty.Value.Clone();
                        }
                    }

                    continue;
                }

                if (normalizedPropertyName is "accessconditions" or "attractionaccessconditions")
                {
                    accessConditions.AddRange(ReadAccessConditionPatches(property.Value));
                    continue;
                }

                if (!AddLocalizedField(fields, property.Name, property.Value))
                {
                    rawFields[property.Name] = property.Value.Clone();
                }
            }

            patch = new LocalizedContentPatch(fields, rawFields, accessConditions, replaceExisting);
            return fields.Count > 0 || rawFields.Count > 0 || accessConditions.Count > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool AddLocalizedField(Dictionary<string, IReadOnlyCollection<LocalizedText>> fields, string fieldName, JsonElement value)
    {
        IReadOnlyCollection<LocalizedText> localizedValues = ReadLocalizedTexts(value);
        if (localizedValues.Count == 0)
        {
            return false;
        }

        fields[fieldName] = localizedValues;
        return true;
    }

    private static IReadOnlyCollection<LocalizedText> ReadLocalizedTexts(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray()
                .Select(ReadLocalizedText)
                .Where(static item => item is not null)
                .Select(static item => item!)
                .ToList();
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            List<LocalizedText> values = new List<LocalizedText>();
            foreach (JsonProperty property in value.EnumerateObject())
            {
                if (!IsSupportedLanguageCode(property.Name))
                {
                    return Array.Empty<LocalizedText>();
                }

                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    string? text = property.Value.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        values.Add(new LocalizedText(NormalizeLanguageCode(property.Name), text.Trim()));
                    }
                }
            }

            return values;
        }

        return Array.Empty<LocalizedText>();
    }

    private static LocalizedText? ReadLocalizedText(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? languageCode = null;
        string? text = null;
        foreach (JsonProperty property in value.EnumerateObject())
        {
            string normalizedName = NormalizeField(property.Name);
            if (normalizedName is "languagecode" or "language" or "lang")
            {
                languageCode = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
            }
            else if (normalizedName is "value" or "text" or "html")
            {
                text = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : null;
            }
        }

        if (string.IsNullOrWhiteSpace(languageCode) || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return new LocalizedText(NormalizeLanguageCode(languageCode), text.Trim());
    }

    private static IReadOnlyCollection<AccessConditionPatch> ReadAccessConditionPatches(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            AccessConditionPatch? singlePatch = ReadAccessConditionPatch(value);
            return singlePatch is null ? Array.Empty<AccessConditionPatch>() : new[] { singlePatch };
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<AccessConditionPatch>();
        }

        List<AccessConditionPatch> patches = new List<AccessConditionPatch>();
        foreach (JsonElement item in value.EnumerateArray())
        {
            AccessConditionPatch? patch = ReadAccessConditionPatch(item);
            if (patch is not null)
            {
                patches.Add(patch);
            }
        }

        return patches;
    }

    private static AccessConditionPatch? ReadAccessConditionPatch(JsonElement item)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? rawType = null;
        AttractionAccessConditionType type = AttractionAccessConditionType.Custom;
        bool hasExplicitType = false;
        bool parsedKnownType = false;
        string? typeKey = null;
        IReadOnlyCollection<LocalizedText> typeLabel = Array.Empty<LocalizedText>();
        int? displayOrder = null;
        double? numericValue = null;
        AttractionAccessConditionUnit? unit = null;
        bool? requiresAccompaniment = null;
        int? minimumCompanionAge = null;
        bool? isCustom = null;
        IReadOnlyCollection<LocalizedText> label = Array.Empty<LocalizedText>();
        IReadOnlyCollection<LocalizedText> description = Array.Empty<LocalizedText>();

        foreach (JsonProperty property in item.EnumerateObject())
        {
            string normalizedName = NormalizeField(property.Name);
            if (normalizedName is "type" or "conditiontype")
            {
                rawType = ReadString(property.Value);
                if (!string.IsNullOrWhiteSpace(rawType))
                {
                    hasExplicitType = true;
                    parsedKnownType = Enum.TryParse(rawType, true, out type);
                    if (!parsedKnownType)
                    {
                        type = AttractionAccessConditionType.Custom;
                    }
                }
            }
            else if (normalizedName is "customtypekey" or "customkey" or "typekey" or "key")
            {
                typeKey = AttractionAccessConditionTypeKeyNormalizer.Normalize(ReadString(property.Value));
            }
            else if (normalizedName is "customtypelabel" or "customtypelabels" or "typelabel" or "typelabels")
            {
                typeLabel = ReadLocalizedTexts(property.Value);
            }
            else if (normalizedName is "displayorder" or "order")
            {
                displayOrder = ReadInt32(property.Value);
            }
            else if (normalizedName is "value" or "numericvalue")
            {
                numericValue = ReadDouble(property.Value);
            }
            else if (normalizedName is "unit")
            {
                unit = ReadUnit(property.Value);
            }
            else if (normalizedName is "requiresaccompaniment" or "accompanimentrequired" or "requiresadult" or "adultrequired")
            {
                requiresAccompaniment = ReadBoolean(property.Value);
            }
            else if (normalizedName is "minimumcompanionage" or "mincompanionage" or "companionminage" or "minimumaccompanyingage")
            {
                minimumCompanionAge = ReadInt32(property.Value);
            }
            else if (normalizedName is "iscustom")
            {
                isCustom = ReadBoolean(property.Value);
            }
            else if (normalizedName is "label" or "labels")
            {
                label = ReadLocalizedTexts(property.Value);
            }
            else if (normalizedName is "description" or "descriptions")
            {
                description = ReadLocalizedTexts(property.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(rawType))
        {
            typeKey ??= parsedKnownType
                ? AttractionAccessConditionTypeKeyNormalizer.Normalize(type.ToString())
                : AttractionAccessConditionTypeKeyNormalizer.Normalize(rawType);
        }

        if (!string.IsNullOrWhiteSpace(typeKey))
        {
            hasExplicitType = true;
        }

        bool hasAnyPayload = hasExplicitType ||
                             displayOrder.HasValue ||
                             numericValue.HasValue ||
                             unit.HasValue ||
                             requiresAccompaniment.HasValue ||
                             minimumCompanionAge.HasValue ||
                             isCustom.HasValue ||
                             typeLabel.Count > 0 ||
                             label.Count > 0 ||
                             description.Count > 0;

        if (!hasAnyPayload)
        {
            return null;
        }

        return new AccessConditionPatch(
            type,
            hasExplicitType,
            rawType,
            typeKey,
            typeLabel,
            displayOrder,
            numericValue,
            unit,
            requiresAccompaniment,
            minimumCompanionAge,
            isCustom,
            label,
            description);
    }

    private static string? ReadString(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static int? ReadInt32(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int parsed))
        {
            return parsed;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out parsed))
        {
            return parsed;
        }

        return null;
    }

    private static double? ReadDouble(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double parsed))
        {
            return parsed;
        }

        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool? ReadBoolean(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (value.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        if (value.ValueKind == JsonValueKind.String && bool.TryParse(value.GetString(), out bool parsed))
        {
            return parsed;
        }

        return null;
    }

    private static AttractionAccessConditionUnit? ReadUnit(JsonElement value)
    {
        string? unitValue = ReadString(value);
        return !string.IsNullOrWhiteSpace(unitValue) && Enum.TryParse(unitValue, true, out AttractionAccessConditionUnit parsed)
            ? parsed
            : null;
    }

    private static bool TryReadReplaceExisting(JsonElement root)
    {
        if (root.TryGetProperty("replaceExisting", out JsonElement replaceExisting) && replaceExisting.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return replaceExisting.GetBoolean();
        }

        if (root.TryGetProperty("replace", out JsonElement replace) && replace.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return replace.GetBoolean();
        }

        if (root.TryGetProperty("mode", out JsonElement mode) && mode.ValueKind == JsonValueKind.String)
        {
            return string.Equals(mode.GetString(), "replace", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool IsSupportedLanguageCode(string? languageCode)
    {
        string normalized = NormalizeLanguageCode(languageCode);
        return normalized is "fr" or "en" or "es" or "de" or "it" or "pl" or "nl" or "pt";
    }

    private static string NormalizeField(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }

    private static string? NormalizeCustomTypeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        List<char> chars = new List<char>();
        bool previousWasSeparator = false;
        foreach (char character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                chars.Add(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                chars.Add('-');
                previousWasSeparator = true;
            }
        }

        string key = new string(chars.ToArray()).Trim('-');
        return string.IsNullOrWhiteSpace(key) ? null : key;
    }

    private static string NormalizeLanguageCode(string? languageCode)
    {
        return string.IsNullOrWhiteSpace(languageCode) ? string.Empty : languageCode.Trim().ToLowerInvariant();
    }

    private sealed record LocalizedContentPatch(
        IReadOnlyDictionary<string, IReadOnlyCollection<LocalizedText>> Fields,
        IReadOnlyDictionary<string, JsonElement> RawFields,
        IReadOnlyCollection<AccessConditionPatch> AccessConditions,
        bool ReplaceExisting);

    private sealed record AccessConditionPatch(
        AttractionAccessConditionType Type,
        bool HasExplicitType,
        string? RawType,
        string? TypeKey,
        IReadOnlyCollection<LocalizedText> TypeLabel,
        int? DisplayOrder,
        double? Value,
        AttractionAccessConditionUnit? Unit,
        bool? RequiresAccompaniment,
        int? MinimumCompanionAge,
        bool? IsCustom,
        IReadOnlyCollection<LocalizedText> Label,
        IReadOnlyCollection<LocalizedText> Description)
    {
        public bool CanCreate => HasExplicitType || !string.IsNullOrWhiteSpace(TypeKey);

        public string Selector
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(TypeKey))
                {
                    return $"typeKey:{TypeKey}";
                }

                if (HasExplicitType)
                {
                    return Type.ToString();
                }

                return DisplayOrder.HasValue ? $"displayOrder:{DisplayOrder}" : "unspecified";
            }
        }
    }
}
