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
}
