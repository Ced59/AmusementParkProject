using System.Security.Cryptography;
using System.Text;

namespace AmusementPark.Application.Features.FactualEvents.Models;

public static class FactualChangeMaterializationJob
{
    public const string Kind = "watch.materialize-factual-event";
    public const int PayloadVersion = 1;

    public static string BuildIdempotencyKey(string deduplicationKey, long sourceRevision)
    {
        string logicalRevision = $"{deduplicationKey}:{sourceRevision}";
        string digest = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(logicalRevision)));
        return $"{Kind}:{digest}";
    }
}
