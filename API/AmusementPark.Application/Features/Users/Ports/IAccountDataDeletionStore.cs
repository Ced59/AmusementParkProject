using AmusementPark.Application.Features.Users.Models;

namespace AmusementPark.Application.Features.Users.Ports;

public interface IAccountDataDeletionStore
{
    Task<AccountDataDeletionResult> PurgeAsync(
        string userId,
        CancellationToken cancellationToken);
}
