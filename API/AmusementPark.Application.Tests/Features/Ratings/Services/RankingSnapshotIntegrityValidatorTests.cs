using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
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
        RankingSnapshotIntegrityFixture fixture = this.CreateFixture(eligibleEntryCount: 501);

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
        RankingSnapshotIntegrityFixture fixture = this.CreateFixture(eligibleEntryCount: 501);

        RankingSnapshotIntegrityResult result = this.CreateValidator().Validate(
            fixture.Header,
            fixture.Chunks.Take(1).ToList(),
            CanonicalRankingScopes.GlobalParks);

        Assert.False(result.IsValid);
        Assert.Equal(RankingSnapshotErrorCodes.ChunkCountMismatch, result.ErrorCode);
    }

    [Fact]
    public void Validate_WhenChunkBelongsToAnEarlierBuildAttempt_ShouldRejectTheBuild()
    {
        RankingSnapshotIntegrityFixture fixture = this.CreateFixture(eligibleEntryCount: 3);
        RankingSnapshotHeader restartedHeader = CreateHeader(
            fixture.Header.Id,
            fixture.Header.TotalEntryCount,
            fixture.Header.EligibleEntryCount,
            fixture.Header.ChunkCount,
            fixture.Header.Checksum,
            buildAttempt: 2);

        RankingSnapshotIntegrityResult result = this.CreateValidator().Validate(
            restartedHeader,
            fixture.Chunks,
            CanonicalRankingScopes.GlobalParks);

        Assert.False(result.IsValid);
        Assert.Equal(RankingSnapshotErrorCodes.BuildAttemptMismatch, result.ErrorCode);
    }

    [Fact]
    public void Validate_WhenAChunkChecksumWasAltered_ShouldRejectTheBuild()
    {
        RankingSnapshotIntegrityFixture fixture = this.CreateFixture(eligibleEntryCount: 3);
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
        RankingSnapshotIntegrityFixture fixture = this.CreateFixture(eligibleEntryCount: 3);
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
    public void Validate_WhenParkItemCategoryDoesNotMatchScope_ShouldRejectTheBuild()
    {
        RankingScopeDefinition attractionScope = CanonicalRankingScopes.PublicItemCategories
            .Single(static scope => scope.Filter.ParkItemCategory == ParkItemCategory.Attraction);
        RankingSnapshotId snapshotId = RankingSnapshotId.Parse("snapshot-wrong-category");
        RankingSnapshotEntry restaurantEntry = new RankingSnapshotEntry(
            position: 1,
            rank: 1,
            RatingTargetType.ParkItem,
            "item-1",
            ParkItemCategory.Restaurant,
            4.25d,
            CreateSimpleEvidence());
        RankingSnapshotChunk chunk = CreateChunk(snapshotId, 0, new[] { restaurantEntry });
        RankingSnapshotHeader header = CreateHeader(
            snapshotId,
            totalEntryCount: 1,
            eligibleEntryCount: 1,
            chunkCount: 1,
            this.checksumCalculator.CalculateSnapshot(1, 1, 500, new[] { chunk }),
            attractionScope.Key);

        RankingSnapshotIntegrityResult result = this.CreateValidator().Validate(
            header,
            new[] { chunk },
            attractionScope);

        Assert.False(result.IsValid);
        Assert.Equal(RankingSnapshotErrorCodes.ScopeFilterMismatch, result.ErrorCode);
    }

    [Fact]
    public void Validate_WhenScoresAreTied_ShouldAcceptSharedCompetitionRanks()
    {
        RankingSnapshotId snapshotId = RankingSnapshotId.Parse("snapshot-ties");
        RankingSnapshotEntry[] entries =
        {
            CreateEntry(position: 1, rank: 1, "park-1", RatingTargetType.Park, 4.5d),
            CreateEntry(position: 2, rank: 1, "park-2", RatingTargetType.Park, 4.49995d),
            CreateEntry(position: 3, rank: 3, "park-3", RatingTargetType.Park, 4.25d),
        };
        RankingSnapshotChunk chunk = CreateChunk(snapshotId, 0, entries);
        RankingSnapshotHeader header = CreateHeader(
            snapshotId,
            entries.Length,
            entries.Length,
            1,
            this.checksumCalculator.CalculateSnapshot(entries.Length, entries.Length, 500, new[] { chunk }));

        RankingSnapshotIntegrityResult result = this.CreateValidator().Validate(
            header,
            new[] { chunk },
            CanonicalRankingScopes.GlobalParks);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WhenTiedScoresUseSequentialRanks_ShouldRejectTheBuild()
    {
        RankingSnapshotId snapshotId = RankingSnapshotId.Parse("snapshot-invalid-ties");
        RankingSnapshotEntry[] entries =
        {
            CreateEntry(position: 1, rank: 1, "park-1", RatingTargetType.Park, 4.5d),
            CreateEntry(position: 2, rank: 2, "park-2", RatingTargetType.Park, 4.49995d),
            CreateEntry(position: 3, rank: 3, "park-3", RatingTargetType.Park, 4.25d),
        };
        RankingSnapshotChunk chunk = CreateChunk(snapshotId, 0, entries);
        RankingSnapshotHeader header = CreateHeader(
            snapshotId,
            entries.Length,
            entries.Length,
            1,
            this.checksumCalculator.CalculateSnapshot(entries.Length, entries.Length, 500, new[] { chunk }));

        RankingSnapshotIntegrityResult result = this.CreateValidator().Validate(
            header,
            new[] { chunk },
            CanonicalRankingScopes.GlobalParks);

        Assert.False(result.IsValid);
        Assert.Equal(RankingSnapshotErrorCodes.RankSequenceInvalid, result.ErrorCode);
    }

    [Fact]
    public void Validate_WhenAdjacentScoresExtendATieBeyondItsAnchor_ShouldRejectTheBuild()
    {
        RankingSnapshotId snapshotId = RankingSnapshotId.Parse("snapshot-transitive-tie");
        RankingSnapshotEntry[] entries =
        {
            CreateEntry(position: 1, rank: 1, "park-1", RatingTargetType.Park, 4d),
            CreateEntry(position: 2, rank: 1, "park-2", RatingTargetType.Park, 3.99994d),
            CreateEntry(position: 3, rank: 1, "park-3", RatingTargetType.Park, 3.99988d),
        };
        RankingSnapshotChunk chunk = CreateChunk(snapshotId, 0, entries);
        RankingSnapshotHeader header = CreateHeader(
            snapshotId,
            entries.Length,
            entries.Length,
            1,
            this.checksumCalculator.CalculateSnapshot(entries.Length, entries.Length, 500, new[] { chunk }));

        RankingSnapshotIntegrityResult result = this.CreateValidator().Validate(
            header,
            new[] { chunk },
            CanonicalRankingScopes.GlobalParks);

        Assert.False(result.IsValid);
        Assert.Equal(RankingSnapshotErrorCodes.RankSequenceInvalid, result.ErrorCode);
    }

    [Fact]
    public void Validate_WhenScoresIncreaseWithinTheTieEpsilon_ShouldRejectTheBuild()
    {
        RankingSnapshotId snapshotId = RankingSnapshotId.Parse("snapshot-invalid-score-order");
        RankingSnapshotEntry[] entries =
        {
            CreateEntry(position: 1, rank: 1, "park-1", RatingTargetType.Park, 4.5d),
            CreateEntry(position: 2, rank: 1, "park-2", RatingTargetType.Park, 4.50005d),
            CreateEntry(position: 3, rank: 3, "park-3", RatingTargetType.Park, 4.25d),
        };
        RankingSnapshotChunk chunk = CreateChunk(snapshotId, 0, entries);
        RankingSnapshotHeader header = CreateHeader(
            snapshotId,
            entries.Length,
            entries.Length,
            1,
            this.checksumCalculator.CalculateSnapshot(entries.Length, entries.Length, 500, new[] { chunk }));

        RankingSnapshotIntegrityResult result = this.CreateValidator().Validate(
            header,
            new[] { chunk },
            CanonicalRankingScopes.GlobalParks);

        Assert.False(result.IsValid);
        Assert.Equal(RankingSnapshotErrorCodes.ScoreOrderInvalid, result.ErrorCode);
    }

    [Fact]
    public void Validate_WhenATieCrossesAChunkBoundary_ShouldPreserveTheSharedRank()
    {
        RankingSnapshotId snapshotId = RankingSnapshotId.Parse("snapshot-cross-chunk-tie");
        List<RankingSnapshotEntry> entries = Enumerable.Range(1, 501)
            .Select(position => CreateEntry(
                position,
                position,
                $"park-{position}",
                RatingTargetType.Park,
                5d - (position * 0.001d)))
            .ToList();
        RankingSnapshotEntry positionFiveHundred = entries[499];
        entries[500] = CreateEntry(
            position: 501,
            rank: 500,
            "park-501",
            RatingTargetType.Park,
            positionFiveHundred.Score);
        List<RankingSnapshotChunk> chunks = entries
            .Chunk(500)
            .Select((items, index) => CreateChunk(snapshotId, index, items))
            .ToList();
        RankingSnapshotHeader header = CreateHeader(
            snapshotId,
            entries.Count,
            entries.Count,
            chunks.Count,
            this.checksumCalculator.CalculateSnapshot(entries.Count, entries.Count, 500, chunks));

        RankingSnapshotIntegrityResult result = this.CreateValidator().Validate(
            header,
            chunks,
            CanonicalRankingScopes.GlobalParks);

        Assert.True(result.IsValid);
        Assert.Equal(500, chunks[1].Entries.Single().Rank);
        Assert.Equal(501, chunks[1].Entries.Single().Position);
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

    [Fact]
    public void CalculateChunk_WhenParkItemCategoryChanges_ShouldChangeTheChecksum()
    {
        RankingSnapshotEntry attraction = new RankingSnapshotEntry(
            1,
            1,
            RatingTargetType.ParkItem,
            "item-1",
            ParkItemCategory.Attraction,
            4.25d,
            CreateSimpleEvidence());
        RankingSnapshotEntry restaurant = new RankingSnapshotEntry(
            1,
            1,
            RatingTargetType.ParkItem,
            "item-1",
            ParkItemCategory.Restaurant,
            4.25d,
            CreateSimpleEvidence());

        Assert.NotEqual(
            this.checksumCalculator.CalculateChunk(new[] { attraction }),
            this.checksumCalculator.CalculateChunk(new[] { restaurant }));
    }

    [Fact]
    public void CalculateChunk_WhenSingleCategoryExceptionChanges_ShouldChangeTheChecksum()
    {
        RankingSnapshotEntry regularPark = CreateEntry(1, "park-1", RatingTargetType.Park);
        RankingSnapshotEntry singleCategoryPark = new RankingSnapshotEntry(
            1,
            RatingTargetType.Park,
            "park-1",
            4.25d,
            CreateEvidence() with { IsSingleCategoryParkException = true });

        Assert.NotEqual(
            this.checksumCalculator.CalculateChunk(new[] { regularPark }),
            this.checksumCalculator.CalculateChunk(new[] { singleCategoryPark }));
    }

    [Fact]
    public void CalculateChunk_WhenPublicCategoryEvidenceChanges_ShouldChangeTheChecksum()
    {
        RankingSnapshotEntry onePublicCategory = CreateEntry(1, "park-1", RatingTargetType.Park);
        RankingSnapshotEntry twoPublicCategories = new RankingSnapshotEntry(
            1,
            RatingTargetType.Park,
            "park-1",
            4.25d,
            CreateEvidence() with { PublicItemCategoryCount = 2 });

        Assert.NotEqual(
            this.checksumCalculator.CalculateChunk(new[] { onePublicCategory }),
            this.checksumCalculator.CalculateChunk(new[] { twoPublicCategories }));
    }

    private RankingSnapshotIntegrityFixture CreateFixture(int eligibleEntryCount)
    {
        RankingSnapshotId snapshotId = RankingSnapshotId.Parse("snapshot-1");
        List<RankingSnapshotEntry> entries = Enumerable.Range(1, eligibleEntryCount)
            .Select(position => CreateEntry(
                position,
                position,
                $"park-{position}",
                RatingTargetType.Park,
                5d - (position * 0.001d)))
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
        return new RankingSnapshotIntegrityFixture(header, chunks);
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
        RankingSnapshotChecksum checksum,
        RankingScopeKey? scopeKey = null,
        int buildAttempt = 1)
    {
        return new RankingSnapshotHeader(
            snapshotId,
            scopeKey ?? RankingScopeKey.Parse("parks:global"),
            RankingEligibilityPolicy.InitialMethodologyVersion,
            sourceRevision: 7,
            RankingSnapshotStatus.Building,
            totalEntryCount,
            eligibleEntryCount,
            chunkSize: 500,
            chunkCount,
            checksum,
            NowUtc,
            buildAttempt: buildAttempt);
    }

    private static RankingSnapshotEntry CreateEntry(
        int rank,
        string targetId,
        RatingTargetType targetType)
    {
        return CreateEntry(rank, rank, targetId, targetType, 4.25d);
    }

    private static RankingSnapshotEntry CreateEntry(
        int position,
        int rank,
        string targetId,
        RatingTargetType targetType,
        double score)
    {
        ParkItemCategory? parkItemCategory = targetType == RatingTargetType.ParkItem
            ? ParkItemCategory.Attraction
            : null;
        return new RankingSnapshotEntry(
            position,
            rank,
            targetType,
            targetId,
            parkItemCategory,
            score,
            targetType == RatingTargetType.ParkItem
                ? CreateSimpleEvidence()
                : CreateEvidence());
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
            IsSingleCategoryParkException = false,
            PublicItemCategoryCount = 1,
        };
    }

    private static RankingEvidence CreateSimpleEvidence()
    {
        return RankingEligibilityPolicy.Initial.EvaluateSimpleTarget(
            new SimpleRankingEvidenceInput(
                UniqueContributorCount: 12,
                RatingObservationCount: 12,
                TargetCanReceiveVisitorRatings: true,
                IsExcludedByModeration: false,
                AggregateIntegrityIsValid: true));
    }

    private RankingSnapshotIntegrityValidator CreateValidator()
    {
        return new RankingSnapshotIntegrityValidator(this.checksumCalculator);
    }
}
