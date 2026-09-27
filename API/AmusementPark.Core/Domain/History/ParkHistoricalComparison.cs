namespace AmusementPark.Core.Domain.History;

public sealed record ParkHistoricalComparison
{
    public ParkHistoricalComparison(
        ParkHistoricalSnapshot from,
        ParkHistoricalSnapshot to,
        IReadOnlyCollection<HistoricalSubjectComparison> subjects,
        IReadOnlyCollection<HistoricalCategoryNetChange> categoryNetChanges,
        int fromUnclassifiedOpenItemCount,
        int toUnclassifiedOpenItemCount,
        string methodologyVersion)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(subjects);
        ArgumentNullException.ThrowIfNull(categoryNetChanges);
        string normalizedMethodologyVersion = methodologyVersion?.Trim() ?? string.Empty;
        if (!string.Equals(from.ParkId, to.ParkId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Historical snapshots must belong to the same park.");
        }

        if (from.RequestedInstant.GetEnvelope().LatestPossibleDate
            >= to.RequestedInstant.GetEnvelope().EarliestPossibleDate)
        {
            throw new ArgumentException("The comparison start date must precede its end date.");
        }

        if (fromUnclassifiedOpenItemCount < 0 || toUnclassifiedOpenItemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fromUnclassifiedOpenItemCount));
        }

        if (normalizedMethodologyVersion.Length == 0 || normalizedMethodologyVersion.Length > 100)
        {
            throw new ArgumentException("A comparison methodology version is required.", nameof(methodologyVersion));
        }

        this.From = from;
        this.To = to;
        this.Subjects = Array.AsReadOnly(subjects
            .OrderBy(static subject => subject.To.Subject.Type)
            .ThenBy(static subject => subject.To.Subject.HistoricalLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static subject => subject.To.Subject.Id, StringComparer.Ordinal)
            .ToArray());
        this.CategoryNetChanges = Array.AsReadOnly(categoryNetChanges
            .OrderBy(static change => change.Category, StringComparer.OrdinalIgnoreCase)
            .ToArray());
        this.FromUnclassifiedOpenItemCount = fromUnclassifiedOpenItemCount;
        this.ToUnclassifiedOpenItemCount = toUnclassifiedOpenItemCount;
        this.MethodologyVersion = normalizedMethodologyVersion;
    }

    public ParkHistoricalSnapshot From { get; }

    public ParkHistoricalSnapshot To { get; }

    public IReadOnlyList<HistoricalSubjectComparison> Subjects { get; }

    public IReadOnlyList<HistoricalCategoryNetChange> CategoryNetChanges { get; }

    public int FromUnclassifiedOpenItemCount { get; }

    public int ToUnclassifiedOpenItemCount { get; }

    public bool IsCategoryComparisonComplete => this.FromUnclassifiedOpenItemCount == 0
        && this.ToUnclassifiedOpenItemCount == 0;

    public string MethodologyVersion { get; }
}
