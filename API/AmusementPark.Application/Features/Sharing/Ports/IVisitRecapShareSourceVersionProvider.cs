using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IVisitRecapShareSourceVersionProvider
{
    Task<ApplicationResult<long>> GetOwnedSourceVersionAsync(
        string ownerUserId,
        string visitId,
        CancellationToken cancellationToken);
}
