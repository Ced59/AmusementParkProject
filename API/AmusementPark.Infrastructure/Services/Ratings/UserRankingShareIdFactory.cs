using System.Security.Cryptography;
using AmusementPark.Application.Features.Ratings.Ports;

namespace AmusementPark.Infrastructure.Services.Ratings;

public sealed class UserRankingShareIdFactory : IUserRankingShareIdFactory
{
    public string Generate()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
