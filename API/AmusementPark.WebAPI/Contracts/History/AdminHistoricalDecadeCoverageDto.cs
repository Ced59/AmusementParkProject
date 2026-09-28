namespace AmusementPark.WebAPI.Contracts.History;

public sealed class AdminHistoricalDecadeCoverageDto
{
    public int Decade { get; init; }

    public int ResourceCount { get; init; }

    public int SourcedResourceCount { get; init; }

    public int PublishedResourceCount { get; init; }

    public int SubjectCount { get; init; }

    public int SourceCoveragePercentage { get; init; }
}
