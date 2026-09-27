using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Ports;

public interface IHistoricalRelationRepository
{
    Task<HistoricalRevisionWriteDisposition> AppendRevisionAsync(
        HistoricalRelation relation,
        HistoricalReviewEvent transitionReviewEvent,
        CancellationToken cancellationToken);

    Task<HistoricalRelation?> GetLatestRevisionAsync(
        Guid relationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HistoricalRelation>> GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
        IReadOnlyCollection<HistoricalSubjectKey> subjects,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HistoricalRelation>> GetLatestDecisionEligibleRevisionsTouchingEachSubjectAsync(
        IReadOnlyCollection<HistoricalSubjectKey> subjects,
        int limitPerSubject,
        CancellationToken cancellationToken);
}
