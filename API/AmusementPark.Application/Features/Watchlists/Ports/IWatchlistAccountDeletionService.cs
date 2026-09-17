using AmusementPark.Application.Features.Watchlists.Results;

namespace AmusementPark.Application.Features.Watchlists.Ports;

/// <summary>
/// Participant du sous-système de suivi au workflow global de suppression d'un compte.
/// Le compte doit déjà refuser toute nouvelle mutation avant l'appel.
/// </summary>
public interface IWatchlistAccountDeletionService
{
    Task<WatchlistAccountDeletionResult> DeleteAsync(
        string userId,
        CancellationToken cancellationToken);
}
