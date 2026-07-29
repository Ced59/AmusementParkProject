using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AmusementPark.Core.Domain.Users;

/// <summary>
/// Protège les pseudonymes publics contre les valeurs invisibles et l'usurpation de rôles.
/// </summary>
public static class PublicDisplayNamePolicy
{
    public const int MaximumLength = 60;

    private static readonly string[] ReservedPrefixes =
    {
        "admin",
        "administrator",
        "administrateur",
        "administrador",
        "modo",
        "moderator",
        "moderateur",
        "moderador",
        "staff",
        "official",
        "officiel",
        "support",
        "equipe",
        "team",
        "amusementparks",
        "equipeamusementparks",
        "teamamusementparks",
        "officialamusementparks",
        "amusementparksofficiel",
        "amusementparksofficial",
    };

    public static bool IsValid(string? publicDisplayName)
    {
        if (publicDisplayName is null)
        {
            return true;
        }

        if (publicDisplayName.Length == 0 || publicDisplayName.Length > MaximumLength)
        {
            return false;
        }

        bool hasVisibleContent = false;
        foreach (Rune rune in publicDisplayName.EnumerateRunes())
        {
            UnicodeCategory category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.Control
                or UnicodeCategory.Format
                or UnicodeCategory.LineSeparator
                or UnicodeCategory.ParagraphSeparator
                or UnicodeCategory.OtherNotAssigned
                || rune.Value == 0xFFFD)
            {
                return false;
            }

            if (category is UnicodeCategory.UppercaseLetter
                or UnicodeCategory.LowercaseLetter
                or UnicodeCategory.TitlecaseLetter
                or UnicodeCategory.ModifierLetter
                or UnicodeCategory.OtherLetter
                or UnicodeCategory.DecimalDigitNumber
                or UnicodeCategory.LetterNumber
                or UnicodeCategory.OtherNumber
                or UnicodeCategory.MathSymbol
                or UnicodeCategory.CurrencySymbol
                or UnicodeCategory.ModifierSymbol
                or UnicodeCategory.OtherSymbol)
            {
                hasVisibleContent = true;
            }
        }

        return hasVisibleContent;
    }

    public static bool IsReserved(string publicDisplayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicDisplayName);

        string canonical = Canonicalize(publicDisplayName, true);
        string canonicalWithoutLeetMapping = Canonicalize(publicDisplayName, false);
        if (ReservedPrefixes.Any(
            prefix => canonical.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return true;
        }

        return Regex.IsMatch(
            canonicalWithoutLeetMapping,
            "^user[0-9]+$",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
    }

    private static string Canonicalize(string value, bool mapLeetCharacters)
    {
        string decomposed = value.Normalize(NormalizationForm.FormKD);
        StringBuilder builder = new StringBuilder(decomposed.Length);
        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            char normalizedCharacter = char.ToLowerInvariant(character);
            char normalized = normalizedCharacter switch
            {
                '0' when mapLeetCharacters => 'o',
                '1' when mapLeetCharacters => 'i',
                '3' when mapLeetCharacters => 'e',
                '4' when mapLeetCharacters => 'a',
                '5' when mapLeetCharacters => 's',
                '7' when mapLeetCharacters => 't',
                '@' when mapLeetCharacters => 'a',
                '$' when mapLeetCharacters => 's',
                '!' or '|' when mapLeetCharacters => 'i',
                'а' or 'α' when mapLeetCharacters => 'a',
                'д' or 'δ' when mapLeetCharacters => 'd',
                'м' or 'μ' when mapLeetCharacters => 'm',
                'і' or 'ι' or 'ı' when mapLeetCharacters => 'i',
                'н' when mapLeetCharacters => 'n',
                'о' or 'ο' when mapLeetCharacters => 'o',
                'е' or 'ε' when mapLeetCharacters => 'e',
                'ѕ' when mapLeetCharacters => 's',
                'т' or 'τ' when mapLeetCharacters => 't',
                char candidate when char.IsLetterOrDigit(candidate) => candidate,
                _ => '\0',
            };
            if (normalized != '\0')
            {
                builder.Append(normalized);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
