using Services.Interfaces.Settings;

namespace WebAPI.Settings.Security;

public class JwtSettings : IJwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int TokenBaseExpirationMinutes { get; set; } = 0;
    public int TokenRefreshLimitMinutes { get; set; } = 0;
}