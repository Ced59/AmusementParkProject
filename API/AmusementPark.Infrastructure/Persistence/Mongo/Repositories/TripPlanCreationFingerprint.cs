using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class TripPlanCreationFingerprint
{
    public static string HashOperationKey(string clientOperationId)
    {
        return Hash(clientOperationId);
    }

    public static string HashPayload(TripPlan tripPlan)
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
        return Hash(JsonSerializer.Serialize(payload));
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
