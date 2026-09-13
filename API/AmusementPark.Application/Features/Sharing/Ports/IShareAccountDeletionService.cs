using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;

namespace AmusementPark.Application.Features.Sharing.Ports;

/// <summary>
/// Participant du sous-système de partage au workflow global de suppression d'un compte.
/// Le compte doit déjà refuser toute nouvelle mutation avant l'appel.
/// </summary>
public interface IShareAccountDeletionService
{
    Task<ApplicationResult<ShareAccountDeletionResult>> DeleteAsync(
        string ownerUserId,
        CancellationToken cancellationToken);
}
