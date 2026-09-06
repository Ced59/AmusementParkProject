using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface ISharePublicationAccessResolver
{
    Task<ApplicationResult<ResolvedSharePublicationResult>> ResolveAsync(
        string shareId,
        SharePublicationType expectedPublicationType,
        CancellationToken cancellationToken);
}
