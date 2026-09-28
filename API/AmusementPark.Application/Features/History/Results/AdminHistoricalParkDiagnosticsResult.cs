using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Results;

public sealed record AdminHistoricalParkDiagnosticsResult(
    string ParkId,
    string ParkName,
    HistoricalParkDiagnostics Diagnostics,
    HistoricalVisitDiagnosticCounts VisitCounts);
