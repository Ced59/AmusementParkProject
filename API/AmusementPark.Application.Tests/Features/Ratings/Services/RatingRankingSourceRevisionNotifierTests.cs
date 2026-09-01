using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RatingRankingSourceRevisionNotifierTests
{
    private static readonly DateTime NowUtc = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task NotifyMutationAsync_WhenParkItemChanges_ShouldInvalidateCategoryAndComposedParkScopes()
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
        RatingRankingSourceRevisionNotifier notifier = CreateNotifier(revisions.Object);

        await notifier.NotifyMutationAsync(
            RatingTargetType.ParkItem,
            ParkItemCategory.Attraction,
            CancellationToken.None);

        Assert.Equal(
            new[] { "park-items:category:attraction", "parks:global" },
            incrementedScopes.Select(static scopeKey => scopeKey.Value));
        revisions.VerifyAll();
    }

    [Fact]
    public async Task NotifyMutationAsync_WhenCategoryInvalidationFails_ShouldStillInvalidateTheGlobalParkScope()
    {
        RankingScopeKey categoryScopeKey = RankingScopeKey.Parse("park-items:category:attraction");
        RankingScopeKey globalScopeKey = RankingScopeKey.Parse("parks:global");
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.IncrementAsync(categoryScopeKey, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Mongo unavailable"));
        revisions
            .Setup(repository => repository.IncrementAsync(globalScopeKey, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(globalScopeKey, 7, NowUtc));
        RatingRankingSourceRevisionNotifier notifier = CreateNotifier(revisions.Object);

        await notifier.NotifyMutationAsync(
            RatingTargetType.ParkItem,
            ParkItemCategory.Attraction,
            CancellationToken.None);

        revisions.VerifyAll();
    }

    [Fact]
    public async Task NotifyMutationAsync_WhenParkChanges_ShouldOnlyInvalidateTheGlobalParkScope()
    {
        RankingScopeKey globalScopeKey = RankingScopeKey.Parse("parks:global");
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.IncrementAsync(globalScopeKey, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(globalScopeKey, 3, NowUtc));
        RatingRankingSourceRevisionNotifier notifier = CreateNotifier(revisions.Object);

        await notifier.NotifyMutationAsync(RatingTargetType.Park, null, CancellationToken.None);

        revisions.VerifyAll();
    }

    private static RatingRankingSourceRevisionNotifier CreateNotifier(
        IRatingRankingSourceRevisionRepository revisions)
    {
        RankingScopeRegistry registry = new RankingScopeRegistry(
            CanonicalRankingScopes.Version,
            CanonicalRankingScopes.All);
        return new RatingRankingSourceRevisionNotifier(
            registry,
            revisions,
            NullLogger<RatingRankingSourceRevisionNotifier>.Instance);
    }
}
