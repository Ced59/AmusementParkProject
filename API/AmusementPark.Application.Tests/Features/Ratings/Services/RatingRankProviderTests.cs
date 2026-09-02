using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RatingRankProviderTests
{
    private static readonly DateTime GeneratedAtUtc =
        new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PublishedAtUtc =
        new DateTime(2026, 9, 1, 8, 5, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetRankAsync_WhenEligibilityIsDisabled_ShouldWithholdRankWithoutReadingSources()
    {
        ProviderFixture fixture = new ProviderFixture(eligibilityEnabled: false);

        RatingPublishedRank? result = await fixture.Provider.GetRankAsync(
            CreateAggregate("park-2"),
            CancellationToken.None);

        Assert.Null(result);
        fixture.Ratings.VerifyNoOtherCalls();
        fixture.Snapshots.VerifyNoOtherCalls();
        fixture.Revisions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetRankAsync_WhenPublishedSnapshotIsCurrentAndValid_ShouldReturnItsCompetitionRank()
    {
        ProviderFixture fixture = new ProviderFixture();
        SnapshotFixture snapshot = fixture.CreateSnapshot(3, sourceRevision: 7);
        fixture.SetupCurrent(snapshot);

        RatingPublishedRank? result = await fixture.Provider.GetRankAsync(
            CreateAggregate("park-2"),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result.Rank);
        Assert.Equal(RankingEligibilityPolicy.InitialMethodologyVersion, result.MethodologyVersion);
        Assert.Equal(GeneratedAtUtc, result.GeneratedAtUtc);
        fixture.Snapshots.Verify(repository => repository.GetCurrentPageAsync(
            CanonicalRankingScopes.GlobalParks.Key,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            0,
            500,
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task GetCanonicalSnapshotAsync_WhenPointerBackedHeaderAwaitsStatusReconciliation_ShouldReturnSnapshot()
    {
        ProviderFixture fixture = new ProviderFixture();
        SnapshotFixture snapshot = fixture.CreateSnapshot(
            3,
            sourceRevision: 7,
            awaitingStatusReconciliation: true);
        fixture.SetupCurrent(snapshot);

        RatingPublishedRankingSnapshot? result = await fixture.Provider.GetCanonicalSnapshotAsync(
            RatingTargetType.Park,
            null,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(snapshot.Header.Id, result.SnapshotId);
        Assert.Equal(3, result.Entries.Count);
    }

    [Fact]
    public async Task GetCanonicalSnapshotAsync_WhenSourceMutationIsPending_ShouldWithholdRankWithoutReadingPointer()
    {
        ProviderFixture fixture = new ProviderFixture();
        fixture.Revisions
            .Setup(repository => repository.GetAsync(
                CanonicalRankingScopes.GlobalParks.Key,
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                CanonicalRankingScopes.GlobalParks.Key,
                7,
                GeneratedAtUtc,
                PendingMutationCount: 1));

        RatingPublishedRankingSnapshot? result = await fixture.Provider.GetCanonicalSnapshotAsync(
            RatingTargetType.Park,
            null,
            CancellationToken.None);

        Assert.Null(result);
        fixture.Snapshots.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetCanonicalSnapshotAsync_WhenSourceRevisionIsMissing_ShouldWithholdRankWithoutReadingPointer()
    {
        ProviderFixture fixture = new ProviderFixture();
        fixture.Revisions
            .Setup(repository => repository.GetAsync(
                CanonicalRankingScopes.GlobalParks.Key,
                CancellationToken.None))
            .ReturnsAsync((RatingRankingSourceRevision?)null);

        RatingPublishedRankingSnapshot? result = await fixture.Provider.GetCanonicalSnapshotAsync(
            RatingTargetType.Park,
            null,
            CancellationToken.None);

        Assert.Null(result);
        fixture.Snapshots.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetCanonicalSnapshotAsync_WhenPointerIsBehindSourceRevision_ShouldWithholdRank()
    {
        ProviderFixture fixture = new ProviderFixture();
        SnapshotFixture snapshot = fixture.CreateSnapshot(3, sourceRevision: 7);
        fixture.Revisions
            .Setup(repository => repository.GetAsync(
                CanonicalRankingScopes.GlobalParks.Key,
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                CanonicalRankingScopes.GlobalParks.Key,
                8,
                GeneratedAtUtc));
        fixture.Snapshots
            .Setup(repository => repository.GetPointerAsync(
                CanonicalRankingScopes.GlobalParks.Key,
                CancellationToken.None))
            .ReturnsAsync(snapshot.Pointer);

        RatingPublishedRankingSnapshot? result = await fixture.Provider.GetCanonicalSnapshotAsync(
            RatingTargetType.Park,
            null,
            CancellationToken.None);

        Assert.Null(result);
        fixture.Snapshots.Verify(repository => repository.GetCurrentHeaderAsync(
            It.IsAny<RankingScopeKey>(),
            It.IsAny<RatingMethodologyVersion>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCanonicalSnapshotAsync_WhenSnapshotPageIsMissing_ShouldNeverFallBackToLegacyRanks()
    {
        ProviderFixture fixture = new ProviderFixture();
        SnapshotFixture snapshot = fixture.CreateSnapshot(3, sourceRevision: 7);
        fixture.SetupRevisionAndPointer(snapshot);
        fixture.Snapshots
            .Setup(repository => repository.GetCurrentHeaderAsync(
                snapshot.Header.ScopeKey,
                snapshot.Header.MethodologyVersion,
                CancellationToken.None))
            .ReturnsAsync(snapshot.Header);
        fixture.Snapshots
            .Setup(repository => repository.GetCurrentPageAsync(
                snapshot.Header.ScopeKey,
                snapshot.Header.MethodologyVersion,
                0,
                500,
                CancellationToken.None))
            .ReturnsAsync((RankingSnapshotPage?)null);

        RatingPublishedRankingSnapshot? result = await fixture.Provider.GetCanonicalSnapshotAsync(
            RatingTargetType.Park,
            null,
            CancellationToken.None);

        Assert.Null(result);
        fixture.Ratings.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetCanonicalSnapshotAsync_WhenPublicationChangesDuringRead_ShouldDiscardLoadedSnapshot()
    {
        ProviderFixture fixture = new ProviderFixture();
        SnapshotFixture snapshot = fixture.CreateSnapshot(3, sourceRevision: 7);
        RankingPublicationPointer replacement = fixture.CreatePointer(
            RankingSnapshotId.Parse("snapshot-replacement"),
            sourceRevision: 7,
            pointerVersion: 2);
        fixture.Revisions
            .Setup(repository => repository.GetAsync(
                CanonicalRankingScopes.GlobalParks.Key,
                CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                CanonicalRankingScopes.GlobalParks.Key,
                7,
                GeneratedAtUtc));
        fixture.Snapshots
            .SetupSequence(repository => repository.GetPointerAsync(
                CanonicalRankingScopes.GlobalParks.Key,
                CancellationToken.None))
            .ReturnsAsync(snapshot.Pointer)
            .ReturnsAsync(replacement);
        fixture.SetupSnapshotStorage(snapshot);

        RatingPublishedRankingSnapshot? result = await fixture.Provider.GetCanonicalSnapshotAsync(
            RatingTargetType.Park,
            null,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCanonicalSnapshotAsync_WhenEligibleSetIsBelowScopeMinimum_ShouldWithholdRank()
    {
        ProviderFixture fixture = new ProviderFixture();
        SnapshotFixture snapshot = fixture.CreateSnapshot(2, sourceRevision: 7);
        fixture.SetupRevisionAndPointer(snapshot);
        fixture.Snapshots
            .Setup(repository => repository.GetCurrentHeaderAsync(
                snapshot.Header.ScopeKey,
                snapshot.Header.MethodologyVersion,
                CancellationToken.None))
            .ReturnsAsync(snapshot.Header);

        RatingPublishedRankingSnapshot? result = await fixture.Provider.GetCanonicalSnapshotAsync(
            RatingTargetType.Park,
            null,
            CancellationToken.None);

        Assert.Null(result);
        fixture.Snapshots.Verify(repository => repository.GetCurrentPageAsync(
            It.IsAny<RankingScopeKey>(),
            It.IsAny<RatingMethodologyVersion>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static RatingAggregate CreateAggregate(string targetId)
    {
        return new RatingAggregate
        {
            TargetType = RatingTargetType.Park,
            TargetId = targetId,
            ParkId = targetId,
            RatingCount = 12,
            UniqueContributorCount = 12,
            RatingSum = 54,
            AverageRating = 4.5,
            BayesianScore = 4.25,
            MutationVersion = 1,
            CalculatedVersion = 1,
            SourceIntegrityIsValid = true,
        };
    }

    private sealed class ProviderFixture
    {
        private readonly RankingSnapshotChecksumCalculator checksumCalculator =
            new RankingSnapshotChecksumCalculator();

        public ProviderFixture(bool eligibilityEnabled = true)
        {
            this.Ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
            this.Snapshots = new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
            this.Revisions = new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
            RankingScopeRegistry registry = new RankingScopeRegistry(
                CanonicalRankingScopes.Version,
                CanonicalRankingScopes.All);
            this.Provider = new RatingRankProvider(
                this.Ratings.Object,
                new PassthroughRankSnapshotCache(),
                this.Snapshots.Object,
                this.Revisions.Object,
                registry,
                new ConfigurableFeatureFlags(eligibilityEnabled),
                this.checksumCalculator,
                new RankingSnapshotIntegrityValidator(this.checksumCalculator));
        }

        public Mock<IRatingRepository> Ratings { get; }

        public Mock<IRankingSnapshotRepository> Snapshots { get; }

        public Mock<IRatingRankingSourceRevisionRepository> Revisions { get; }

        public RatingRankProvider Provider { get; }

        public SnapshotFixture CreateSnapshot(
            int entryCount,
            long sourceRevision,
            bool awaitingStatusReconciliation = false)
        {
            RankingSnapshotId snapshotId = RankingSnapshotId.Parse("snapshot-current");
            List<RankingSnapshotEntry> entries = Enumerable.Range(1, entryCount)
                .Select(position => new RankingSnapshotEntry(
                    position,
                    position,
                    RatingTargetType.Park,
                    $"park-{position}",
                    null,
                    5d - (position * 0.1d),
                    CreateParkEvidence()))
                .ToList();
            RankingSnapshotChunk chunk = new RankingSnapshotChunk(
                snapshotId,
                0,
                entries,
                this.checksumCalculator.CalculateChunk(entries));
            RankingSnapshotChecksum checksum = this.checksumCalculator.CalculateSnapshot(
                entryCount,
                entryCount,
                500,
                new[] { chunk });
            RankingSnapshotHeader header = new RankingSnapshotHeader(
                snapshotId,
                CanonicalRankingScopes.GlobalParks.Key,
                RankingEligibilityPolicy.InitialMethodologyVersion,
                sourceRevision,
                awaitingStatusReconciliation
                    ? RankingSnapshotStatus.Validated
                    : RankingSnapshotStatus.Current,
                entryCount,
                entryCount,
                500,
                1,
                checksum,
                GeneratedAtUtc,
                GeneratedAtUtc.AddMinutes(2),
                awaitingStatusReconciliation ? null : PublishedAtUtc);
            return new SnapshotFixture(
                header,
                chunk,
                this.CreatePointer(snapshotId, sourceRevision, pointerVersion: 1));
        }

        public RankingPublicationPointer CreatePointer(
            RankingSnapshotId snapshotId,
            long sourceRevision,
            long pointerVersion)
        {
            return new RankingPublicationPointer(
                CanonicalRankingScopes.GlobalParks.Key,
                snapshotId,
                PublishedAtUtc,
                null,
                null,
                RankingEligibilityPolicy.InitialMethodologyVersion,
                sourceRevision,
                sourceRevision,
                pointerVersion,
                PublishedAtUtc);
        }

        public void SetupCurrent(SnapshotFixture snapshot)
        {
            this.SetupRevisionAndPointer(snapshot);
            this.SetupSnapshotStorage(snapshot);
        }

        public void SetupRevisionAndPointer(SnapshotFixture snapshot)
        {
            this.Revisions
                .Setup(repository => repository.GetAsync(
                    CanonicalRankingScopes.GlobalParks.Key,
                    CancellationToken.None))
                .ReturnsAsync(new RatingRankingSourceRevision(
                    CanonicalRankingScopes.GlobalParks.Key,
                    snapshot.Header.SourceRevision,
                    GeneratedAtUtc));
            this.Snapshots
                .Setup(repository => repository.GetPointerAsync(
                    CanonicalRankingScopes.GlobalParks.Key,
                    CancellationToken.None))
                .ReturnsAsync(snapshot.Pointer);
        }

        public void SetupSnapshotStorage(SnapshotFixture snapshot)
        {
            this.Snapshots
                .Setup(repository => repository.GetCurrentHeaderAsync(
                    snapshot.Header.ScopeKey,
                    snapshot.Header.MethodologyVersion,
                    CancellationToken.None))
                .ReturnsAsync(snapshot.Header);
            this.Snapshots
                .Setup(repository => repository.GetCurrentPageAsync(
                    snapshot.Header.ScopeKey,
                    snapshot.Header.MethodologyVersion,
                    0,
                    500,
                    CancellationToken.None))
                .ReturnsAsync(new RankingSnapshotPage(
                    snapshot.Header,
                    snapshot.Chunk.Entries,
                    0,
                    500));
        }

        private static RankingEvidence CreateParkEvidence()
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

    }

    private sealed class ConfigurableFeatureFlags : IRatingRankingFeatureFlags
    {
        public ConfigurableFeatureFlags(bool eligibilityEnabled)
        {
            this.EligibilityEnabled = eligibilityEnabled;
        }

        public bool EligibilityEnabled { get; }
    }

    private sealed class PassthroughRankSnapshotCache : IRatingRankSnapshotCache
    {
        public Task<IReadOnlyDictionary<string, int>> GetOrCreateAsync(
            RatingTargetType targetType,
            ParkItemCategory? parkItemCategory,
            Func<CancellationToken, Task<IReadOnlyDictionary<string, int>>> factory,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Legacy rank computation must not be used.");
        }

        public Task<RatingPublishedRankingSnapshot?> GetOrCreatePublishedAsync(
            RankingScopeKey scopeKey,
            RankingSnapshotId snapshotId,
            RatingMethodologyVersion methodologyVersion,
            long sourceRevision,
            long pointerVersion,
            Func<CancellationToken, Task<RatingPublishedRankingSnapshot?>> factory,
            CancellationToken cancellationToken)
        {
            return factory(cancellationToken);
        }

        public void Invalidate()
        {
        }
    }

    private sealed record SnapshotFixture(
        RankingSnapshotHeader Header,
        RankingSnapshotChunk Chunk,
        RankingPublicationPointer Pointer);
}
