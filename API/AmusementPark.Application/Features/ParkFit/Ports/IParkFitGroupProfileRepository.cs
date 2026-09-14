using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Ports;

public interface IParkFitGroupProfileRepository
{
    Task<long> CountOwnedAsync(
        string ownerUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ParkFitGroupProfile>> ListOwnedAsync(
        string ownerUserId,
        CancellationToken cancellationToken);

    Task<ParkFitGroupProfile?> GetOwnedAsync(
        ParkFitGroupProfileId profileId,
        string ownerUserId,
        CancellationToken cancellationToken);

    Task<ParkFitGroupProfileWriteOutcome> CreateAsync(
        ParkFitGroupProfile profile,
        CancellationToken cancellationToken);

    Task<ParkFitGroupProfileWriteOutcome> ReplaceAsync(
        ParkFitGroupProfile profile,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<ParkFitGroupProfileWriteOutcome> DeleteOwnedAsync(
        ParkFitGroupProfileId profileId,
        string ownerUserId,
        long expectedVersion,
        CancellationToken cancellationToken);
}
