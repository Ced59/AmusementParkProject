using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Ports;

public interface IHistoricalFactRepository
{
    Task<HistoricalRevisionWriteDisposition> AppendRevisionAsync(
        HistoricalFact fact,
        HistoricalReviewEvent transitionReviewEvent,
        CancellationToken cancellationToken);

    Task<HistoricalFact?> GetRevisionAsync(
        Guid factId,
        int revision,
        CancellationToken cancellationToken);

    Task<HistoricalFact?> GetLatestRevisionAsync(
        Guid factId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HistoricalFact>> GetLatestRevisionsForSubjectsAsync(
        IReadOnlyCollection<HistoricalSubject> subjects,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<HistoricalFact>> GetLatestDecisionEligibleRevisionsForParkAsync(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> publicCurrentSubjects,
        CancellationToken cancellationToken);

    Task<PagedResult<HistoricalFact>> GetLatestDecisionEligibleRevisionsForParkPageAsync(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> publicCurrentSubjects,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
