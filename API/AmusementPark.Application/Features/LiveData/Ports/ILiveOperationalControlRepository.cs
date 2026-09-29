using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILiveOperationalControlRepository
{
    Task<LiveOperationalControl?> GetLatestAsync(
        LiveOperationalControlScope scope,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LiveOperationalControl>> GetLatestBySourceAsync(
        LiveDataSourceId sourceId,
        CancellationToken cancellationToken);

    Task<LiveOperationalControlWriteOutcome> AppendRevisionAsync(
        LiveOperationalControl control,
        int expectedRevision,
        CancellationToken cancellationToken);
}
