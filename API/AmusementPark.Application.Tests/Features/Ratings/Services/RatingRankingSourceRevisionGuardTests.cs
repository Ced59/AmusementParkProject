using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RatingRankingSourceRevisionGuardTests
{
    private static readonly DateTime NowUtc = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task PrepareMutationAsync_WhenParkItemCategoryChanged_ShouldInvalidateBothCategoriesAndParkScope()
    {
        List<RankingScopeKey> incrementedScopes = new List<RankingScopeKey>();
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.IncrementAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Callback((RankingScopeKey scopeKey, CancellationToken _) => incrementedScopes.Add(scopeKey))
            .ReturnsAsync((RankingScopeKey scopeKey, CancellationToken _) =>
                new RatingRankingSourceRevision(scopeKey, 12, NowUtc));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);

        RatingRankingMutationPreparation preparation = await guard.PrepareMutationAsync(
            RatingTargetType.ParkItem,
            ParkItemCategory.Attraction,
            ParkItemCategory.Show,
            CancellationToken.None);

        Assert.Equal(
            new[] { "park-items:category:attraction", "park-items:category:show", "parks:global" },
            incrementedScopes.Select(static scopeKey => scopeKey.Value));
        Assert.Equal(3, preparation.SourceRevisions.Count);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareMutationAsync_WhenARevisionCannotBePersisted_ShouldAbortThePreparation()
    {
        RankingScopeKey categoryScopeKey = RankingScopeKey.Parse("park-items:category:attraction");
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.IncrementAsync(categoryScopeKey, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Mongo unavailable"));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => guard.PrepareMutationAsync(
            RatingTargetType.ParkItem,
            ParkItemCategory.Attraction,
            null,
            CancellationToken.None));

        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareMutationAsync_WhenParkChanges_ShouldOnlyInvalidateTheGlobalParkScope()
    {
        RankingScopeKey globalScopeKey = RankingScopeKey.Parse("parks:global");
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.IncrementAsync(globalScopeKey, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(globalScopeKey, 3, NowUtc));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);

        RatingRankingMutationPreparation preparation = await guard.PrepareMutationAsync(
            RatingTargetType.Park,
            null,
            null,
            CancellationToken.None);

        Assert.Single(preparation.SourceRevisions);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task ScheduleRebuildsAsync_WhenOneScheduleFails_ShouldContinueWithRemainingRevisions()
    {
        RatingRankingSourceRevision first = new RatingRankingSourceRevision(
            RankingScopeKey.Parse("park-items:category:attraction"),
            4,
            NowUtc);
        RatingRankingSourceRevision second = new RatingRankingSourceRevision(
            RankingScopeKey.Parse("parks:global"),
            7,
            NowUtc);
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        scheduler
            .Setup(value => value.ScheduleAsync(first, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Queue unavailable"));
        scheduler
            .Setup(value => value.ScheduleAsync(second, CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object, scheduler.Object);

        await guard.ScheduleRebuildsAsync(
            new RatingRankingMutationPreparation(new[] { first, second }),
            CancellationToken.None);

        scheduler.VerifyAll();
        revisions.VerifyNoOtherCalls();
    }

    private static RatingRankingSourceRevisionGuard CreateGuard(
        IRatingRankingSourceRevisionRepository revisions,
        IRatingRankingRebuildScheduler? scheduler = null)
    {
        RankingScopeRegistry registry = new RankingScopeRegistry(
            CanonicalRankingScopes.Version,
            CanonicalRankingScopes.All);
        IRatingRankingRebuildScheduler resolvedScheduler = scheduler
            ?? new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict).Object;
        return new RatingRankingSourceRevisionGuard(
            registry,
            revisions,
            resolvedScheduler,
            NullLogger<RatingRankingSourceRevisionGuard>.Instance);
    }
}
