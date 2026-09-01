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
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Callback((RankingScopeKey scopeKey, CancellationToken _) => incrementedScopes.Add(scopeKey))
            .Returns((RankingScopeKey scopeKey, CancellationToken _) =>
                Task.FromResult(CreateLease(scopeKey)));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);

        RatingRankingMutationPreparation preparation = await guard.PrepareMutationAsync(
            RatingTargetType.ParkItem,
            ParkItemCategory.Attraction,
            ParkItemCategory.Show,
            CancellationToken.None);

        Assert.Equal(
            new[] { "park-items:category:attraction", "park-items:category:show", "parks:global" },
            incrementedScopes.Select(static scopeKey => scopeKey.Value));
        Assert.Equal(3, preparation.MutationLeases.Count);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareMutationAsync_WhenARevisionCannotBePersisted_ShouldAbortThePreparation()
    {
        RankingScopeKey categoryScopeKey = RankingScopeKey.Parse("park-items:category:attraction");
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(categoryScopeKey, CancellationToken.None))
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
            .Setup(repository => repository.BeginMutationAsync(globalScopeKey, CancellationToken.None))
            .ReturnsAsync(CreateLease(globalScopeKey));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);

        RatingRankingMutationPreparation preparation = await guard.PrepareMutationAsync(
            RatingTargetType.Park,
            null,
            null,
            CancellationToken.None);

        Assert.Single(preparation.MutationLeases);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task CompleteMutationAsync_WhenOneScheduleFails_ShouldContinueWithRemainingRevisions()
    {
        RatingRankingSourceRevision first = new RatingRankingSourceRevision(
            RankingScopeKey.Parse("park-items:category:attraction"),
            4,
            NowUtc);
        RatingRankingSourceRevision second = new RatingRankingSourceRevision(
            RankingScopeKey.Parse("parks:global"),
            7,
            NowUtc);
        RatingRankingMutationLease firstLease = CreateLease(first.ScopeKey, 1);
        RatingRankingMutationLease secondLease = CreateLease(second.ScopeKey, 2);
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        scheduler
            .Setup(value => value.ScheduleIfOutstandingAsync(first, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Queue unavailable"));
        scheduler
            .Setup(value => value.ScheduleIfOutstandingAsync(second, CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(value => value.CompleteMutationAsync(firstLease, true, CancellationToken.None))
            .ReturnsAsync(first);
        revisions
            .Setup(value => value.CompleteMutationAsync(secondLease, true, CancellationToken.None))
            .ReturnsAsync(second);
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object, scheduler.Object);

        await guard.CompleteMutationAsync(
            new RatingRankingMutationPreparation(new[] { firstLease, secondLease }),
            sourceChanged: true,
            CancellationToken.None);

        scheduler.VerifyAll();
        revisions.VerifyAll();
    }

    [Fact]
    public async Task CompleteMutationAsync_WhenAnotherMutationIsPending_ShouldKeepRevisionHidden()
    {
        RankingScopeKey scopeKey = RankingScopeKey.Parse("parks:global");
        RatingRankingSourceRevision blockedRevision = new RatingRankingSourceRevision(
            scopeKey,
            7,
            NowUtc,
            PendingMutationCount: 1,
            MutationLeaseExpiresAtUtc: NowUtc.AddMinutes(30));
        RatingRankingMutationLease mutationLease = CreateLease(scopeKey);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(value => value.CompleteMutationAsync(mutationLease, true, CancellationToken.None))
            .ReturnsAsync(blockedRevision);
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object, scheduler.Object);

        await guard.CompleteMutationAsync(
            new RatingRankingMutationPreparation(new[] { mutationLease }),
            sourceChanged: true,
            CancellationToken.None);

        revisions.VerifyAll();
        scheduler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PrepareParkChangesAsync_WhenVisibleParkBecomesHidden_ShouldInvalidateAllCanonicalScopes()
    {
        List<RankingScopeKey> incrementedScopes = new List<RankingScopeKey>();
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Callback((RankingScopeKey scopeKey, CancellationToken _) => incrementedScopes.Add(scopeKey))
            .Returns((RankingScopeKey scopeKey, CancellationToken _) =>
                Task.FromResult(CreateLease(scopeKey)));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);
        Park previous = new Park
        {
            Id = "park-1",
            Name = "Demo Park",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };
        Park current = new Park
        {
            Id = "park-1",
            Name = "Demo Park",
            IsVisible = false,
            Status = ParkStatus.Operating,
        };

        RatingRankingMutationPreparation preparation = await guard.PrepareParkChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        Assert.Equal(CanonicalRankingScopes.All.Count, preparation.MutationLeases.Count);
        Assert.Equal(
            CanonicalRankingScopes.All.Select(static scope => scope.Key.Value).OrderBy(static key => key),
            incrementedScopes.Select(static scope => scope.Value));
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkChangesAsync_WhenIncludedParkNameChanges_ShouldInvalidateOnlyParkScope()
    {
        List<RankingScopeKey> incrementedScopes = new List<RankingScopeKey>();
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Callback((RankingScopeKey scopeKey, CancellationToken _) => incrementedScopes.Add(scopeKey))
            .Returns((RankingScopeKey scopeKey, CancellationToken _) =>
                Task.FromResult(CreateLease(scopeKey)));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);
        Park previous = new Park
        {
            Id = "park-1",
            Name = "Alpha Park",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };
        Park current = new Park
        {
            Id = "park-1",
            Name = "Beta Park",
            IsVisible = true,
            Status = ParkStatus.Operating,
        };

        RatingRankingMutationPreparation preparation = await guard.PrepareParkChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        RankingScopeKey scopeKey = Assert.Single(preparation.MutationLeases).ScopeKey;
        Assert.Equal("parks:global", scopeKey.Value);
        Assert.Equal(new[] { "parks:global" }, incrementedScopes.Select(static scope => scope.Value));
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkItemChangesAsync_WhenCategoryChanges_ShouldInvalidateOldNewAndParkScopes()
    {
        List<RankingScopeKey> incrementedScopes = new List<RankingScopeKey>();
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Callback((RankingScopeKey scopeKey, CancellationToken _) => incrementedScopes.Add(scopeKey))
            .Returns((RankingScopeKey scopeKey, CancellationToken _) =>
                Task.FromResult(CreateLease(scopeKey)));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);
        ParkItem previous = CreateVisibleParkItem(ParkItemCategory.Attraction);
        ParkItem current = CreateVisibleParkItem(ParkItemCategory.Show);

        RatingRankingMutationPreparation preparation = await guard.PrepareParkItemChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        Assert.Equal(
            new[] { "park-items:category:attraction", "park-items:category:show", "parks:global" },
            incrementedScopes.Select(static scope => scope.Value));
        Assert.Equal(3, preparation.MutationLeases.Count);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkItemChangesAsync_WhenIncludedItemNameChanges_ShouldInvalidateOnlyItsCategoryScope()
    {
        List<RankingScopeKey> incrementedScopes = new List<RankingScopeKey>();
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Callback((RankingScopeKey scopeKey, CancellationToken _) => incrementedScopes.Add(scopeKey))
            .Returns((RankingScopeKey scopeKey, CancellationToken _) =>
                Task.FromResult(CreateLease(scopeKey)));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);
        ParkItem previous = CreateVisibleParkItem(ParkItemCategory.Attraction);
        previous.Name = "Alpha Ride";
        ParkItem current = CreateVisibleParkItem(ParkItemCategory.Attraction);
        current.Name = "Beta Ride";

        RatingRankingMutationPreparation preparation = await guard.PrepareParkItemChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        RankingScopeKey scopeKey = Assert.Single(preparation.MutationLeases).ScopeKey;
        Assert.Equal("park-items:category:attraction", scopeKey.Value);
        Assert.Equal(
            new[] { "park-items:category:attraction" },
            incrementedScopes.Select(static scope => scope.Value));
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkItemChangesAsync_WhenIncludedItemTypeChanges_ShouldInvalidateOnlyParkScope()
    {
        List<RankingScopeKey> incrementedScopes = new List<RankingScopeKey>();
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.BeginMutationAsync(
                It.IsAny<RankingScopeKey>(),
                CancellationToken.None))
            .Callback((RankingScopeKey scopeKey, CancellationToken _) => incrementedScopes.Add(scopeKey))
            .Returns((RankingScopeKey scopeKey, CancellationToken _) =>
                Task.FromResult(CreateLease(scopeKey)));
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);
        ParkItem previous = CreateVisibleParkItem(ParkItemCategory.Attraction);
        previous.Type = ParkItemType.RollerCoaster;
        ParkItem current = CreateVisibleParkItem(ParkItemCategory.Attraction);
        current.Type = ParkItemType.DarkRide;

        RatingRankingMutationPreparation preparation = await guard.PrepareParkItemChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        RankingScopeKey scopeKey = Assert.Single(preparation.MutationLeases).ScopeKey;
        Assert.Equal("parks:global", scopeKey.Value);
        Assert.Equal(new[] { "parks:global" }, incrementedScopes.Select(static scope => scope.Value));
        revisions.VerifyAll();
    }

    [Fact]
    public async Task PrepareParkItemChangesAsync_WhenHiddenItemMetadataChanges_ShouldNotAdvanceRevision()
    {
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        RatingRankingSourceRevisionGuard guard = CreateGuard(revisions.Object);
        ParkItem previous = CreateVisibleParkItem(ParkItemCategory.Attraction);
        previous.IsVisible = false;
        ParkItem current = CreateVisibleParkItem(ParkItemCategory.Show);
        current.IsVisible = false;

        RatingRankingMutationPreparation preparation = await guard.PrepareParkItemChangesAsync(
            new[] { previous },
            new[] { current },
            CancellationToken.None);

        Assert.Empty(preparation.MutationLeases);
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

    private static ParkItem CreateVisibleParkItem(ParkItemCategory category)
    {
        return new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Demo Item",
            Category = category,
            IsVisible = true,
            AttractionDetails = new AttractionDetails
            {
                Status = ParkItemStatusNormalizer.Operating,
            },
        };
    }

    private static RatingRankingMutationLease CreateLease(
        RankingScopeKey scopeKey,
        int tokenSeed = 1)
    {
        return new RatingRankingMutationLease(
            scopeKey,
            tokenSeed.ToString("x32"));
    }
}
