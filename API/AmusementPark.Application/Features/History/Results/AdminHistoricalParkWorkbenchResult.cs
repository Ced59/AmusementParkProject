using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Results;

public sealed record AdminHistoricalParkWorkbenchResult(
    string ParkId,
    string ParkName,
    IReadOnlyCollection<HistoricalSubject> Subjects,
    IReadOnlyCollection<HistoricalFact> Facts,
    IReadOnlyCollection<HistoricalRelation> Relations,
    IReadOnlyCollection<HistoricalSourceReference> Sources,
    HistoricalParkDiagnostics Diagnostics,
    HistoricalVisitDiagnosticCounts VisitCounts,
    HistoricalParkRolloutGate RolloutGate);
