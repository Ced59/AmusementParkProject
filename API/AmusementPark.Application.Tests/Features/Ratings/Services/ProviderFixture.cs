using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;
using Moq;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

internal sealed class ProviderFixture
{
    private readonly RankingSnapshotChecksumCalculator checksumCalculator =
        new RankingSnapshotChecksumCalculator();

    public ProviderFixture()
    {
        this.Snapshots = new Mock<IRankingSnapshotRepository>(MockBehavior.Strict);
        this.Revisions = new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        RankingScopeRegistry registry = new RankingScopeRegistry(
            CanonicalRankingScopes.Version,
            CanonicalRankingScopes.All);
        this.Provider = new RatingRankProvider(
            new PassthroughRankSnapshotCache(),
            this.Snapshots.Object,
            this.Revisions.Object,
            registry,
            this.checksumCalculator,
            new RankingSnapshotIntegrityValidator(this.checksumCalculator));
    }

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
            RatingRankProviderTests.GeneratedAtUtc,
            RatingRankProviderTests.GeneratedAtUtc.AddMinutes(2),
            awaitingStatusReconciliation ? null : RatingRankProviderTests.PublishedAtUtc);
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
            RatingRankProviderTests.PublishedAtUtc,
            null,
            null,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            sourceRevision,
            sourceRevision,
            pointerVersion,
            RatingRankProviderTests.PublishedAtUtc);
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
                RatingRankProviderTests.GeneratedAtUtc));
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
