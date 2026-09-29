using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed record LiveOperationalObservationWriteResult(
    LiveLatestObservationWriteResult WriteResult,
    IReadOnlyCollection<LiveLatestObservation> AcceptedObservations);
