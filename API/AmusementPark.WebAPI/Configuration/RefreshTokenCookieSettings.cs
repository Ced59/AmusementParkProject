using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Configuration;

/// <summary>
/// Paramètres du cookie HttpOnly portant le refresh token.
/// </summary>
public sealed class RefreshTokenCookieSettings
{
    public const string SectionName = "Authentication:RefreshTokenCookie";

    public string Name { get; init; } = "amusementpark.refresh";

    public string Path { get; init; } = "/auth";

    public bool HttpOnly { get; init; } = true;

    public bool Secure { get; init; } = true;

    public string SameSite { get; init; } = "None";

    public string? Domain { get; init; }

    public SameSiteMode GetSameSiteMode()
    {
        if (string.Equals(this.SameSite, "Strict", System.StringComparison.OrdinalIgnoreCase))
        {
            return SameSiteMode.Strict;
        }

        if (string.Equals(this.SameSite, "Lax", System.StringComparison.OrdinalIgnoreCase))
        {
            return SameSiteMode.Lax;
        }

        if (string.Equals(this.SameSite, "Unspecified", System.StringComparison.OrdinalIgnoreCase))
        {
            return SameSiteMode.Unspecified;
        }

        return SameSiteMode.None;
    }
}
