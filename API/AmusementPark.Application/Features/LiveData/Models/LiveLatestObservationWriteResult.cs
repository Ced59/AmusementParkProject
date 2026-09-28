namespace AmusementPark.Application.Features.LiveData.Models;

public sealed record LiveLatestObservationWriteResult(
    int InsertedCount,
    int UpdatedCount,
    int IgnoredCount);
