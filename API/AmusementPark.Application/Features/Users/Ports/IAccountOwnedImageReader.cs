using AmusementPark.Application.Features.Users.Models;

namespace AmusementPark.Application.Features.Users.Ports;

public interface IAccountOwnedImageReader
{
    Task<IReadOnlyCollection<AccountOwnedImage>> ListAsync(
        string userId,
        CancellationToken cancellationToken);
}
