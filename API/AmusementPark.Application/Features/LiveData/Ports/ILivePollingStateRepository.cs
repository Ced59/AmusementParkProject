using AmusementPark.Application.Features.LiveData.Models;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILivePollingStateRepository
{
    Task<LivePollingLease?> TryAcquireAsync(
        LivePollingLeaseRequest request,
        CancellationToken cancellationToken);

    Task<bool> CompleteAsync(
        LivePollingCompletion completion,
        CancellationToken cancellationToken);
}
