using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Infrastructure.Configuration.Authentication;

namespace AmusementPark.Infrastructure.Services.Email;

public sealed class EncryptedNotificationEmailUnsubscribeTokenProtector
    : INotificationEmailUnsubscribeTokenProtector
{
    private const byte TokenVersion = 1;
    private const int NonceLength = 12;
    private const int TagLength = 16;
    private const string EncryptionPurpose = "AmusementPark.NotificationEmailUnsubscribe.v1";
    private static readonly byte[] AdditionalData = Encoding.UTF8.GetBytes(EncryptionPurpose);
    private readonly byte[] encryptionKey;

    public EncryptedNotificationEmailUnsubscribeTokenProtector(JwtSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settings.Key))
        {
            throw new InvalidOperationException(
                "The authentication signing key is required to protect email unsubscribe links.");
        }

        using HMACSHA256 derivation = new HMACSHA256(Encoding.UTF8.GetBytes(settings.Key));
        this.encryptionKey = derivation.ComputeHash(AdditionalData);
    }

    public string CreateToken(string userId)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        byte[] plaintext = Encoding.UTF8.GetBytes(normalizedUserId);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceLength);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagLength];
        using AesGcm cipher = new AesGcm(this.encryptionKey, TagLength);
        cipher.Encrypt(nonce, plaintext, ciphertext, tag, AdditionalData);
        byte[] payload = new byte[1 + NonceLength + ciphertext.Length + TagLength];
        payload[0] = TokenVersion;
        nonce.CopyTo(payload, 1);
        ciphertext.CopyTo(payload, 1 + NonceLength);
        tag.CopyTo(payload, 1 + NonceLength + ciphertext.Length);
        return Encode(payload);
    }

    public bool TryReadUserId(string token, out string userId)
    {
        userId = string.Empty;
        string normalizedToken = token?.Trim() ?? string.Empty;
        if (normalizedToken.Length == 0
            || normalizedToken.Length > 1000
            || !string.Equals(token, normalizedToken, StringComparison.Ordinal)
            || !TryDecode(normalizedToken, out byte[] payload)
            || payload.Length <= 1 + NonceLength + TagLength
            || payload[0] != TokenVersion)
        {
            return false;
        }

        ReadOnlySpan<byte> nonce = payload.AsSpan(1, NonceLength);
        int ciphertextLength = payload.Length - 1 - NonceLength - TagLength;
        ReadOnlySpan<byte> ciphertext = payload.AsSpan(1 + NonceLength, ciphertextLength);
        ReadOnlySpan<byte> tag = payload.AsSpan(payload.Length - TagLength, TagLength);
        byte[] plaintext = new byte[ciphertextLength];
        try
        {
            using AesGcm cipher = new AesGcm(this.encryptionKey, TagLength);
            cipher.Decrypt(nonce, ciphertext, tag, plaintext, AdditionalData);
            string candidate = new UTF8Encoding(false, true).GetString(plaintext);
            userId = IdentifierRules.NormalizeRequired(candidate, nameof(token));
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string Encode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool TryDecode(string value, out byte[] decoded)
    {
        decoded = Array.Empty<byte>();
        try
        {
            string base64 = value.Replace('-', '+').Replace('_', '/');
            int padding = (4 - base64.Length % 4) % 4;
            byte[] candidate = Convert.FromBase64String(
                base64.PadRight(base64.Length + padding, '='));
            if (!string.Equals(Encode(candidate), value, StringComparison.Ordinal))
            {
                return false;
            }

            decoded = candidate;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
