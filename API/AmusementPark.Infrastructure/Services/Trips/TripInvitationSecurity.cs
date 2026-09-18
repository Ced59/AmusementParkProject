using System.Security.Cryptography;
using System.Text;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Trips;

namespace AmusementPark.Infrastructure.Services.Trips;

public sealed class TripInvitationSecurity : ITripInvitationSecurity
{
    private const int TokenByteCount = 32;
    private const int NonceByteCount = 12;
    private const int TagByteCount = 16;
    private readonly IReadOnlyDictionary<string, byte[]> rootKeys;

    public TripInvitationSecurity(TripFingerprintKeyRingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        this.CurrentKeyVersion = settings.CurrentVersion.Trim();
        this.rootKeys = settings.GetValidatedKeys().ToDictionary(
            static pair => pair.Key,
            static pair => Convert.FromBase64String(pair.Value),
            StringComparer.Ordinal);
    }

    public string CurrentKeyVersion { get; }

    public string HashOperationKey(string actorUserId, string clientOperationId)
    {
        return HashCanonical($"{NormalizeRequired(actorUserId)}\n{NormalizeRequired(clientOperationId)}");
    }

    public string HashCreationPayload(
        TripDelegatedRole proposedRole,
        int lifetimeHours,
        string? normalizedTargetEmail,
        string? existingRequestHash)
    {
        string email = normalizedTargetEmail?.Trim().ToLowerInvariant() ?? string.Empty;
        string keyVersion = this.ResolveCreationPayloadKeyVersion(existingRequestHash);
        byte[] key = this.DeriveKey(
            keyVersion,
            "trip-invitation-creation-payload-v1");
        byte[] payload = Encoding.UTF8.GetBytes($"{proposedRole:D}\n{lifetimeHours}\n{email}");
        try
        {
            using HMACSHA256 hmac = new HMACSHA256(key);
            return $"{keyVersion}:{Convert.ToBase64String(hmac.ComputeHash(payload))}";
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private string ResolveCreationPayloadKeyVersion(string? existingRequestHash)
    {
        string normalized = existingRequestHash?.Trim() ?? string.Empty;
        int separatorIndex = normalized.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return this.CurrentKeyVersion;
        }

        string persistedVersion = normalized[..separatorIndex];
        return this.rootKeys.ContainsKey(persistedVersion)
            ? persistedVersion
            : this.CurrentKeyVersion;
    }

    public TripInvitationTokenMaterial CreateToken(
        TripInvitationId invitationId,
        string actorUserId,
        string operationKeyHash,
        string requestHash)
    {
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(TokenByteCount);
        try
        {
            string plainTextToken = EncodeBase64Url(tokenBytes);
            string tokenHash = Convert.ToBase64String(SHA256.HashData(tokenBytes));
            string tokenHint = plainTextToken[..6];
            string sealedToken = this.Seal(
                tokenBytes,
                this.CurrentKeyVersion,
                BuildAssociatedData(invitationId, actorUserId, operationKeyHash, requestHash));
            return new TripInvitationTokenMaterial(
                plainTextToken,
                tokenHash,
                tokenHint,
                sealedToken,
                this.CurrentKeyVersion);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(tokenBytes);
        }
    }

    public bool TryRevealToken(TripInvitationCreationRecord record, string actorUserId, out string token)
    {
        ArgumentNullException.ThrowIfNull(record);
        token = string.Empty;
        if (!this.rootKeys.ContainsKey(record.SealedTokenKeyVersion)
            || !TryDecodeBase64Url(record.SealedToken, out byte[] sealedBytes)
            || sealedBytes.Length != NonceByteCount + TagByteCount + TokenByteCount)
        {
            return false;
        }

        byte[] nonce = sealedBytes[..NonceByteCount];
        byte[] tag = sealedBytes[NonceByteCount..(NonceByteCount + TagByteCount)];
        byte[] ciphertext = sealedBytes[(NonceByteCount + TagByteCount)..];
        byte[] plaintext = new byte[TokenByteCount];
        byte[] key = this.DeriveKey(record.SealedTokenKeyVersion, "trip-invitation-token-sealing-v1");
        byte[] associatedData = BuildAssociatedData(
            record.Invitation.Id,
            actorUserId,
            record.OperationKeyHash,
            record.RequestHash);
        try
        {
            using AesGcm aes = new AesGcm(key, TagByteCount);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
            string candidate = EncodeBase64Url(plaintext);
            byte[] candidateHash = SHA256.HashData(plaintext);
            byte[] expectedHash = Convert.FromBase64String(record.Invitation.TokenHash);
            if (!CryptographicOperations.FixedTimeEquals(candidateHash, expectedHash))
            {
                return false;
            }

            token = candidate;
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(sealedBytes);
        }
    }

    public bool TryHashPublicToken(string token, out string tokenHash)
    {
        tokenHash = string.Empty;
        string normalized = token?.Trim() ?? string.Empty;
        if (!TryDecodeBase64Url(normalized, out byte[] tokenBytes))
        {
            return false;
        }

        try
        {
            if (tokenBytes.Length != TokenByteCount
                || !string.Equals(EncodeBase64Url(tokenBytes), normalized, StringComparison.Ordinal))
            {
                return false;
            }

            tokenHash = Convert.ToBase64String(SHA256.HashData(tokenBytes));
            return true;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(tokenBytes);
        }
    }

    public TripInvitationEmailFingerprint FingerprintEmail(string normalizedEmail)
    {
        string email = NormalizeRequired(normalizedEmail).ToLowerInvariant();
        byte[] key = this.DeriveKey(this.CurrentKeyVersion, "trip-invitation-email-fingerprint-v1");
        byte[] payload = Encoding.UTF8.GetBytes(email);
        try
        {
            using HMACSHA256 hmac = new HMACSHA256(key);
            return new TripInvitationEmailFingerprint(
                Convert.ToBase64String(hmac.ComputeHash(payload)),
                this.CurrentKeyVersion);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public bool MatchesEmailFingerprint(
        string normalizedEmail,
        string expectedFingerprint,
        string keyVersion)
    {
        string email = NormalizeRequired(normalizedEmail).ToLowerInvariant();
        string normalizedKeyVersion = NormalizeRequired(keyVersion);
        if (!this.rootKeys.ContainsKey(normalizedKeyVersion))
        {
            return false;
        }

        byte[] key = this.DeriveKey(normalizedKeyVersion, "trip-invitation-email-fingerprint-v1");
        byte[] payload = Encoding.UTF8.GetBytes(email);
        try
        {
            using HMACSHA256 hmac = new HMACSHA256(key);
            byte[] actual = hmac.ComputeHash(payload);
            byte[] expected;
            try
            {
                expected = Convert.FromBase64String(expectedFingerprint);
            }
            catch (FormatException)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private string Seal(byte[] plaintext, string keyVersion, byte[] associatedData)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceByteCount);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagByteCount];
        byte[] key = this.DeriveKey(keyVersion, "trip-invitation-token-sealing-v1");
        byte[] sealedBytes = new byte[NonceByteCount + TagByteCount + ciphertext.Length];
        try
        {
            using AesGcm aes = new AesGcm(key, TagByteCount);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
            nonce.CopyTo(sealedBytes, 0);
            tag.CopyTo(sealedBytes, NonceByteCount);
            ciphertext.CopyTo(sealedBytes, NonceByteCount + TagByteCount);
            return EncodeBase64Url(sealedBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(sealedBytes);
        }
    }

    private byte[] DeriveKey(string keyVersion, string purpose)
    {
        if (!this.rootKeys.TryGetValue(keyVersion, out byte[]? rootKey))
        {
            throw new InvalidOperationException("The trip invitation key version is unavailable.");
        }

        using HMACSHA256 hmac = new HMACSHA256(rootKey);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(purpose));
    }

    private static byte[] BuildAssociatedData(
        TripInvitationId invitationId,
        string actorUserId,
        string operationKeyHash,
        string requestHash)
    {
        return Encoding.UTF8.GetBytes(string.Join(
            '\n',
            invitationId.Value,
            NormalizeRequired(actorUserId),
            NormalizeRequired(operationKeyHash),
            NormalizeRequired(requestHash)));
    }

    private static string HashCanonical(string value)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string NormalizeRequired(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length > 0
            ? normalized
            : throw new ArgumentException("A required cryptographic context value is missing.");
    }

    private static string EncodeBase64Url(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static bool TryDecodeBase64Url(string value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(value)
            || value.Any(static character => !char.IsAsciiLetterOrDigit(character)
                && character is not '-' and not '_'))
        {
            return false;
        }

        string padded = value.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
        {
            0 => string.Empty,
            2 => "==",
            3 => "=",
            _ => "invalid",
        };
        try
        {
            bytes = Convert.FromBase64String(padded);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
