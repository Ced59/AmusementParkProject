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
    public async Task ReconcileRecoveredParkItemMutationsAsync_ShouldInvalidateCurrentCategoryBeforeAcknowledgingTarget()
    {
        RankingScopeKey globalScopeKey = CanonicalRankingScopes.GlobalParks.Key;
        RankingScopeDefinition hotelScope = CanonicalRankingScopes.PublicItemCategories.Single(
            static definition => definition.Filter.ParkItemCategory == ParkItemCategory.Hotel);
        RatingRankingMutationLease hotelLease = RatingRankingMutationLease.Create(hotelScope.Key);
        RatingRankingSourceRevision hotelRevision = new RatingRankingSourceRevision(
            hotelScope.Key,
            13,
            NowUtc);
        List<string> operations = new List<string>();
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
                RecoveredParkItemTargetIds: new[] { "item-1" }));
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
            .Setup(repository => repository.AcknowledgeRecoveredParkItemTargetAsync(
                globalScopeKey,
                "item-1",
                CancellationToken.None))
            .Callback(() => operations.Add("acknowledge-target"))
            .ReturnsAsync(true);
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        scheduler
            .Setup(value => value.ScheduleIfOutstandingAsync(hotelRevision, CancellationToken.None))
            .Callback(() => operations.Add("schedule-category"))
            .Returns(Task.CompletedTask);
        RatingRankingRecoveryCoordinator coordinator = new RatingRankingRecoveryCoordinator(
            parkItems.Object,
            revisions.Object,
            scheduler.Object,
            NullLogger<RatingRankingRecoveryCoordinator>.Instance);

        await coordinator.ReconcileRecoveredParkItemMutationsAsync(CancellationToken.None);

        Assert.Equal(
            new[]
            {
                "read-current-category",
                "begin-category-fence",
                "complete-category-fence",
                "schedule-category",
                "acknowledge-target",
            },
            operations);
        parkItems.VerifyAll();
        revisions.VerifyAll();
        scheduler.VerifyAll();
    }

    [Fact]
    public async Task ReconcileRecoveredParkItemMutationsAsync_WhenTargetNoLongerExists_ShouldAcknowledgeIt()
    {
        RankingScopeKey globalScopeKey = CanonicalRankingScopes.GlobalParks.Key;
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
                RecoveredParkItemTargetIds: new[] { "deleted-item" }));
        revisions
            .Setup(repository => repository.AcknowledgeRecoveredParkItemTargetAsync(
                globalScopeKey,
                "deleted-item",
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IRatingRankingRebuildScheduler> scheduler =
            new Mock<IRatingRankingRebuildScheduler>(MockBehavior.Strict);
        RatingRankingRecoveryCoordinator coordinator = new RatingRankingRecoveryCoordinator(
            parkItems.Object,
            revisions.Object,
            scheduler.Object,
            NullLogger<RatingRankingRecoveryCoordinator>.Instance);

        await coordinator.ReconcileRecoveredParkItemMutationsAsync(CancellationToken.None);

        parkItems.VerifyAll();
        revisions.VerifyAll();
        scheduler.VerifyNoOtherCalls();
    }
}
