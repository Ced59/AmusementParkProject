namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Verdict métier qui autorise l'exposition publique de l'explorateur historique d'un parc.
/// Il est recalculé depuis les faits canoniques et ne dépend d'aucune visite.
/// </summary>
public sealed record HistoricalParkRolloutGate
{
    public HistoricalParkRolloutGate(
        int publishedFactCount,
        int sourcedFactCount,
        int majorFactCount,
        IReadOnlyCollection<int> indexableKeyYears)
    {
        ArgumentNullException.ThrowIfNull(indexableKeyYears);
        if (publishedFactCount < 0
            || sourcedFactCount < 0
            || sourcedFactCount > publishedFactCount
            || majorFactCount < 0
            || majorFactCount > publishedFactCount)
        {
            throw new ArgumentOutOfRangeException(nameof(publishedFactCount));
        }

        this.PublishedFactCount = publishedFactCount;
        this.SourcedFactCount = sourcedFactCount;
        this.MajorFactCount = majorFactCount;
        this.IndexableKeyYears = Array.AsReadOnly(indexableKeyYears
            .Distinct()
            .Order()
            .ToArray());
    }

    public int PublishedFactCount { get; }

    public int SourcedFactCount { get; }

    public int MajorFactCount { get; }

    public IReadOnlyList<int> IndexableKeyYears { get; }

    public bool HasEnoughStructuredFacts =>
        this.PublishedFactCount >= HistoricalSnapshotSeoEligibilityEvaluator.MinimumSupportingFactCount;

    public bool HasCompleteSourceCoverage =>
        this.PublishedFactCount > 0 && this.SourcedFactCount == this.PublishedFactCount;

    public bool HasMajorMilestone => this.MajorFactCount > 0;

    public bool HasIndexableKeyYear => this.IndexableKeyYears.Count > 0;

    public bool HasPublicTimeline => this.HasEnoughStructuredFacts
        && this.HasCompleteSourceCoverage
        && this.HasMajorMilestone;

    public bool IsOpen => this.HasPublicTimeline
        && this.HasIndexableKeyYear;
}
