using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Trips;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripPlanCreationFingerprint
{
    private static readonly byte[] PayloadSigningPurpose =
        Encoding.UTF8.GetBytes("amusement-park/trip-plan/creation-payload/v1");
    private static readonly byte[] OwnerScopeSigningPurpose =
        Encoding.UTF8.GetBytes("amusement-park/trip-plan/owner-scope/v1");
    private readonly IReadOnlyDictionary<string, byte[]> payloadSigningKeys;
    private readonly IReadOnlyDictionary<string, byte[]> ownerScopeSigningKeys;

    public TripPlanCreationFingerprint(TripFingerprintKeyRingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        IReadOnlyDictionary<string, string> configuredKeys = settings.GetValidatedKeys();
        Dictionary<string, byte[]> payloadKeys = new(StringComparer.Ordinal);
        Dictionary<string, byte[]> ownerKeys = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> configuredKey in configuredKeys)
        {
            byte[] rootKey = Convert.FromBase64String(configuredKey.Value);
            try
            {
                using HMACSHA256 keyDerivation = new(rootKey);
                payloadKeys.Add(
                    configuredKey.Key,
                    keyDerivation.ComputeHash(PayloadSigningPurpose));
                ownerKeys.Add(
                    configuredKey.Key,
                    keyDerivation.ComputeHash(OwnerScopeSigningPurpose));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rootKey);
            }
        }

        this.CurrentKeyVersion = settings.CurrentVersion.Trim();
        this.payloadSigningKeys = payloadKeys;
        this.ownerScopeSigningKeys = ownerKeys;
    }

    public string CurrentKeyVersion { get; }

    public static string HashOperationKey(string clientOperationId)
    {
        return Hash(clientOperationId);
    }

    public string HashOwnerScope(string ownerUserId)
    {
        return this.HashOwnerScope(ownerUserId, this.CurrentKeyVersion);
    }

    public IReadOnlyCollection<string> HashOwnerScopes(string ownerUserId)
    {
        return this.ownerScopeSigningKeys.Keys
            .Select(keyVersion => this.HashOwnerScope(ownerUserId, keyVersion))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public string HashOwnerScope(string ownerUserId, string keyVersion)
    {
        string normalizedOwnerUserId = ownerUserId?.Trim() ?? string.Empty;
        if (normalizedOwnerUserId.Length == 0)
        {
            throw new ArgumentException("A non-empty owner identifier is required.", nameof(ownerUserId));
        }

        byte[] ownerBytes = Encoding.UTF8.GetBytes(normalizedOwnerUserId);
        try
        {
            using HMACSHA256 hmac = new(GetRequiredKey(this.ownerScopeSigningKeys, keyVersion));
            return Convert.ToHexString(hmac.ComputeHash(ownerBytes)).ToLowerInvariant();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ownerBytes);
        }
    }

    public string HashPayload(TripPlan tripPlan)
    {
        return this.HashPayload(tripPlan, this.CurrentKeyVersion);
    }

    public string HashPayload(TripPlan tripPlan, string keyVersion)
    {
        ArgumentNullException.ThrowIfNull(tripPlan);
        TripPlanCreationPayload payload = new(
            tripPlan.OwnerUserId,
            tripPlan.Title,
            tripPlan.DateProposal.Kind.ToString(),
            Format(tripPlan.DateProposal.StartDate),
            Format(tripPlan.DateProposal.EndDate),
            tripPlan.DateProposal.CandidateDates.Select(static date => Format(date)!).ToArray(),
            tripPlan.DestinationTimeZoneId);
        byte[] serializedPayload = JsonSerializer.SerializeToUtf8Bytes(payload);
        try
        {
            using HMACSHA256 hmac = new(GetRequiredKey(this.payloadSigningKeys, keyVersion));
            return Convert.ToHexString(hmac.ComputeHash(serializedPayload)).ToLowerInvariant();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(serializedPayload);
        }
    }

    private static string? Format(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string Hash(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        try
        {
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    private static byte[] GetRequiredKey(
        IReadOnlyDictionary<string, byte[]> keys,
        string keyVersion)
    {
        string normalizedVersion = keyVersion?.Trim() ?? string.Empty;
        return keys.TryGetValue(normalizedVersion, out byte[]? key)
            ? key
            : throw new InvalidOperationException(
                $"The trip fingerprint key version '{normalizedVersion}' is unavailable.");
    }
}
