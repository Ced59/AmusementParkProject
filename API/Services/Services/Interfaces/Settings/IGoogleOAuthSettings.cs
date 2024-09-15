namespace Services.Interfaces.Settings;

public interface IGoogleOAuthSettings
{
    string ClientId { get; set; }
    string ClientSecret { get; set; }
    string RedirectUri { get; set; }
    string GrantType { get; set; }
    string TokenExchangeEndpoint { get; set; }
    string UserInfosEndpoint { get; set; }
}