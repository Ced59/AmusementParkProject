using System.Globalization;
using System.Text;

namespace AmusementPark.Application.Features.AttractionAccessConditionTypes;

/// <summary>
/// Normalise les clés stables des types de conditions d'accès.
/// </summary>
public static class AttractionAccessConditionTypeKeyNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string trimmed = value.Trim();
        StringBuilder builder = new StringBuilder(trimmed.Length + 8);
        bool previousWasSeparator = false;
        char previousInput = '\0';

        foreach (char input in trimmed.Normalize(NormalizationForm.FormD))
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(input);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(input))
            {
                if (builder.Length > 0 && char.IsUpper(input) && char.IsLetter(previousInput) && char.IsLower(previousInput) && !previousWasSeparator)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(input));
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }

            previousInput = input;
        }

        return builder.ToString().Trim('-');
    }
}
