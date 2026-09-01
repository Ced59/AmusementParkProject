using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Ratings;

public sealed class RankingSnapshotModelsTests
{
    private static readonly DateTime NowUtc = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly RankingSnapshotChecksum Checksum =
        RankingSnapshotChecksum.Parse(new string('a', RankingSnapshotChecksum.HexadecimalLength));

    [Theory]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef")]
    public void Checksum_WhenValueIsCanonical_ShouldPreserveIt(string value)
    {
        RankingSnapshotChecksum checksum = RankingSnapshotChecksum.Parse(value);

        Assert.Equal(value, checksum.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABCDEF0123456789abcdef0123456789abcdef0123456789abcdef0123456789")]
    [InlineData("g123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef")]
    [InlineData("1234")]
    public void Checksum_WhenValueIsNotCanonical_ShouldRejectIt(string? value)
    {
        Assert.False(RankingSnapshotChecksum.TryParse(value, out RankingSnapshotChecksum checksum));
        Assert.Equal(default, checksum);
    }

    [Fact]
    public void Entry_WhenEvidenceIsEligible_ShouldPreserveTheRankingFacts()
    {
        RankingSnapshotEntry entry = CreateEntry(1, "park-1");

        Assert.Equal(1, entry.Rank);
        Assert.Equal(RatingTargetType.Park, entry.TargetType);
        Assert.Equal("park-1", entry.TargetId);
        Assert.Equal(4.25d, entry.Score);
        Assert.True(entry.Evidence.IsEligibleForMainRanking);
    }

    [Fact]
    public void Entry_WhenEvidenceIsNotEligible_ShouldRejectIt()
    {
        RankingEvidence evidence = CreateEvidence() with
        {
            Level = RankingEvidenceLevel.Provisional,
            IsEligibleForMainRanking = false,
            IneligibilityReason = RankingIneligibilityReason.TooFewUniqueContributors,
        };

        Assert.Throws<ArgumentException>(() => new RankingSnapshotEntry(
            1,
            RatingTargetType.Park,
            "park-1",
            4.25d,
            evidence));
    }

    [Theory]
    [InlineData(-1, 0, 500, 0)]
    [InlineData(5001, 1, 500, 1)]
    [InlineData(10, 11, 500, 1)]
    [InlineData(10, 10, 249, 1)]
    [InlineData(501, 501, 500, 1)]
    public void Header_WhenBoundsOrChunkCountAreInvalid_ShouldRejectIt(
        int totalEntryCount,
        int eligibleEntryCount,
        int chunkSize,
        int chunkCount)
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateHeader(
            RankingSnapshotStatus.Building,
            totalEntryCount,
            eligibleEntryCount,
            chunkSize,
            chunkCount));
    }

    [Fact]
    public void Header_WhenLifecycleMetadataDoesNotMatchStatus_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new RankingSnapshotHeader(
            RankingSnapshotId.Parse("snapshot-1"),
            RankingScopeKey.Parse("parks:global"),
            RankingEligibilityPolicy.InitialMethodologyVersion,
            sourceRevision: 4,
            RankingSnapshotStatus.Current,
            totalEntryCount: 3,
            eligibleEntryCount: 3,
            chunkSize: 500,
            chunkCount: 1,
            Checksum,
            NowUtc));
    }

    [Fact]
    public void Chunk_WhenRanksAreNotContiguous_ShouldRejectIt()
    {
        Assert.Throws<ArgumentException>(() => new RankingSnapshotChunk(
            RankingSnapshotId.Parse("snapshot-1"),
            0,
            new[] { CreateEntry(1, "park-1"), CreateEntry(3, "park-3") },
            Checksum));
    }

    [Fact]
    public void Chunk_ShouldExposeAnImmutableEntryCollection()
    {
        RankingSnapshotChunk chunk = new RankingSnapshotChunk(
            RankingSnapshotId.Parse("snapshot-1"),
            0,
            new[] { CreateEntry(1, "park-1") },
            Checksum);
        ICollection<RankingSnapshotEntry> entries =
            Assert.IsAssignableFrom<ICollection<RankingSnapshotEntry>>(chunk.Entries);

        Assert.True(entries.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => entries.Add(CreateEntry(2, "park-2")));
    }

    [Fact]
    public void Pointer_WhenVersionIsNotPositive_ShouldRejectIt()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RankingPublicationPointer(
            RankingScopeKey.Parse("parks:global"),
            RankingSnapshotId.Parse("snapshot-1"),
            null,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            sourceRevision: 4,
            version: 0,
            NowUtc));
    }

    private static RankingSnapshotHeader CreateHeader(
        RankingSnapshotStatus status,
        int totalEntryCount,
        int eligibleEntryCount,
        int chunkSize,
        int chunkCount)
    {
        return new RankingSnapshotHeader(
            RankingSnapshotId.Parse("snapshot-1"),
            RankingScopeKey.Parse("parks:global"),
            RankingEligibilityPolicy.InitialMethodologyVersion,
            sourceRevision: 4,
            status,
            totalEntryCount,
            eligibleEntryCount,
            chunkSize,
            chunkCount,
            Checksum,
            NowUtc);
    }

    private static RankingSnapshotEntry CreateEntry(int rank, string targetId)
    {
        return new RankingSnapshotEntry(
            rank,
            RatingTargetType.Park,
            targetId,
            4.25d,
            CreateEvidence());
    }

    private static RankingEvidence CreateEvidence()
    {
        return new RankingEvidence(
            RankingEvidenceLevel.Eligible,
            true,
            12,
            12,
            12,
            0,
            0,
            0,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            null)
        {
            NextContributorThreshold = 30,
        };
    }
}
