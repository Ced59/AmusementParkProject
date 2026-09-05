using System.Security.Cryptography;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Infrastructure.Services.Sharing;

public sealed class CryptographicShareTokenFactory : IShareTokenFactory
{
    public ShareToken Generate()
    {
        byte[] entropy = RandomNumberGenerator.GetBytes(ShareToken.EntropyByteLength);
        string encoded = Convert.ToBase64String(entropy)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return ShareToken.Parse(encoded);
    }
}
