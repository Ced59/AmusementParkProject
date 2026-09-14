namespace AmusementPark.Application.Features.Sharing.Ports;

/// <summary>
/// Purge physique des données de partage après la coupure métier de tous les accès publics.
/// </summary>
public interface IShareAccountDeletionStore
{
    Task<long> PurgeAsync(
        string ownerUserId,
        CancellationToken cancellationToken);
}
