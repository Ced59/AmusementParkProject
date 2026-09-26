namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalFieldCoverage
{
    public HistoricalFieldCoverage(int documentedSubjectCount, int applicableSubjectCount)
    {
        if (documentedSubjectCount < 0
            || applicableSubjectCount < 0
            || documentedSubjectCount > applicableSubjectCount)
        {
            throw new ArgumentOutOfRangeException(nameof(documentedSubjectCount));
        }

        this.DocumentedSubjectCount = documentedSubjectCount;
        this.ApplicableSubjectCount = applicableSubjectCount;
        this.Percentage = applicableSubjectCount == 0
            ? 100m
            : Math.Round(
                100m * documentedSubjectCount / applicableSubjectCount,
                1,
                MidpointRounding.AwayFromZero);
    }

    public int DocumentedSubjectCount { get; }

    public int ApplicableSubjectCount { get; }

    public decimal Percentage { get; }

    public bool IsComplete => this.DocumentedSubjectCount == this.ApplicableSubjectCount;
}
