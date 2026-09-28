namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalDecadeCoverage
{
    public HistoricalDecadeCoverage(
        int decade,
        int resourceCount,
        int sourcedResourceCount,
        int publishedResourceCount,
        int subjectCount)
    {
        if (decade < 0
            || decade % 10 != 0
            || resourceCount < 0
            || sourcedResourceCount < 0
            || sourcedResourceCount > resourceCount
            || publishedResourceCount < 0
            || publishedResourceCount > resourceCount
            || subjectCount < 0
            || subjectCount > resourceCount)
        {
            throw new ArgumentException("The historical decade coverage is invalid.");
        }

        this.Decade = decade;
        this.ResourceCount = resourceCount;
        this.SourcedResourceCount = sourcedResourceCount;
        this.PublishedResourceCount = publishedResourceCount;
        this.SubjectCount = subjectCount;
    }

    public int Decade { get; }

    public int ResourceCount { get; }

    public int SourcedResourceCount { get; }

    public int PublishedResourceCount { get; }

    public int SubjectCount { get; }

    public int SourceCoveragePercentage => this.ResourceCount == 0
        ? 0
        : (int)Math.Round(
            this.SourcedResourceCount * 100m / this.ResourceCount,
            MidpointRounding.AwayFromZero);
}
