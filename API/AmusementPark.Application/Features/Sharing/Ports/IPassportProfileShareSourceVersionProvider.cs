using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IPassportProfileShareSourceVersionProvider
{
    Task<ApplicationResult<PassportProfileShareSourceRevision>> GetOwnedSourceVersionAsync(
        string ownerUserId,
        CancellationToken cancellationToken);
}
