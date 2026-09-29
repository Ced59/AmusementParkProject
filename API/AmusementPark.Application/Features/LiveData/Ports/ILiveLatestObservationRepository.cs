using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILiveLatestObservationRepository
{
    Task<LiveLatestObservationWriteResult> WriteLatestAsync(
        IReadOnlyCollection<LiveLatestObservation> observations,
        CancellationToken cancellationToken);
}
