namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankingRecoveryCoordinator
{
    Task<bool> ReconcileRecoveredRatingMutationsAsync(CancellationToken cancellationToken);
}
