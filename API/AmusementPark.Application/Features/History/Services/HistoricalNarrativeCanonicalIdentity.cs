using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AmusementPark.Application.Features.History.Services;

internal static class HistoricalNarrativeCanonicalIdentity
{
    public static Guid CreateGuid(
        string version,
        string resourceType,
        string narrativeId,
        DateTime narrativeUpdatedAtUtc,
        int index = 0)
    {
        long persistedMilliseconds = HistoricalNarrativeCanonicalizationPolicy
            .NormalizeUtc(narrativeUpdatedAtUtc)
            .Ticks
            / TimeSpan.TicksPerMillisecond;
        string value = string.Create(
            CultureInfo.InvariantCulture,
            $"{version}:{resourceType}:{narrativeId.Trim()}:{persistedMilliseconds}:{index}");
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        byte[] guidBytes = digest[..16];
        guidBytes[7] = (byte)((guidBytes[7] & 0x0f) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3f) | 0x80);
        return new Guid(guidBytes);
    }
}
