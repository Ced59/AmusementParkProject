using AmusementPark.Application.Features.FactualEvents.Models;

namespace AmusementPark.Application.Features.FactualEvents.Ports;

public interface IFactualChangeOutboxRepository
{
    Task<FactualChangeOutboxWriteResult> RecordAsync(
        FactualChangeOutboxEntry entry,
        CancellationToken cancellationToken);

    Task<FactualChangeOutboxEntry?> GetAsync(
        string entryId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FactualChangeOutboxEntry>> ListPendingAsync(
        FactualChangeOutboxCursor? after,
        int maximumCount,
        CancellationToken cancellationToken);

    Task<bool> MarkMaterializedAsync(
        string entryId,
        string eventId,
        long expectedVersion,
        DateTime materializedAtUtc,
        CancellationToken cancellationToken);

    Task<bool> MarkTerminalAsync(
        string entryId,
        string eventId,
        long expectedVersion,
        DateTime terminalAtUtc,
        string errorCode,
        CancellationToken cancellationToken);
}
