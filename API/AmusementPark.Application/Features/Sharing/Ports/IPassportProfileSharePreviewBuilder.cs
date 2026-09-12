using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IPassportProfileSharePreviewBuilder
{
    Task<ApplicationResult<SharePublicationPreviewResult>> BuildAsync(
        string ownerUserId,
        ShareContentPolicy contentPolicy,
        PassportProfileShareInput? input,
        CancellationToken cancellationToken);
}
