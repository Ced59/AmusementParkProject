using AmusementPark.Application.Features.LiveData.Models;

namespace AmusementPark.Application.Features.LiveData.Ports;

public interface ILiveLatestObservationIngestor
{
    Task<LiveLatestObservationIngestionResult> IngestAsync(
        LiveLatestObservationIngestionRequest request,
        CancellationToken cancellationToken);
}
