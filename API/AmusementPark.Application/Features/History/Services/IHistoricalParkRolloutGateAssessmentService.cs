using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public interface IHistoricalParkRolloutGateAssessmentService
{
    Task<HistoricalParkRolloutGate> AssessAsync(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> subjects,
        IReadOnlyCollection<HistoricalFact> facts,
        CancellationToken cancellationToken);

    Task<HistoricalParkRolloutGate> AssessPublicParkAsync(
        HistoricalParkEditorialScope scope,
        IReadOnlyCollection<HistoricalFact> facts,
        CancellationToken cancellationToken);
}
