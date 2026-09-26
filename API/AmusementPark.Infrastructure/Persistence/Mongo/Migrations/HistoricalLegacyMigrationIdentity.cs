using System.Security.Cryptography;
using System.Text;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

internal static class HistoricalLegacyMigrationIdentity
{
    public static Guid CreateGuid(string migrationId, string resourceType, string legacyId, int index = 0)
    {
        string value = string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"{migrationId}:{resourceType}:{legacyId.Trim()}:{index}");
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        byte[] guidBytes = digest[..16];
        guidBytes[7] = (byte)((guidBytes[7] & 0x0f) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3f) | 0x80);
        return new Guid(guidBytes);
    }

    public static string CreateAnomalyId(string migrationId, string legacyId)
    {
        return CreateGuid(migrationId, "anomaly", legacyId).ToString("N");
    }
}
