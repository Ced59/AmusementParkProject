using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RatingRankProviderTests
{
    internal static readonly DateTime GeneratedAtUtc =
        new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
    internal static readonly DateTime PublishedAtUtc =
        new DateTime(2026, 9, 1, 8, 5, 0, DateTimeKind.Utc);

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
}
