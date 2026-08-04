using System.Globalization;
using System.Text;

namespace AmusementPark.Core.Domain.Parks;

public static class ParkItemStatusNormalizer
{
    public const string Operating = "Operating";

    public const string UnderConstruction = "UnderConstruction";

    public const string TemporarilyClosed = "TemporarilyClosed";

    public const string ClosedDefinitively = "ClosedDefinitively";

    public const string Removed = "Removed";

    public const string Planned = "Planned";

    public const string Unknown = "Unknown";

    public static string? Normalize(string? value)
    {
        string normalized = NormalizeToken(value);
        if (normalized.Length == 0)
        {
            return null;
        }

        return normalized switch
        {
            "operating" or "open" or "opened" or "enfonctionnement" => Operating,
            "underconstruction" or "construction" => UnderConstruction,
            "temporarilyclosed" or "temporaryclosed" or "closedtemporarily" => TemporarilyClosed,
            "closeddefinitively" or "permanentlyclosed" or "definitivelyclosed" or "fermedefinitivement" => ClosedDefinitively,
            "removed" or "dismantled" => Removed,
            "planned" or "announced" => Planned,
            "unknown" => Unknown,
            _ => value?.Trim(),
        };
    }

    public static bool IsClosedDefinitively(string? value)
    {
        return string.Equals(Normalize(value), ClosedDefinitively, StringComparison.Ordinal);
    }

    public static bool CanReceiveVisitorRatings(ParkItemCategory category, string? value)
    {
        string? normalized = Normalize(value);
        if (normalized is null)
        {
            return category != ParkItemCategory.Attraction;
        }

        return normalized is Operating
            or TemporarilyClosed
            or ClosedDefinitively
            or Removed;
    }

    public static bool CanAppearInCurrentRatingRankings(ParkItemCategory category, string? value)
    {
        string? normalized = Normalize(value);
        if (normalized is null)
        {
            return category != ParkItemCategory.Attraction;
        }

        return string.Equals(normalized, Operating, StringComparison.Ordinal);
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
