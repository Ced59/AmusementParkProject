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
        string clientId = _configuration.ClientId;
        string clientSecret = _configuration.ClientSecret;
        string redirectUri = _configuration.RedirectUri;
        string tokenEndpoint = _configuration.TokenExchangeEndpoint;

        Dictionary<string, string> values = new Dictionary<string, string>
        {
            { "code", code },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri },
            { "grant_type", "authorization_code" }
        };

        FormUrlEncodedContent content = new FormUrlEncodedContent(values);
        HttpResponseMessage response = await _httpClient.PostAsync(tokenEndpoint, content);
        string responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Could not retrieve the access token for {provider}");

        TokenResponse? tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseString);
        return tokenResponse.AccessToken;
    }

    public async Task<UserGoogleInfos> GetGoogleUserInfo(string provider, string accessToken)
    {
        string userInfoEndpoint = _configuration.UserInfosEndpoint;

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        HttpResponseMessage response = await _httpClient.GetAsync(userInfoEndpoint);

        string responseString = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Could not retrieve user info for {provider}");

        return JsonConvert.DeserializeObject<UserGoogleInfos>(responseString);
    }
}


