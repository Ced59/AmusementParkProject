using System;
using System.Security.Cryptography;
using System.Text;
using Services.Interfaces.Authentication;

namespace Services.Implementations.Authentication
{
    public class LocalAccountTokenService : ILocalAccountTokenService
    {
        public string GenerateToken()
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
}
