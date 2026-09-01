using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RankingSnapshotIntegrityValidatorTests
{
    private static readonly DateTime NowUtc = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
    private readonly RankingSnapshotChecksumCalculator checksumCalculator = new RankingSnapshotChecksumCalculator();

    [Fact]
    public void Validate_WhenChunksMatchHeader_ShouldAcceptTheBuild()
    {
        SnapshotFixture fixture = this.CreateFixture(eligibleEntryCount: 501);

        RankingSnapshotIntegrityResult result = this.CreateValidator().Validate(
            fixture.Header,
            fixture.Chunks,
            CanonicalRankingScopes.GlobalParks);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void Validate_WhenAChunkIsMissing_ShouldRejectTheBuild()
    {
        SnapshotFixture fixture = this.CreateFixture(eligibleEntryCount: 501);

        RankingSnapshotIntegrityResult result = this.CreateValidator().Validate(
            fixture.Header,
            fixture.Chunks.Take(1).ToList(),
            CanonicalRankingScopes.GlobalParks);

        Assert.False(result.IsValid);
        Assert.Equal(RankingSnapshotErrorCodes.ChunkCountMismatch, result.ErrorCode);
    }

    [Fact]
    public void Validate_WhenAChunkChecksumWasAltered_ShouldRejectTheBuild()
    {
        SnapshotFixture fixture = this.CreateFixture(eligibleEntryCount: 3);
        RankingSnapshotChunk original = Assert.Single(fixture.Chunks);
        RankingSnapshotChunk corrupted = new RankingSnapshotChunk(
            original.SnapshotId,
            original.ChunkIndex,
            original.Entries,
            RankingSnapshotChecksum.Parse(new string('f', RankingSnapshotChecksum.HexadecimalLength)));

        RankingSnapshotIntegrityResult result = this.CreateValidator().Validate(
            fixture.Header,
            new[] { corrupted },
            CanonicalRankingScopes.GlobalParks);

        Assert.False(result.IsValid);
        Assert.Equal(RankingSnapshotErrorCodes.ChunkChecksumMismatch, result.ErrorCode);
    }

    [Fact]
    public void Validate_WhenOverallChecksumWasAltered_ShouldRejectTheBuild()
    {
        SnapshotFixture fixture = this.CreateFixture(eligibleEntryCount: 3);
        RankingSnapshotHeader corruptedHeader = CreateHeader(
            fixture.Header.Id,
            fixture.Header.TotalEntryCount,
            fixture.Header.EligibleEntryCount,
            fixture.Header.ChunkCount,
            RankingSnapshotChecksum.Parse(new string('f', RankingSnapshotChecksum.HexadecimalLength)));

        RankingSnapshotIntegrityResult result = this.CreateValidator().Validate(
            corruptedHeader,
            fixture.Chunks,
            CanonicalRankingScopes.GlobalParks);

        Assert.False(result.IsValid);
        Assert.Equal(RankingSnapshotErrorCodes.SnapshotChecksumMismatch, result.ErrorCode);
    }

    [Fact]
    public void Validate_WhenTargetFamilyDoesNotMatchScope_ShouldRejectTheBuild()
    {
        RankingSnapshotId snapshotId = RankingSnapshotId.Parse("snapshot-1");
        RankingSnapshotEntry itemEntry = CreateEntry(1, "item-1", RatingTargetType.ParkItem);
        RankingSnapshotChunk chunk = CreateChunk(snapshotId, 0, new[] { itemEntry });
        RankingSnapshotHeader header = CreateHeader(
            snapshotId,
            totalEntryCount: 1,
            eligibleEntryCount: 1,
            chunkCount: 1,
            this.checksumCalculator.CalculateSnapshot(1, 1, 500, new[] { chunk }));

        RankingSnapshotIntegrityResult result = this.CreateValidator().Validate(
            header,
            new[] { chunk },
            CanonicalRankingScopes.GlobalParks);

        Assert.False(result.IsValid);
        Assert.Equal(RankingSnapshotErrorCodes.TargetFamilyMismatch, result.ErrorCode);
    }

    [Fact]
    public void CalculateChunk_WhenAnyRankingFactChanges_ShouldChangeTheChecksum()
    {
        RankingSnapshotEntry first = CreateEntry(1, "park-1", RatingTargetType.Park);
        RankingSnapshotEntry changed = new RankingSnapshotEntry(
            1,
            RatingTargetType.Park,
            "park-1",
            4.5d,
            CreateEvidence());

        RankingSnapshotChecksum original = this.checksumCalculator.CalculateChunk(new[] { first });
        RankingSnapshotChecksum modified = this.checksumCalculator.CalculateChunk(new[] { changed });

        Assert.NotEqual(original, modified);
        Assert.Equal(RankingSnapshotChecksum.HexadecimalLength, original.Value.Length);
    }

    private SnapshotFixture CreateFixture(int eligibleEntryCount)
    {
        RankingSnapshotId snapshotId = RankingSnapshotId.Parse("snapshot-1");
        List<RankingSnapshotEntry> entries = Enumerable.Range(1, eligibleEntryCount)
            .Select(rank => CreateEntry(rank, $"park-{rank}", RatingTargetType.Park))
            .ToList();
        List<RankingSnapshotChunk> chunks = entries
            .Chunk(500)
            .Select((items, index) => CreateChunk(snapshotId, index, items))
            .ToList();
        RankingSnapshotChecksum checksum = this.checksumCalculator.CalculateSnapshot(
            eligibleEntryCount,
            eligibleEntryCount,
            500,
            chunks);
        RankingSnapshotHeader header = CreateHeader(
            snapshotId,
            eligibleEntryCount,
            eligibleEntryCount,
            chunks.Count,
            checksum);
        return new SnapshotFixture(header, chunks);
    }

    private RankingSnapshotChunk CreateChunk(
        RankingSnapshotId snapshotId,
        int chunkIndex,
        IReadOnlyCollection<RankingSnapshotEntry> entries)
    {
        return new RankingSnapshotChunk(
            snapshotId,
            chunkIndex,
            entries,
            this.checksumCalculator.CalculateChunk(entries));
    }

    private static RankingSnapshotHeader CreateHeader(
        RankingSnapshotId snapshotId,
        int totalEntryCount,
        int eligibleEntryCount,
        int chunkCount,
        RankingSnapshotChecksum checksum)
    {
        return new RankingSnapshotHeader(
            snapshotId,
            RankingScopeKey.Parse("parks:global"),
            RankingEligibilityPolicy.InitialMethodologyVersion,
            sourceRevision: 7,
            RankingSnapshotStatus.Building,
            totalEntryCount,
            eligibleEntryCount,
            chunkSize: 500,
            chunkCount,
            checksum,
            NowUtc);
    }

    private static RankingSnapshotEntry CreateEntry(
        int rank,
        string targetId,
        RatingTargetType targetType)
    {
        return new RankingSnapshotEntry(rank, targetType, targetId, 4.25d, CreateEvidence());
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

    private RankingSnapshotIntegrityValidator CreateValidator()
    {
        return new RankingSnapshotIntegrityValidator(this.checksumCalculator);
    }

    private sealed record SnapshotFixture(
        RankingSnapshotHeader Header,
        IReadOnlyCollection<RankingSnapshotChunk> Chunks);
}
