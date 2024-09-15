using Services.Interfaces.Settings;

namespace WebAPI.Settings.OAuth;

public class GoogleOAuthSettings : IGoogleOAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string GrantType { get; set; } = string.Empty;
    public string TokenExchangeEndpoint { get; set; } = string.Empty;
    public string UserInfosEndpoint { get; set; } = string.Empty;
}