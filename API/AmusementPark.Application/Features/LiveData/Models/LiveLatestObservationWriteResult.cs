using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed record LiveLatestObservationWriteResult(
    int InsertedCount,
    int UpdatedCount,
    int IgnoredCount,
    IReadOnlyCollection<LiveLatestObservation>? StoredLatestObservations = null)
{
    public IReadOnlyCollection<LiveLatestObservation> CommittedObservations =>
        this.StoredLatestObservations ?? Array.Empty<LiveLatestObservation>();
}
