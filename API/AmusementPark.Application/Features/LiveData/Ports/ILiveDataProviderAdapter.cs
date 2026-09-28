using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILiveDataProviderAdapter
{
    LiveDataSourceId SourceId { get; }

    string AdapterVersion { get; }

    Task<LiveProviderReadResult> FetchLatestAsync(
        LiveProviderReadRequest request,
        CancellationToken cancellationToken);
}
