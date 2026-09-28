using AmusementPark.Application.Features.History.Models;

namespace AmusementPark.Application.Features.History.Ports;

public interface IHistoricalVisitDiagnosticsReader
{
    Task<HistoricalVisitDiagnosticCounts> GetCountsAsync(
        string parkId,
        CancellationToken cancellationToken);
}
