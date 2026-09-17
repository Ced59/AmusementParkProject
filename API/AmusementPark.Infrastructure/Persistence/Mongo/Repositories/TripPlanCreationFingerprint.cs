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
    private readonly byte[] payloadSigningKey;

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
