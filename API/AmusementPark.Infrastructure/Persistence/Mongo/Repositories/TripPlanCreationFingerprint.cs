using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Authentication;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripPlanCreationFingerprint
{
    private static readonly byte[] PayloadSigningPurpose =
        Encoding.UTF8.GetBytes("amusement-park/trip-plan/creation-payload/v1");
    private static readonly byte[] OwnerScopeSigningPurpose =
        Encoding.UTF8.GetBytes("amusement-park/trip-plan/owner-scope/v1");
    private readonly byte[] payloadSigningKey;
    private readonly byte[] ownerScopeSigningKey;

    public TripPlanCreationFingerprint(JwtSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settings.Key))
        {
            throw new ArgumentException("A server signing key is required.", nameof(settings));
        }

        byte[] rootKey = Encoding.UTF8.GetBytes(settings.Key);
        try
        {
            using HMACSHA256 keyDerivation = new(rootKey);
            this.payloadSigningKey = keyDerivation.ComputeHash(PayloadSigningPurpose);
            this.ownerScopeSigningKey = keyDerivation.ComputeHash(OwnerScopeSigningPurpose);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(rootKey);
        }
    }

    public static string HashOperationKey(string clientOperationId)
    {
        return Hash(clientOperationId);
    }

    public string HashOwnerScope(string ownerUserId)
    {
        string normalizedOwnerUserId = ownerUserId?.Trim() ?? string.Empty;
        if (normalizedOwnerUserId.Length == 0)
        {
            throw new ArgumentException("A non-empty owner identifier is required.", nameof(ownerUserId));
        }

        byte[] ownerBytes = Encoding.UTF8.GetBytes(normalizedOwnerUserId);
        try
        {
            using HMACSHA256 hmac = new(this.ownerScopeSigningKey);
            return Convert.ToHexString(hmac.ComputeHash(ownerBytes)).ToLowerInvariant();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ownerBytes);
        }
    }

    public string HashPayload(TripPlan tripPlan)
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
            using HMACSHA256 hmac = new(this.payloadSigningKey);
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
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
