using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IVisitRecapSharePreviewBuilder : ISharePublicationPreviewBuilder
{
    Task<ApplicationResult<SharePublicationPreviewResult>> BuildAsync(
        string ownerUserId,
        string sourceId,
        ShareContentPolicy contentPolicy,
        VisitRecapShareInput? input,
        CancellationToken cancellationToken);
}
