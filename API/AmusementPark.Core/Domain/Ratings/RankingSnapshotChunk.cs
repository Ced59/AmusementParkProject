namespace AmusementPark.Core.Domain.Ratings;

public sealed class RankingSnapshotChunk
{
    public RankingSnapshotChunk(
        RankingSnapshotId snapshotId,
        int chunkIndex,
        IReadOnlyCollection<RankingSnapshotEntry> entries,
        RankingSnapshotChecksum checksum,
        int buildAttempt = 1)
    {
        _ = snapshotId.Value;
        if (chunkIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkIndex));
        }

        ArgumentNullException.ThrowIfNull(entries);
        RankingSnapshotEntry[] materializedEntries = entries.ToArray();
        if (materializedEntries.Length == 0 ||
            materializedEntries.Length > RankingScopeDefinition.MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(entries));
        }

        if (buildAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildAttempt));
        }

        for (int index = 0; index < materializedEntries.Length; index++)
        {
            ArgumentNullException.ThrowIfNull(materializedEntries[index]);
            if (index > 0 && materializedEntries[index].Position != materializedEntries[index - 1].Position + 1)
            {
                throw new ArgumentException("Chunk positions must be contiguous.", nameof(entries));
            }

            if (index > 0 && materializedEntries[index].Rank < materializedEntries[index - 1].Rank)
            {
                throw new ArgumentException("Public ranks must be ordered.", nameof(entries));
            }
        }

        _ = checksum.Value;
        this.SnapshotId = snapshotId;
        this.ChunkIndex = chunkIndex;
        this.Entries = Array.AsReadOnly(materializedEntries);
        this.Checksum = checksum;
        this.BuildAttempt = buildAttempt;
    }

    public RankingSnapshotId SnapshotId { get; }

    public int ChunkIndex { get; }

    public IReadOnlyCollection<RankingSnapshotEntry> Entries { get; }

    public int FirstPosition => this.Entries.First().Position;

    public int LastPosition => this.Entries.Last().Position;

    public int FirstRank => this.Entries.First().Rank;

    public int LastRank => this.Entries.Last().Rank;

    public RankingSnapshotChecksum Checksum { get; }

    public int BuildAttempt { get; }
}
