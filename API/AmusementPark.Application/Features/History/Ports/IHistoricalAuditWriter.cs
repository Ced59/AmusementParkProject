using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Ports;

public interface IHistoricalAuditWriter
{
    Task<HistoricalRevisionWriteDisposition> AppendAsync(
        HistoricalReviewEvent reviewEvent,
        CancellationToken cancellationToken);
}
