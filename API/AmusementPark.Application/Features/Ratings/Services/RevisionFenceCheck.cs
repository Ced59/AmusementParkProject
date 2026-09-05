namespace AmusementPark.Application.Features.Ratings.Services;

internal sealed record RevisionFenceCheck(
    RevisionFenceDisposition Disposition,
    long? GlobalRevision,
    bool CacheConverged);
