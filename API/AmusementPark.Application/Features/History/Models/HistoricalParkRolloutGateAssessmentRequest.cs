using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Models;

public sealed record HistoricalParkRolloutGateAssessmentRequest(
    string ParkId,
    IReadOnlyCollection<HistoricalSubject> Subjects,
    IReadOnlyCollection<HistoricalFact> Facts);
