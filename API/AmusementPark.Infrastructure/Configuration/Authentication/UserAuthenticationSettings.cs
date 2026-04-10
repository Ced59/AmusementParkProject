using AmusementPark.Application.Features.Users.Ports;
using Microsoft.Extensions.Configuration;

namespace AmusementPark.Infrastructure.Configuration.Authentication;

/// <summary>
/// Agrégation des paramètres fonctionnels nécessaires à la feature Users.
/// </summary>
public sealed class UserAuthenticationSettings : IUserAuthenticationSettings
{
    public int EmailConfirmationTokenExpirationHours { get; set; } = 24;

    public int PasswordResetTokenExpirationMinutes { get; set; } = 60;

    public int TokenRefreshLimitMinutes { get; set; } = 45;

    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";

    public static UserAuthenticationSettings Bind(IConfiguration configuration)
    {
        UserAuthenticationSettings settings = new UserAuthenticationSettings();

        configuration.GetSection("Authentication:Local").Bind(settings);

        JwtSettings jwtSettings = configuration.GetSection("Authentication:Jwt").Get<JwtSettings>() ?? new JwtSettings();
        settings.TokenRefreshLimitMinutes = jwtSettings.TokenRefreshLimitMinutes <= 0 ? settings.TokenRefreshLimitMinutes : jwtSettings.TokenRefreshLimitMinutes;
        return settings;
    }
}
