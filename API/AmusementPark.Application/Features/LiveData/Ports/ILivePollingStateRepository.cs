using AmusementPark.Application.Features.LiveData.Models;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILivePollingStateRepository
{
    Task<LivePollingStateSnapshot?> GetAsync(
        AmusementPark.Core.Domain.LiveData.LiveDataSourceId sourceId,
        string externalEntityId,
        CancellationToken cancellationToken);

    Task<LivePollingLease?> TryAcquireAsync(
        LivePollingLeaseRequest request,
        CancellationToken cancellationToken);

    Task<bool> CompleteAsync(
        LivePollingCompletion completion,
        CancellationToken cancellationToken);
}
