namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoricalSnapshotImpactSummaryDto
{
    public int KnownOpenSubjectCount { get; init; }

    public int AmbiguityCount { get; init; }

    public int ReliablePeriodSubjectCount { get; init; }

    public int PartialPeriodSubjectCount { get; init; }

    public int UndatedSubjectCount { get; init; }
}
