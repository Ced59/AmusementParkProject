using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class UserVisitCreationFingerprint
{
    public static string HashOperationKey(string clientOperationId)
    {
        return Hash(clientOperationId);
    }

    public static string HashPayload(Visit visit)
    {
        ArgumentNullException.ThrowIfNull(visit);

        string canonicalPayload = JsonSerializer.Serialize(new CreationPayload(
            visit.ParkId,
            visit.Date.Year,
            visit.Date.Month,
            visit.Date.Day,
            visit.Date.Precision,
            visit.Date.IsApproximate,
            visit.TimeZoneId,
            visit.ServiceDayConvention,
            visit.Title,
            visit.PrivateNote));
        return Hash(canonicalPayload);
    }

    private static string Hash(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private sealed record CreationPayload(
        string ParkId,
        int Year,
        int? Month,
        int? Day,
        VisitDatePrecision Precision,
        bool IsApproximate,
        string? TimeZoneId,
        LocalServiceDayConvention ServiceDayConvention,
        string? Title,
        string? PrivateNote);
}
