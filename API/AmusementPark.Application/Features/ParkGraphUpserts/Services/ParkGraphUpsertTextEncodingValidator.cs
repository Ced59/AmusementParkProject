using System.Text.Json;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

internal static class ParkGraphUpsertTextEncodingValidator
{
    private const int MaximumErrorCount = 100;
    private static readonly HashSet<string> PublicPlainTextPropertyNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "altText",
        "birthDate",
        "birthPlace",
        "caption",
        "city",
        "closingDateText",
        "credit",
        "credits",
        "deathDate",
        "description",
        "email",
        "label",
        "launchType",
        "legalName",
        "locationLabel",
        "materialType",
        "model",
        "name",
        "newName",
        "occupation",
        "openingDateText",
        "originalFileName",
        "phoneNumber",
        "postalCode",
        "previousName",
        "restraintType",
        "seatingType",
        "status",
        "street",
        "subtype",
        "title",
    };
    private static readonly HashSet<string> FlexibleHistoryTextPropertyNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "caption",
        "captions",
        "subtitle",
        "subtitles",
        "summary",
        "summaries",
        "text",
        "texts",
        "title",
        "titles",
    };

    internal static IReadOnlyCollection<string> FindErrors(JsonElement root)
    {
        List<string> errors = new List<string>();
        CollectErrors(root, "$", errors);
        return errors;
    }

    private static void CollectErrors(JsonElement element, string path, ICollection<string> errors)
    {
        if (errors.Count >= MaximumErrorCount)
        {
            return;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                CollectErrors(property.Value, $"{path}.{property.Name}", errors);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement item in element.EnumerateArray())
            {
                CollectErrors(item, $"{path}[{index}]", errors);
                index += 1;
            }

            return;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            return;
        }

        string? value = element.GetString();
        if (!IsPublicTextPath(path))
        {
            return;
        }

        if (DataCompletenessScoringRules.HasEncodedDisplayEntity(value))
        {
            errors.Add($"{path} contient une entité HTML de présentation. Utiliser directement le caractère Unicode attendu.");
            return;
        }

        if (!IsRichHtmlTextPath(path) && DataCompletenessScoringRules.HasHtmlEntity(value))
        {
            errors.Add($"{path} est un champ de texte brut qui contient une entité HTML. Utiliser directement le caractère Unicode attendu.");
        }
    }

    private static bool IsPublicTextPath(string path)
    {
        if (path.StartsWith("$.identity.", StringComparison.Ordinal))
        {
            return false;
        }

        int lastDotIndex = path.LastIndexOf('.');
        string propertyName = lastDotIndex >= 0 ? path[(lastDotIndex + 1)..] : path;
        return string.Equals(propertyName, "value", StringComparison.Ordinal)
            || PublicPlainTextPropertyNames.Contains(propertyName)
            || IsFlexibleHistoryTextPath(path, propertyName);
    }

    private static bool IsFlexibleHistoryTextPath(string path, string propertyName)
    {
        if (!path.StartsWith("$.history.", StringComparison.Ordinal)
            && !path.StartsWith("$.history[", StringComparison.Ordinal)
            && !path.StartsWith("$.historyEvents.", StringComparison.Ordinal)
            && !path.StartsWith("$.historyEvents[", StringComparison.Ordinal))
        {
            return false;
        }

        if (FlexibleHistoryTextPropertyNames.Contains(propertyName))
        {
            return true;
        }

        int lastDotIndex = path.LastIndexOf('.');
        if (lastDotIndex <= 0)
        {
            return false;
        }

        int parentDotIndex = path.LastIndexOf('.', lastDotIndex - 1);
        string parentPropertyName = parentDotIndex >= 0
            ? path[(parentDotIndex + 1)..lastDotIndex]
            : path[..lastDotIndex];
        return FlexibleHistoryTextPropertyNames.Contains(parentPropertyName);
    }

    private static bool IsRichHtmlTextPath(string path)
    {
        if (!path.EndsWith(".value", StringComparison.Ordinal))
        {
            return false;
        }

        return path.StartsWith("$.park.descriptions[", StringComparison.Ordinal)
            || (path.StartsWith("$.zones[", StringComparison.Ordinal) && path.Contains("].descriptions[", StringComparison.Ordinal))
            || (path.StartsWith("$.items[", StringComparison.Ordinal) && path.Contains("].descriptions[", StringComparison.Ordinal))
            || path.StartsWith("$.standaloneAttraction.descriptions[", StringComparison.Ordinal)
            || (path.StartsWith("$.standaloneAttractions[", StringComparison.Ordinal) && path.Contains("].descriptions[", StringComparison.Ordinal))
            || path.Contains(".biography[", StringComparison.Ordinal)
            || (path.Contains(".references.operators[", StringComparison.Ordinal) && path.Contains("].description[", StringComparison.Ordinal));
    }
}
