using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Ports;

public interface IParkFitOperationalStatusRepository
{
    Task<ParkFitOperationalStatus?> GetAsync(
        string parkId,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, ParkFitOperationalStatus>> GetByParkIdsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken);

    Task<ParkFitOperationalStatusWriteOutcome> ReplaceAsync(
        ParkFitOperationalStatus status,
        long expectedRevision,
        CancellationToken cancellationToken);
}
