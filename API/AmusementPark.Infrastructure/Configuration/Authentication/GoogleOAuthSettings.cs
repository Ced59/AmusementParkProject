namespace AmusementPark.Infrastructure.Configuration.Authentication;

/// <summary>
/// Paramètres de validation Google OAuth.
/// </summary>
public sealed class GoogleOAuthSettings
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string GrantType { get; set; } = "authorization_code";

    public string TokenExchangeEndpoint { get; set; } = "https://oauth2.googleapis.com/token";

    public string UserInfosEndpoint { get; set; } = "https://www.googleapis.com/oauth2/v2/userinfo";
}
