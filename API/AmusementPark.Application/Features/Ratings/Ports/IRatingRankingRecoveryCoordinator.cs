namespace AmusementPark.Application.Features.Ratings.Ports;

public interface IRatingRankingRecoveryCoordinator
{
    Task ReconcileRecoveredParkItemMutationsAsync(CancellationToken cancellationToken);
}
