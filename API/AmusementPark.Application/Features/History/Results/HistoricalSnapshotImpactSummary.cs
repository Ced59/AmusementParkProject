namespace AmusementPark.Application.Features.History.Results;

public sealed record HistoricalSnapshotImpactSummary(
    int KnownOpenSubjectCount,
    int AmbiguityCount,
    int ReliablePeriodSubjectCount,
    int PartialPeriodSubjectCount,
    int UndatedSubjectCount);
