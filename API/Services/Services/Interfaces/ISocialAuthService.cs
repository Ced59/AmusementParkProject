using Entities.Model.Users;

namespace Services.Interfaces;

public interface ISocialAuthService
{

    Task<string> ExchangeGoogleCodeForToken(string provider, string code);
    Task<UserGoogleInfos> GetGoogleUserInfo(string provider, string accessToken);

}