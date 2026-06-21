using System.Globalization;
using System.Text;

namespace AmusementPark.Core.Domain.Parks;

public static class ParkItemStatusNormalizer
{
    public const string ClosedDefinitively = "ClosedDefinitively";

    public static string? Normalize(string? value)
    {
        string normalized = NormalizeToken(value);
        if (normalized.Length == 0)
        {
            return null;
        }

        return normalized switch
        {
            "operating" or "open" or "opened" or "enfonctionnement" => "Operating",
            "underconstruction" or "construction" => "UnderConstruction",
            "temporarilyclosed" or "temporaryclosed" or "closedtemporarily" => "TemporarilyClosed",
            "closeddefinitively" or "permanentlyclosed" or "definitivelyclosed" or "fermedefinitivement" => ClosedDefinitively,
            "removed" or "dismantled" => "Removed",
            "planned" or "announced" => "Planned",
            "unknown" => "Unknown",
            _ => value?.Trim(),
        };
    }

    public static bool IsClosedDefinitively(string? value)
    {
        return string.Equals(Normalize(value), ClosedDefinitively, StringComparison.Ordinal);
    }

    private static string NormalizeToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(decomposed.Length);
        foreach (char character in decomposed)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark || character == '_' || character == '-' || character == ' ' || character == '\'')
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
