using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILiveLatestObservationRepository
{
    Task<IReadOnlyCollection<LiveLatestObservation>> GetByTargetAsync(
        LiveTargetType targetType,
        string targetId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LiveLatestObservation>> GetParkItemsAsync(
        string parkId,
        CancellationToken cancellationToken);

    Task<LiveLatestObservationWriteResult> WriteLatestAsync(
        IReadOnlyCollection<LiveLatestObservation> observations,
        CancellationToken cancellationToken);
}
