namespace AmusementPark.WebAPI.Configuration;

/// <summary>
/// Configuration des limites ciblées sur les endpoints d'authentification exposés publiquement.
/// </summary>
public sealed class AuthenticationRateLimitingSettings
{
    public const string ConfigurationSectionName = "RateLimiting:Authentication";

    public FixedWindowRateLimitSettings Login { get; set; } = FixedWindowRateLimitSettings.Create(5, 60);

    public FixedWindowRateLimitSettings ExternalLogin { get; set; } = FixedWindowRateLimitSettings.Create(10, 60);

    public FixedWindowRateLimitSettings RefreshToken { get; set; } = FixedWindowRateLimitSettings.Create(30, 60);

    public FixedWindowRateLimitSettings Registration { get; set; } = FixedWindowRateLimitSettings.Create(5, 900);

    public FixedWindowRateLimitSettings EmailChallenge { get; set; } = FixedWindowRateLimitSettings.Create(3, 900);

    public FixedWindowRateLimitSettings PasswordReset { get; set; } = FixedWindowRateLimitSettings.Create(5, 900);
}
