using Newtonsoft.Json;
using Services.Interfaces;
using System.Net.Http.Headers;
using Entities.Model.Users;
using Google.Apis.Auth.OAuth2.Responses;
using Services.Interfaces.Settings;

namespace Services.Implementations;

public class SocialAuthService : ISocialAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IGoogleOAuthSettings _configuration;

    public SocialAuthService(HttpClient httpClient, IGoogleOAuthSettings configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<string> ExchangeGoogleCodeForToken(string provider, string code)
    {
        // Exemple de récupération des configurations spécifiques au fournisseur
        var clientId = _configuration.ClientId;
        var clientSecret = _configuration.ClientSecret;
        var redirectUri = _configuration.RedirectUri;
        var tokenEndpoint = _configuration.TokenExchangeEndpoint;

        var values = new Dictionary<string, string>
        {
            { "code", code },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri },
            { "grant_type", "authorization_code" }
        };

        var content = new FormUrlEncodedContent(values);
        var response = await _httpClient.PostAsync(tokenEndpoint, content);
        var responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Could not retrieve the access token for {provider}");

        var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseString);
        return tokenResponse.AccessToken;
    }

    public async Task<UserGoogleInfos> GetGoogleUserInfo(string provider, string accessToken)
    {
        var userInfoEndpoint = _configuration.UserInfosEndpoint;

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _httpClient.GetAsync(userInfoEndpoint);

        var responseString = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Could not retrieve user info for {provider}");

        return JsonConvert.DeserializeObject<UserGoogleInfos>(responseString);
    }
}


