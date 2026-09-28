namespace AmusementPark.WebAPI.Contracts.History;

public sealed class AdminHistoricalParkRolloutGateDto
{
    public bool IsOpen { get; init; }

    public bool HasEnoughStructuredFacts { get; init; }

    public bool HasCompleteSourceCoverage { get; init; }

    public bool HasMajorMilestone { get; init; }

    public bool HasIndexableKeyYear { get; init; }

    public int PublishedFactCount { get; init; }

    public int SourcedFactCount { get; init; }

    public int MajorFactCount { get; init; }

    public IReadOnlyCollection<int> IndexableKeyYears { get; init; } = Array.Empty<int>();
}
