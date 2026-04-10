using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Ports;

namespace AmusementPark.Infrastructure.Services.Authentication;

/// <summary>
/// Génération et hashage des tokens opaques de comptes locaux.
/// </summary>
public sealed class LocalAccountTokenFactory : IRefreshTokenFactory
{
    public string Generate()
    {
        byte[] buffer = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(buffer)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public string ComputeHash(string token)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(token);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
