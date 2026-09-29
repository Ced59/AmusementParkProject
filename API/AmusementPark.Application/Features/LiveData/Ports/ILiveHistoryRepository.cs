using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILiveHistoryRepository
{
    Task StoreAsync(
        IReadOnlyCollection<LiveLatestObservation> observations,
        LiveHistoryRetentionPolicy retentionPolicy,
        CancellationToken cancellationToken);
}
