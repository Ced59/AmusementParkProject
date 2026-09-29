using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILiveOperationalGate
{
    Task<LiveOperationalGateSnapshot> LoadAsync(
        LiveDataSourceId sourceId,
        string externalEntityId,
        CancellationToken cancellationToken);
}
