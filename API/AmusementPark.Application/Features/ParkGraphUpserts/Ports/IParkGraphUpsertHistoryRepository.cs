using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Ports;

public interface IParkGraphUpsertHistoryRepository
{
    Task SaveAsync(ParkGraphUpsertHistoryEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ParkGraphUpsertHistoryEntry>> ListRecentAsync(ParkGraphUpsertHistoryQuery query, CancellationToken cancellationToken);
}
