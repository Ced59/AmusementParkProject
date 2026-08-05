using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.ParkDataEditorTokens.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Infrastructure.Services.Authentication;

public sealed class ParkDataEditorTokenProtector : IParkDataEditorTokenProtector
{
    public const string TokenPrefix = "apf_pde_";

    public ParkDataEditorTokenMaterial Create(string tokenId)
    {
        if (!Guid.TryParseExact(tokenId, "N", out Guid _))
        {
            throw new ArgumentException("The token identifier must be a GUID in N format.", nameof(tokenId));
        }

        string secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        string plainTextToken = $"{TokenPrefix}{tokenId}.{secret}";
        return new ParkDataEditorTokenMaterial(
            plainTextToken,
            ComputeHash(plainTextToken),
            $"{TokenPrefix}{tokenId[..8]}");
    }

    public bool TryReadTokenId(string plainTextToken, out string tokenId)
    {
        tokenId = string.Empty;
        if (string.IsNullOrWhiteSpace(plainTextToken)
            || !plainTextToken.StartsWith(TokenPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        int separatorIndex = plainTextToken.IndexOf('.', TokenPrefix.Length);
        if (separatorIndex <= TokenPrefix.Length)
        {
            return false;
        }

        string candidate = plainTextToken[TokenPrefix.Length..separatorIndex];
        if (!Guid.TryParseExact(candidate, "N", out Guid parsedId))
        {
            return false;
        }

        string secret = plainTextToken[(separatorIndex + 1)..];
        if (secret.Length < 40)
        {
            return false;
        }

        tokenId = parsedId.ToString("N");
        return true;
    }

    public bool Verify(string plainTextToken, ParkDataEditorAccessToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        byte[] expectedHash;
        try
        {
            expectedHash = Convert.FromBase64String(token.TokenHash);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(plainTextToken));
        return expectedHash.Length == providedHash.Length
               && CryptographicOperations.FixedTimeEquals(expectedHash, providedHash);
    }

    private static string ComputeHash(string plainTextToken)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(plainTextToken));
        return Convert.ToBase64String(hash);
    }
}
