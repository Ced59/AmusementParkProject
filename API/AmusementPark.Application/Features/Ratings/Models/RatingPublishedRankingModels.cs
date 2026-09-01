using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingPublishedRank(
    int Rank,
    RatingMethodologyVersion MethodologyVersion,
    DateTime? GeneratedAtUtc);

public sealed class RatingPublishedRankingSnapshot
{
    private readonly IReadOnlyDictionary<string, RankingSnapshotEntry> entriesByTargetId;

    public RatingPublishedRankingSnapshot(
        RankingScopeKey scopeKey,
        RankingSnapshotId snapshotId,
        RatingMethodologyVersion methodologyVersion,
        long sourceRevision,
        long pointerVersion,
        DateTime generatedAtUtc,
        IReadOnlyCollection<RankingSnapshotEntry> entries)
    {
        _ = scopeKey.Value;
        _ = snapshotId.Value;
        _ = methodologyVersion.Value;
        if (sourceRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceRevision));
        }

        if (pointerVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pointerVersion));
        }

        if (generatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must use UTC.", nameof(generatedAtUtc));
        }

        ArgumentNullException.ThrowIfNull(entries);
        RankingSnapshotEntry[] materializedEntries = entries.ToArray();
        this.entriesByTargetId = materializedEntries.ToDictionary(
            static entry => entry.TargetId,
            StringComparer.Ordinal);
        this.ScopeKey = scopeKey;
        this.SnapshotId = snapshotId;
        this.MethodologyVersion = methodologyVersion;
        this.SourceRevision = sourceRevision;
        this.PointerVersion = pointerVersion;
        this.GeneratedAtUtc = generatedAtUtc;
        this.Entries = Array.AsReadOnly(materializedEntries);
    }

    public RankingScopeKey ScopeKey { get; }

    public RankingSnapshotId SnapshotId { get; }

    public RatingMethodologyVersion MethodologyVersion { get; }

    public long SourceRevision { get; }

    public long PointerVersion { get; }

    public DateTime GeneratedAtUtc { get; }

    public IReadOnlyCollection<RankingSnapshotEntry> Entries { get; }

    public RankingSnapshotEntry? FindEntry(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return null;
        }

        return this.entriesByTargetId.TryGetValue(targetId.Trim(), out RankingSnapshotEntry? entry)
            ? entry
            : null;
    }
}
