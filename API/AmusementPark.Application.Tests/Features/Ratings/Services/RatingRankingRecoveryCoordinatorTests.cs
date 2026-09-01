using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RatingRankingRecoveryCoordinatorTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReconcileRecoveredRatingMutationsAsync_ShouldRepairAggregateAndInvalidateCurrentCategoryBeforeAcknowledgingEvent()
    {
        RankingScopeKey globalScopeKey = CanonicalRankingScopes.GlobalParks.Key;
        RankingScopeDefinition hotelScope = CanonicalRankingScopes.PublicItemCategories.Single(
            static definition => definition.Filter.ParkItemCategory == ParkItemCategory.Hotel);
        RatingRankingRecoveredMutation recoveredMutation = CreateRecoveredMutation(
            1,
            RatingTargetType.ParkItem,
            "item-1");
        RatingRankingMutationLease hotelLease = RatingRankingMutationLease.Create(hotelScope.Key);
        RatingRankingSourceRevision hotelRevision = new RatingRankingSourceRevision(
            hotelScope.Key,
            13,
            NowUtc);
        List<string> operations = new List<string>();
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings
            .Setup(repository => repository.RepairAggregateAsync(
                RatingTargetType.ParkItem,
                "item-1",
                CancellationToken.None))
            .Callback(() => operations.Add("repair-aggregate"))
            .Returns(Task.CompletedTask);
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        rankProvider
            .Setup(provider => provider.Invalidate())
            .Callback(() => operations.Add("invalidate-cache"));
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItems
            .Setup(repository => repository.GetByIdAsync("item-1", false, CancellationToken.None))
            .Callback(() => operations.Add("read-current-category"))
            .ReturnsAsync(new ParkItem
            {
                Id = "item-1",
                Category = ParkItemCategory.Hotel,
            });
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.GetAsync(globalScopeKey, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                globalScopeKey,
                12,
                NowUtc,
                RecoveredMutations: new[] { recoveredMutation }));
        revisions
            .Setup(repository => repository.BeginMutationAsync(hotelScope.Key, CancellationToken.None))
            .Callback(() => operations.Add("begin-category-fence"))
            .ReturnsAsync(hotelLease);
        revisions
            .Setup(repository => repository.CompleteMutationAsync(
                hotelLease,
                true,
                CancellationToken.None))
            .Callback(() => operations.Add("complete-category-fence"))
            .ReturnsAsync(hotelRevision);
        revisions
            .Setup(repository => repository.AcknowledgeRecoveredMutationAsync(
                globalScopeKey,
                recoveredMutation,
                CancellationToken.None))
            .Callback(() => operations.Add("acknowledge-event"))
            .ReturnsAsync(true);
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        scheduler
            .Setup(value => value.ScheduleIfOutstandingAsync(hotelRevision, CancellationToken.None))
            .Callback(() => operations.Add("schedule-category"))
            .Returns(Task.CompletedTask);
        RatingRankingRecoveryCoordinator coordinator = CreateCoordinator(
            ratings.Object,
            rankProvider.Object,
            parkItems.Object,
            revisions.Object,
            scheduler.Object);

        bool result = await coordinator.ReconcileRecoveredRatingMutationsAsync(CancellationToken.None);

        Assert.True(result);
        Assert.Equal(
            new[]
            {
                "repair-aggregate",
                "invalidate-cache",
                "read-current-category",
                "begin-category-fence",
                "complete-category-fence",
                "schedule-category",
                "acknowledge-event",
            },
            operations);
        ratings.VerifyAll();
        rankProvider.VerifyAll();
        parkItems.VerifyAll();
        revisions.VerifyAll();
        scheduler.VerifyAll();
    }

    [Fact]
    public async Task ReconcileRecoveredRatingMutationsAsync_WhenAggregateRepairFails_ShouldKeepEventAndBlockPublication()
    {
        RankingScopeKey globalScopeKey = CanonicalRankingScopes.GlobalParks.Key;
        RatingRankingRecoveredMutation recoveredMutation = CreateRecoveredMutation(
            2,
            RatingTargetType.Park,
            "park-1");
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings
            .Setup(repository => repository.RepairAggregateAsync(
                RatingTargetType.Park,
                "park-1",
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Mongo unavailable"));
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.GetAsync(globalScopeKey, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                globalScopeKey,
                12,
                NowUtc,
                RecoveredMutations: new[] { recoveredMutation }));
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        RatingRankingRecoveryCoordinator coordinator = CreateCoordinator(
            ratings.Object,
            rankProvider.Object,
            parkItems.Object,
            revisions.Object,
            scheduler.Object);

        bool result = await coordinator.ReconcileRecoveredRatingMutationsAsync(CancellationToken.None);

        Assert.False(result);
        ratings.VerifyAll();
        rankProvider.VerifyNoOtherCalls();
        parkItems.VerifyNoOtherCalls();
        revisions.VerifyAll();
        scheduler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconcileRecoveredRatingMutationsAsync_WhenOneOfSeveralRepairsFails_ShouldNotFinalizeAnyEvent()
    {
        RankingScopeKey globalScopeKey = CanonicalRankingScopes.GlobalParks.Key;
        RatingRankingRecoveredMutation failedMutation = CreateRecoveredMutation(
            4,
            RatingTargetType.Park,
            "park-1");
        RatingRankingRecoveredMutation repairedMutation = CreateRecoveredMutation(
            5,
            RatingTargetType.ParkItem,
            "item-1");
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings
            .Setup(repository => repository.RepairAggregateAsync(
                RatingTargetType.Park,
                "park-1",
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Mongo unavailable"));
        ratings
            .Setup(repository => repository.RepairAggregateAsync(
                RatingTargetType.ParkItem,
                "item-1",
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        rankProvider.Setup(provider => provider.Invalidate());
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.GetAsync(globalScopeKey, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                globalScopeKey,
                12,
                NowUtc,
                RecoveredMutations: new[] { failedMutation, repairedMutation }));
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        RatingRankingRecoveryCoordinator coordinator = CreateCoordinator(
            ratings.Object,
            rankProvider.Object,
            parkItems.Object,
            revisions.Object,
            scheduler.Object);

        bool result = await coordinator.ReconcileRecoveredRatingMutationsAsync(CancellationToken.None);

        Assert.False(result);
        ratings.VerifyAll();
        rankProvider.Verify(provider => provider.Invalidate(), Times.Once);
        parkItems.VerifyNoOtherCalls();
        revisions.VerifyAll();
        scheduler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconcileRecoveredRatingMutationsAsync_WhenParkItemNoLongerExists_ShouldRepairAndAcknowledgeExactEvent()
    {
        RankingScopeKey globalScopeKey = CanonicalRankingScopes.GlobalParks.Key;
        RatingRankingRecoveredMutation recoveredMutation = CreateRecoveredMutation(
            3,
            RatingTargetType.ParkItem,
            "deleted-item");
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings
            .Setup(repository => repository.RepairAggregateAsync(
                RatingTargetType.ParkItem,
                "deleted-item",
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        Mock<IRatingRankProvider> rankProvider = new Mock<IRatingRankProvider>(MockBehavior.Strict);
        rankProvider.Setup(provider => provider.Invalidate());
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItems
            .Setup(repository => repository.GetByIdAsync("deleted-item", false, CancellationToken.None))
            .ReturnsAsync((ParkItem?)null);
        Mock<IRatingRankingSourceRevisionRepository> revisions =
            new Mock<IRatingRankingSourceRevisionRepository>(MockBehavior.Strict);
        revisions
            .Setup(repository => repository.GetAsync(globalScopeKey, CancellationToken.None))
            .ReturnsAsync(new RatingRankingSourceRevision(
                globalScopeKey,
                12,
                NowUtc,
                RecoveredMutations: new[] { recoveredMutation }));
        revisions
            .Setup(repository => repository.AcknowledgeRecoveredMutationAsync(
                globalScopeKey,
                recoveredMutation,
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        RatingRankingRecoveryCoordinator coordinator = CreateCoordinator(
            ratings.Object,
            rankProvider.Object,
            parkItems.Object,
            revisions.Object,
            scheduler.Object);

        bool result = await coordinator.ReconcileRecoveredRatingMutationsAsync(CancellationToken.None);

        Assert.True(result);
        ratings.VerifyAll();
        rankProvider.VerifyAll();
        parkItems.VerifyAll();
        revisions.VerifyAll();
        scheduler.VerifyNoOtherCalls();
    }

    private static RatingRankingRecoveryCoordinator CreateCoordinator(
        IRatingRepository ratings,
        IRatingRankProvider rankProvider,
        IParkItemRepository parkItems,
        IRatingRankingSourceRevisionRepository revisions,
        IRatingRankingRebuildScheduler scheduler)
    {
        return new RatingRankingRecoveryCoordinator(
            ratings,
            rankProvider,
            parkItems,
            revisions,
            scheduler,
            NullLogger<RatingRankingRecoveryCoordinator>.Instance);
    }

    private static RatingRankingRecoveredMutation CreateRecoveredMutation(
        int tokenSeed,
        RatingTargetType targetType,
        string targetId)
    {
        return new RatingRankingRecoveredMutation(
            tokenSeed.ToString("x32"),
            targetType,
            targetId);
    }
}
