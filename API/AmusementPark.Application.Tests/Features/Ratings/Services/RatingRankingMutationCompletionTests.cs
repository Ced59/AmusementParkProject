using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

public sealed class RatingRankingMutationCompletionTests
{
    [Fact]
    public async Task CompleteAfterWriteAsync_WhenFinalMetadataResolutionFails_ShouldLeaveInitialPreparationRecoverable()
    {
        RankingScopeKey globalScopeKey = CanonicalRankingScopes.GlobalParks.Key;
        RatingRankingMutationPreparation preparation = new RatingRankingMutationPreparation(
            new[] { RatingRankingMutationLease.Create(globalScopeKey) });
        RatingRankingPreparedMutation preparedMutation = new RatingRankingPreparedMutation(
            new RatingTargetMetadataResult(
                RatingTargetType.ParkItem,
                "item-1",
                "Demo Ride",
                "park-1",
                "Demo Park",
                ParkItemCategory.Attraction,
                ParkItemType.RollerCoaster,
                true),
            preparation,
            new RatingRankingMutationRecoveryTarget(
                RatingTargetType.ParkItem,
                "item-1",
                "user-1",
                1.ToString("x32")),
            new[] { ParkItemCategory.Attraction });
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItems = new Mock<IParkItemRepository>(MockBehavior.Strict);
        parkItems
            .Setup(repository => repository.GetByIdAsync(
                "item-1",
                false,
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Mongo unavailable"));
        Mock<IRatingRankingMutationGuard> guard =
            new Mock<IRatingRankingMutationGuard>(MockBehavior.Strict);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RatingRankingMutationCompletion.CompleteAfterWriteAsync(
                RatingTargetType.ParkItem,
                "item-1",
                preparedMutation,
                true,
                parks.Object,
                parkItems.Object,
                guard.Object));

        parkItems.VerifyAll();
        parks.VerifyNoOtherCalls();
        guard.VerifyNoOtherCalls();
    }
}
