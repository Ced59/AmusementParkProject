namespace AmusementPark.Application.Features.LiveData.Models;

public sealed record LiveLatestObservationIngestionResult(
    int PersistedCount,
    int IgnoredAsOlderCount,
    int UnmappedCount,
    int IneligibleCount);
