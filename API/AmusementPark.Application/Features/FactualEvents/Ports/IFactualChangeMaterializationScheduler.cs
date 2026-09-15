using AmusementPark.Application.Features.FactualEvents.Models;

namespace AmusementPark.Application.Features.FactualEvents.Ports;

public interface IFactualChangeMaterializationScheduler
{
    Task ScheduleAsync(
        FactualChangeOutboxEntry entry,
        CancellationToken cancellationToken);

    Task<FactualChangeOutboxCursor?> ReconcilePendingAsync(
        FactualChangeOutboxCursor? after,
        CancellationToken cancellationToken);
}
