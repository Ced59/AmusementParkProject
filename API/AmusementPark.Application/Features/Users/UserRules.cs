using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AmusementPark.Application.Features.Users;

/// <summary>
/// Règles et normalisations communes de la feature Users.
/// </summary>
internal static class UserRules
{
    private const int MaximumPublicDisplayNameLength = 60;

    public static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? null
            : email.Trim().ToLowerInvariant();
    }

    public static string NormalizePreferredLanguage(string? preferredLanguage)
    {
        return string.IsNullOrWhiteSpace(preferredLanguage)
            ? "EN"
            : preferredLanguage.Trim().ToUpperInvariant();
    }

    public static string NormalizePreferredMeasurementSystem(string? preferredMeasurementSystem)
    {
        string normalized = preferredMeasurementSystem?.Trim() ?? string.Empty;

        if (string.Equals(normalized, "Imperial", StringComparison.OrdinalIgnoreCase))
        {
            return "Imperial";
        }

        return "Metric";
    }

    public static string? NormalizePublicDisplayName(string? publicDisplayName)
    {
        return string.IsNullOrWhiteSpace(publicDisplayName)
            ? null
            : publicDisplayName.Trim();
    }

    public static bool IsValidPublicDisplayName(string? publicDisplayName)
    {
        return publicDisplayName is null || publicDisplayName.Length <= MaximumPublicDisplayNameLength;
    }

    public static bool IsReservedPublicDisplayName(string publicDisplayName)
    {
        string canonical = CanonicalizePublicDisplayName(publicDisplayName, true);
        string canonicalWithoutLeetMapping = CanonicalizePublicDisplayName(publicDisplayName, false);
        string[] reservedPrefixes =
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
            "equipeamusementparks",
            "teamamusementparks",
            "officialamusementparks",
            "amusementparksofficiel",
            "amusementparksofficial",
        };

        if (reservedPrefixes.Any(canonical.StartsWith))
        {
            return true;
        }

        return Regex.IsMatch(
            canonicalWithoutLeetMapping,
            "^user[0-9]+$",
            RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
    }

    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            return Regex.IsMatch(
                email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(250));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    public static bool IsValidPassword(string? password)
    {
        string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$";
        return Regex.IsMatch(password ?? string.Empty, passwordPattern);
    }

    private static string CanonicalizePublicDisplayName(string value, bool mapLeetCharacters)
    {
        string decomposed = value.Normalize(NormalizationForm.FormD);
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
