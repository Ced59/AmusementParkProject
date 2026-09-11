using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IPassportProfileSourceReader
{
    Task<PassportProfileSourceData> ReadOwnedCompletedPassportAsync(
        string ownerUserId,
        CancellationToken cancellationToken);
}
