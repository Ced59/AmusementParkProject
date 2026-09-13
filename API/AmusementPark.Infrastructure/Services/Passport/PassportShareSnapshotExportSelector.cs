namespace AmusementPark.Infrastructure.Services.Passport;

internal static class PassportShareSnapshotExportSelector
{
    public static IReadOnlyCollection<TSnapshot> SelectLatestRetained<TSnapshot>(
        IEnumerable<TSnapshot> snapshots,
        IReadOnlyDictionary<string, long> publicationVersionUpperBounds,
        Func<TSnapshot, string> getPublicationId,
        Func<TSnapshot, long> getPublicationVersion,
        Func<TSnapshot, DateTime> getCreatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        ArgumentNullException.ThrowIfNull(publicationVersionUpperBounds);
        ArgumentNullException.ThrowIfNull(getPublicationId);
        ArgumentNullException.ThrowIfNull(getPublicationVersion);
        ArgumentNullException.ThrowIfNull(getCreatedAtUtc);

        return snapshots
            .Where(snapshot =>
                publicationVersionUpperBounds.TryGetValue(
                    getPublicationId(snapshot),
                    out long upperBound)
                && getPublicationVersion(snapshot) <= upperBound)
            .GroupBy(getPublicationId, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(getPublicationVersion)
                .First())
            .OrderBy(getCreatedAtUtc)
            .ThenBy(getPublicationId, StringComparer.Ordinal)
            .ToArray();
    }
}
