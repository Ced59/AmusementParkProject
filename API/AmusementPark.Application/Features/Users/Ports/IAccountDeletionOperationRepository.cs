using AmusementPark.Application.Features.Users.Models;

namespace AmusementPark.Application.Features.Users.Ports;

public interface IAccountDeletionOperationRepository
{
    Task<AccountDeletionOperation> CreateOrGetAsync(
        string userId,
        DateTime createdAtUtc,
        CancellationToken cancellationToken);

    Task<AccountDeletionOperation?> GetAsync(
        string operationId,
        CancellationToken cancellationToken);

    Task DeleteAsync(string operationId, CancellationToken cancellationToken);
}
