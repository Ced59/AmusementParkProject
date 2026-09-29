using System.Security.Cryptography;
using System.Text.Json;
using AmusementPark.WebAPI.Contracts.LiveData;

namespace AmusementPark.WebAPI.OutputCaching;

public static class PublicLiveEntityTagFactory
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public static string Create(object? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        object stableValue = value switch
        {
            PublicLiveTargetDto target => ToStableValue(target),
            PublicParkLiveItemsDto parkItems => new
            {
                parkItems.ParkId,
                parkItems.ParkDisplayName,
                Items = parkItems.Items.Select(ToStableValue).ToArray(),
            },
            _ => value,
        };
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(stableValue, SerializerOptions);
        byte[] hash = SHA256.HashData(payload);
        return $"\"{Convert.ToHexString(hash).ToLowerInvariant()}\"";
    }

    private static object ToStableValue(PublicLiveTargetDto target)
    {
        return new
        {
            target.TargetId,
            target.TargetType,
            target.DisplayName,
            target.ParkId,
            target.ParkDisplayName,
            target.Availability,
            target.Status,
            target.Queues,
            target.ObservedAtUtc,
            target.ReceivedAtUtc,
            target.Freshness,
            target.ExpiresAtUtc,
            target.Source,
            target.Confidence,
        };
    }
}
