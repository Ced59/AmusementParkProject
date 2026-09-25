using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Ports;

public interface IHistoricalAuditReader
{
    Task<IReadOnlyCollection<HistoricalReviewEvent>> ListAsync(
        HistoricalReviewResourceType resourceType,
        Guid resourceId,
        int limit,
        CancellationToken cancellationToken);
}
