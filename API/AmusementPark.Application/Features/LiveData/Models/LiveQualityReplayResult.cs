namespace AmusementPark.Application.Features.LiveData.Models;

public sealed record LiveQualityReplayResult(
    int ExaminedCount,
    int ResolvedCount,
    int StillBlockedCount,
    int PersistedCount,
    int IgnoredAsOlderCount);
