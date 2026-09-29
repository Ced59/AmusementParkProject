using System.Security.Cryptography;
using System.Text.Json;

namespace AmusementPark.WebAPI.OutputCaching;

public static class PublicLiveEntityTagFactory
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public static string Create<TValue>(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
        byte[] hash = SHA256.HashData(payload);
        return $"\"{Convert.ToHexString(hash).ToLowerInvariant()}\"";
    }
}
