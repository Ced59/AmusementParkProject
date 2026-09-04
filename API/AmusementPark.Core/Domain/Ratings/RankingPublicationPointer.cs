namespace AmusementPark.Core.Domain.Ratings;

public sealed class RankingPublicationPointer
{
    public RankingPublicationPointer(
        RankingScopeKey scopeKey,
        RankingSnapshotId currentSnapshotId,
        DateTime currentSnapshotPublishedAtUtc,
        RankingSnapshotId? previousSnapshotId,
        DateTime? previousSnapshotPublishedAtUtc,
        RatingMethodologyVersion methodologyVersion,
        long sourceRevision,
        long highestPublishedSourceRevision,
        long version,
        DateTime updatedAtUtc)
    {
        _ = scopeKey.Value;
        _ = currentSnapshotId.Value;
        if (previousSnapshotId.HasValue)
        {
            _ = previousSnapshotId.Value.Value;
        }

        if (currentSnapshotPublishedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The current snapshot publication timestamp must use UTC.",
                nameof(currentSnapshotPublishedAtUtc));
        }

        if (previousSnapshotId.HasValue != previousSnapshotPublishedAtUtc.HasValue)
        {
            throw new ArgumentException(
                "A previous snapshot and its publication timestamp must be provided together.",
                nameof(previousSnapshotPublishedAtUtc));
        }

        if (previousSnapshotPublishedAtUtc.HasValue &&
            previousSnapshotPublishedAtUtc.Value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The previous snapshot publication timestamp must use UTC.",
                nameof(previousSnapshotPublishedAtUtc));
        }

        _ = methodologyVersion.Value;
        if (sourceRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceRevision));
        }

        if (highestPublishedSourceRevision < sourceRevision)
        {
            throw new ArgumentOutOfRangeException(nameof(highestPublishedSourceRevision));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (updatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must use UTC.", nameof(updatedAtUtc));
        }

        if (previousSnapshotPublishedAtUtc.HasValue &&
            previousSnapshotPublishedAtUtc.Value > updatedAtUtc)
        {
            throw new ArgumentException(
                "The previous snapshot cannot have been published after the pointer update.",
                nameof(previousSnapshotPublishedAtUtc));
        }

        if (currentSnapshotPublishedAtUtc > updatedAtUtc)
        {
            throw new ArgumentException(
                "The current snapshot cannot have been published after the pointer update.",
                nameof(currentSnapshotPublishedAtUtc));
        }

        this.ScopeKey = scopeKey;
        this.CurrentSnapshotId = currentSnapshotId;
        this.CurrentSnapshotPublishedAtUtc = currentSnapshotPublishedAtUtc;
        this.PreviousSnapshotId = previousSnapshotId;
        this.PreviousSnapshotPublishedAtUtc = previousSnapshotPublishedAtUtc;
        this.MethodologyVersion = methodologyVersion;
        this.SourceRevision = sourceRevision;
        this.HighestPublishedSourceRevision = highestPublishedSourceRevision;
        this.Version = version;
        this.UpdatedAtUtc = updatedAtUtc;
    }

    public RankingScopeKey ScopeKey { get; }

    public RankingSnapshotId CurrentSnapshotId { get; }

    public DateTime CurrentSnapshotPublishedAtUtc { get; }

    public RankingSnapshotId? PreviousSnapshotId { get; }

    public DateTime? PreviousSnapshotPublishedAtUtc { get; }

    public RatingMethodologyVersion MethodologyVersion { get; }

    public long SourceRevision { get; }

    public long HighestPublishedSourceRevision { get; }

    public long Version { get; }

    public DateTime UpdatedAtUtc { get; }
}
