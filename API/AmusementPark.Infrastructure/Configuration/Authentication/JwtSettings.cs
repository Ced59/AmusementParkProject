namespace AmusementPark.Infrastructure.Configuration.Authentication;

/// <summary>
/// Paramètres JWT techniques.
/// </summary>
public sealed class JwtSettings
{
    public string Key { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int TokenBaseExpirationMinutes { get; set; } = 30;

    public int TokenRefreshLimitMinutes { get; set; } = 45;
}
