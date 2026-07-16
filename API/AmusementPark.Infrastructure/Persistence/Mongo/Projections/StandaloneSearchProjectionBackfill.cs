namespace AmusementPark.Infrastructure.Persistence.Mongo.Projections;

internal static class StandaloneSearchProjectionBackfill
{
    private const string SearchOriginalIdPrefix = "standaloneAttraction_";

    public static IReadOnlyCollection<string> BuildProjectionOriginalIds(IEnumerable<string?> standaloneAttractionIds)
    {
        List<string> originalIds = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (string? standaloneAttractionId in standaloneAttractionIds)
        {
            string? normalizedId = NormalizeStandaloneAttractionId(standaloneAttractionId);
            if (normalizedId is null || !seen.Add(normalizedId))
            {
                continue;
            }

            originalIds.Add($"{SearchOriginalIdPrefix}{normalizedId}");
        }

        return originalIds;
    }

    public static IReadOnlyCollection<string> ResolveMissingStandaloneAttractionIds(
        IEnumerable<string?> standaloneAttractionIds,
        IEnumerable<string?> existingProjectionOriginalIds)
    {
        HashSet<string> existingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (string? existingProjectionOriginalId in existingProjectionOriginalIds)
        {
            if (TryResolveStandaloneAttractionId(existingProjectionOriginalId, out string standaloneAttractionId))
            {
                existingIds.Add(standaloneAttractionId);
            }
        }

        List<string> missingIds = new List<string>();
        HashSet<string> seenSourceIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (string? standaloneAttractionId in standaloneAttractionIds)
        {
            string? normalizedId = NormalizeStandaloneAttractionId(standaloneAttractionId);
            if (normalizedId is null || !seenSourceIds.Add(normalizedId) || existingIds.Contains(normalizedId))
            {
                continue;
            }

            missingIds.Add(normalizedId);
        }

        return missingIds;
    }

    private static bool TryResolveStandaloneAttractionId(string? projectionOriginalId, out string standaloneAttractionId)
    {
        if (!string.IsNullOrWhiteSpace(projectionOriginalId) && projectionOriginalId.StartsWith(SearchOriginalIdPrefix, StringComparison.Ordinal))
        {
            standaloneAttractionId = projectionOriginalId[SearchOriginalIdPrefix.Length..].Trim();
            return !string.IsNullOrWhiteSpace(standaloneAttractionId);
        }

        standaloneAttractionId = string.Empty;
        return false;
    }

    private static string? NormalizeStandaloneAttractionId(string? standaloneAttractionId)
    {
        if (string.IsNullOrWhiteSpace(standaloneAttractionId))
        {
            return null;
        }

        return standaloneAttractionId.Trim();
    }
}
