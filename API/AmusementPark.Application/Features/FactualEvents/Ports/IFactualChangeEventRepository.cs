using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Ports;

public interface IFactualChangeEventRepository
{
    Task<FactualChangeEventWriteDisposition> CreateAsync(
        FactualChangeEvent factualEvent,
        CancellationToken cancellationToken);

    Task<FactualChangeEvent?> GetByLogicalRevisionAsync(
        string deduplicationKey,
        long sourceRevision,
        CancellationToken cancellationToken);

    Task<FactualChangeEvent?> GetAsync(
        FactualChangeEventId eventId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FactualChangeEvent>> GetManyAsync(
        IReadOnlyCollection<FactualChangeEventId> eventIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FactualChangeEvent>> ListPublishedAsync(
        PublishedFactualEventCursor? after,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FactualChangeEvent>> ListTerminalAsync(
        TerminalFactualEventCursor? after,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FactualChangeEvent>> GetCorrectedBySuccessorIdsAsync(
        IReadOnlyCollection<FactualChangeEventId> successorEventIds,
        CancellationToken cancellationToken);

    Task<PagedResult<FactualChangeEvent>> SearchAsync(
        FactualChangeEventSearchCriteria criteria,
        CancellationToken cancellationToken);

    Task<FactualChangeEventMutationOutcome> ReplaceAsync(
        FactualChangeEvent factualEvent,
        long expectedVersion,
        CancellationToken cancellationToken);
}
