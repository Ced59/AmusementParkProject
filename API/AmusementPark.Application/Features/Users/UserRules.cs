using System.Text.RegularExpressions;

namespace AmusementPark.Application.Features.Users;

/// <summary>
/// Règles et normalisations communes de la feature Users.
/// </summary>
internal static class UserRules
{
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
}
