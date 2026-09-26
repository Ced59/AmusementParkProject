using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Ports;

public interface IHistoricalAuditReader
{
    Task<HistoricalAuditPage> ListAsync(
        HistoricalReviewResourceType resourceType,
        Guid resourceId,
        int pageSize,
        HistoricalAuditCursor? after,
        CancellationToken cancellationToken);
}
