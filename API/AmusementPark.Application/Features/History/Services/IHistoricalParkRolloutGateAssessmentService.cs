using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public interface IHistoricalParkRolloutGateAssessmentService
{
    HistoricalParkRolloutGate Assess(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> subjects,
        IReadOnlyCollection<HistoricalFact> facts);

    HistoricalParkRolloutGate AssessPublicPark(
        HistoricalParkEditorialScope scope,
        IReadOnlyCollection<HistoricalFact> facts);
}
