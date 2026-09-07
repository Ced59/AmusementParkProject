using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IVisitRecapSharePreviewBuilder : ISharePublicationPreviewBuilder
{
    Task<ApplicationResult<VisitRecapShareCandidatesResult>> GetCandidatesAsync(
        string ownerUserId,
        string sourceId,
        bool includeMissedItems,
        IReadOnlyCollection<string>? preferredParkItemIds,
        CancellationToken cancellationToken);

    Task<ApplicationResult<SharePublicationPreviewResult>> BuildAsync(
        string ownerUserId,
        string sourceId,
        ShareContentPolicy contentPolicy,
        VisitRecapShareInput? input,
        CancellationToken cancellationToken);
}
