using System;
using AmusementPark.WebAPI.Configuration;
using Microsoft.AspNetCore.Http;

namespace AmusementPark.WebAPI.Security;

/// <summary>
/// Gère l'émission, la lecture et la suppression du cookie HttpOnly de refresh token.
/// </summary>
public sealed class RefreshTokenCookieService
{
    private readonly RefreshTokenCookieSettings settings;

    public RefreshTokenCookieService(RefreshTokenCookieSettings settings)
    {
        this.settings = settings;
    }

    public void AppendRefreshTokenCookie(HttpResponse response, string refreshToken, DateTime expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        CookieOptions cookieOptions = this.CreateCookieOptions();
        cookieOptions.Expires = new DateTimeOffset(expiresAtUtc);

        response.Cookies.Append(this.settings.Name, refreshToken, cookieOptions);
    }

    public string? GetRefreshToken(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Cookies.TryGetValue(this.settings.Name, out string? refreshToken);
        return string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken;
    }

    public void DeleteRefreshTokenCookie(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.Cookies.Delete(this.settings.Name, this.CreateCookieOptions());
    }

    private CookieOptions CreateCookieOptions()
    {
        CookieOptions cookieOptions = new CookieOptions
        {
            HttpOnly = this.settings.HttpOnly,
            IsEssential = true,
            Path = string.IsNullOrWhiteSpace(this.settings.Path) ? "/" : this.settings.Path,
            SameSite = this.settings.GetSameSiteMode(),
            Secure = this.settings.Secure,
        };

        if (!string.IsNullOrWhiteSpace(this.settings.Domain))
        {
            cookieOptions.Domain = this.settings.Domain;
        }

        return cookieOptions;
    }
}
